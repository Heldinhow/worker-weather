# weather-100-percent

Bot que monitora mercados de temperatura no Polymarket e compra NO assim que uma observação METAR torna um bucket meteorologicamente impossível para o dia.

## Como funciona

### Domínio

O Polymarket publica eventos do tipo _"highest temperature in São Paulo on June 2, 2026"_. Cada evento tem vários **buckets**: mercados binários para um valor exato de temperatura em °C (ex.: "exactly 18°C"). O bot opera exclusivamente sobre buckets do tipo `exact`.

Quando a temperatura máxima observada no dia (`ObservedMax`) passa de 19°C, por exemplo, o bucket "exactly 18°C" torna-se impossível de resolver em YES — a temperatura já foi maior. O NO desse bucket vale 1.00 e pode ser comprado a qualquer preço abaixo disso. O bot chama essa posição de **Contested NO** e compra imediatamente.

A condição exata: `floor(ObservedMax) > bucket.tempC`.

### Fontes de dados

As temperaturas vêm de METARs — relatórios padronizados de aviação emitidos a cada hora por estações identificadas por código ICAO. O bot busca em paralelo em dois endpoints:

- **NOAA TGFTP** (`tgftp.nws.noaa.gov`) — texto plano
- **AviationWeather** (`aviationweather.gov/api/data/metar`) — JSON

A primeira resposta que elevar o `ObservedMax` dispara a avaliação dos buckets.

### Hot Window

O bot opera em dois modos de polling:

- **Warm poll**: fora do hot window, busca METARs a cada 30 s (durante as horas de operação) ou 10 min (fora delas) e mantém o book cache atualizado.
- **Hot window**: dos 53 min da hora anterior até os 4 min da hora alvo (ex.: 09:53–10:04 para o mercado das 10h), as buscas são contínuas e concorrentes, sem sleep entre ciclos. Cada novo METAR dispara `evaluateBuckets` imediatamente.

### Fluxo de uma compra

```
fetchNoaa / fetchAviationWeather
  └─ handleObs           — atualiza ObservedMax se tempC > atual
       └─ evaluateBuckets — para cada bucket exact onde floor(ObservedMax) > tempC
            └─ postOrder  — FOK cego (0.99) ou baseado no book cache
```

As chamadas a `postOrder` para múltiplos buckets são disparadas em paralelo (fire-and-forget no event loop).

### Ordem FOK

O bot só posta ordens **Fill-or-Kill**. Se o book cache tem asks, consome as ofertas disponíveis até `MAX_STAKE`. Se o book estiver vazio (cache confirmado vazio), posta um FOK cego a 0.99 e marca o bucket como `attempted` permanentemente.

---

## Configuração

Copie `.env.example` para `.env` e preencha:

```env
# Obrigatório
PRIVATE_KEY=0x...                        # chave privada da wallet Polygon
CITIES=[{"slug":"sao-paulo","icao":"SBGR","timezone":"America/Sao_Paulo"}]

# Opcional — credenciais Polymarket pré-derivadas
POLY_FUNDER_ADDRESS=0x...
POLY_SIGNATURE_TYPE=                     # deixar em branco para EOA

# Parâmetros de execução
MAX_STAKE=4          # USDC máximo por ordem
MIN_SHARES=5         # mínimo de shares para não pular a ordem (exceto blind FOK)
PROD=false           # true: aplica guarda de custo mínimo (≥ $1.00 por ordem)
DRY_RUN=false        # true: loga tudo mas nunca chama o CLOB

TARGET_HOURS=10,11,12,13,14,15,16        # horas BRT com hot window ativo
```

> `.env` está no `.gitignore` — nunca comite credenciais.

---

## Executar o bot

```bash
bun --env-file=.env src/index.ts
```

Com DRY_RUN:

```bash
DRY_RUN=true bun --env-file=.env src/index.ts
```

---

## Simular uma detecção

`scripts/simulate-detection.ts` busca os buckets reais do dia no Polymarket, injeta uma observação sintética e roda o caminho completo `handleObs → evaluateBuckets → postOrder` contra o CLOB real.

```bash
bun --env-file=.env scripts/simulate-detection.ts \
  --icao SBGR \
  --obs 19.5 \
  --initial-max 17.0
```

| Argumento | Descrição |
|---|---|
| `--icao` | Código ICAO da estação (deve estar em `CITIES`) |
| `--obs` | Temperatura da observação simulada (°C) |
| `--initial-max` | ObservedMax inicial antes da observação (°C) |

O script dispara compras para todos os buckets onde `floor(--obs) > tempC`. Com `DRY_RUN=false` as ordens chegam ao CLOB — se a wallet não tiver saldo ou o mercado já estiver encerrado, o `postOrder` captura o erro e loga sem travar.

### Exemplo de saída

```
[sim] fetching buckets for sao-paulo (SBGR)...
[sim] 9 buckets loaded. exact: 16°C, 17°C, 18°C, 19°C, 20°C, 21°C, 22°C, 23°C, 24°C
[sim] obs=19.5°C  initial-max=17.0°C  floor(obs)=19
[sim] expected triggers: 16°C, 17°C, 18°C
[sim] firing handleObs...

2026-06-02 21:38:27 BRT [sim/SBGR] tempC=19.5 metar=2026-06-02 21:38:27 BRT
2026-06-02 21:38:27 BRT [sim/SBGR] observedMax 17 → 19.5
2026-06-02 21:38:27 BRT [trader] attempt tokenId=... tempC=16 price=0.99 shares=4.04 cost=3.9996 blind=true
2026-06-02 21:38:27 BRT [trader] attempt tokenId=... tempC=17 price=0.99 shares=4.04 cost=3.9996 blind=true
2026-06-02 21:38:27 BRT [trader] attempt tokenId=... tempC=18 price=0.99 shares=4.04 cost=3.9996 blind=true
2026-06-02 21:38:28 BRT [trader] result=400 tokenId=... tempC=16
2026-06-02 21:38:28 BRT [trader] result=400 tokenId=... tempC=17
2026-06-02 21:38:28 BRT [trader] result=400 tokenId=... tempC=18
```

Os três `attempt` aparecem em ~3 ms (paralelo). Os `result` chegam ~550 ms depois (round-trip CLOB). `result=400` indica mercado encerrado ou saldo insuficiente — comportamento esperado em teste.

---

## Estrutura

```
src/
  index.ts          — bootstrap: carrega config, inicia CLOB, roda cidades
  clob.ts           — inicialização do ClobClient (reutilizado pelo script de simulação)
  hot-window.ts     — loop principal: warm poll + hot window + handleObs
  evaluator.ts      — lógica pura de detecção de Contested NO
  trader.ts         — postOrder: book cache → dimensionamento → FOK
  book-cache.ts     — cache de order book atualizado em background
  config.ts         — leitura de env vars
  logger.ts         — log formatado em BRT
  types.ts          — BucketState, ObservationResult
  fetchers/
    noaa.ts         — fetch METAR via NOAA TGFTP
    aviation-weather.ts — fetch METAR via AviationWeather JSON

scripts/
  simulate-detection.ts  — simulação de detecção com observação sintética
  latency-check.ts       — benchmark de latência dos endpoints externos
```
