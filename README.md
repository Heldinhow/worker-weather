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

- **Warm poll**: fora do hot window, busca METARs a cada 30 s (durante as horas de operação) ou 10 min (fora delas), mantém o book cache atualizado e prepara ordens assinadas.
- **Hot window**: dos 53 min da hora anterior até os 4 min da hora alvo (ex.: 09:53–10:04 para o mercado das 10h), cada endpoint roda em loop próprio, contínuo e sem sleep. Cada resposta de METAR é processada assim que chega, sem esperar o outro endpoint.
- **Book stream**: o cache de asks recebe snapshots e updates pelo websocket de market data do Polymarket, com refresh REST como fallback.
- **Prewarm**: antes da disputa, o bot aquece metadata do CLOB e pré-assina ordens FAK de limite e market por bucket exact.

### Estratégias

O bot suporta três estratégias, controladas pela variável `STRATEGY`. Cada uma opera sobre os mesmos dados de METAR e pode ser combinada com as demais.

---

#### `no` — Contested NO (padrão)

Compra NO nos buckets que se tornaram meteorologicamente impossíveis para o dia.

**Condição:** `floor(ObservedMax) > bucket.tempC`

Quando o ObservedMax sobe, todos os buckets abaixo do novo piso são avaliados. Para cada bucket ainda não comprado, dispara um FAK imediatamente. Não tem estado de dia: reavalía a cada nova observação que elevar o ObservedMax.

```
METAR (tempC > ObservedMax)
  └─ evaluateBuckets
       └─ postOrder (FAK NO, limite 0.99) — paralelo por bucket
```

---

#### `peak` — Peak Detection + Daily Peak Trigger

Duas sub-estratégias complementares que compram YES no bucket do pico e NO no bucket imediatamente acima. Partilham o flag `peakTriggered`: quem disparar primeiro vence, a outra torna-se no-op.

**Peak Detection** (`evaluatePeakDrop`)

Dispara quando uma observação de temperatura mais baixa confirma que o pico passou. Ativo entre 12h–16h (hora local da cidade).

- Condição: `obs.tempC < ObservedMax` enquanto `localHour ∈ [12, 16)`
- Compra: YES@`floor(ObservedMax)` a 0.95 + NO@`floor(ObservedMax)+1` a 0.97

```
METAR (tempC < ObservedMax, 12h–16h local)
  └─ evaluatePeakDrop
       ├─ postPeakYes (FAK YES, limite 0.95)
       └─ postPeakNo  (FAK NO,  limite 0.97)
```

**Daily Peak Trigger** (`evaluatePeakAtHour`)

Safety net baseado em horário: o pico de temperatura máxima ocorre normalmente até às 15h. Se o Peak Detection ainda não disparou, o primeiro METAR da hora de disparo (15h local por defeito) encerra a posição com o ObservedMax do momento — mesmo sem ter observado queda de temperatura.

- Condição: `metarLocalHour === 15` e `peakTriggered === false`
- Se a temperatura subiu no METAR das 15h, o ObservedMax é actualizado antes do disparo.
- Compra: YES@`floor(ObservedMax)` a 0.95 + NO@`floor(ObservedMax)+1` a 0.97

```
METAR (observedAt hora local === 15)
  └─ evaluatePeakAtHour
       ├─ postPeakYes (FAK YES, limite 0.95)
       └─ postPeakNo  (FAK NO,  limite 0.97)
```

---

#### `both` — NO + Peak

Corre as estratégias `no` e `peak` em simultâneo sobre cada observação. Útil em dias com variação de temperatura significativa: o Contested NO captura os buckets abaixo do pico real enquanto Peak Detection/Daily Peak Trigger fecham a posição no pico.

---

### Fluxo de uma compra (estratégia `both`)

```
METAR
  └─ handleObs
       ├─ [tempC > ObservedMax] → atualiza ObservedMax
       │    ├─ evaluateBuckets   → postOrder (NO, 0.99) por bucket contestado
       │    └─ evaluatePeakAtHour → postPeakYes + postPeakNo se hora local = 15
       └─ [tempC ≤ ObservedMax, ObservedMax ≠ -∞]
            ├─ evaluatePeakDrop   → postPeakYes + postPeakNo se queda 12h–16h
            └─ evaluatePeakAtHour → postPeakYes + postPeakNo se hora local = 15
```

As chamadas a `postOrder` para múltiplos buckets são disparadas em paralelo (fire-and-forget no event loop).

### Ordem FAK

O bot só posta ordens **Fill-and-Kill**. Se houver ordem pré-assinada para o bucket, ela é usada no hot path para evitar assinatura e chamadas de metadata no momento da disputa. Se o book cache estiver frio, o bot dispara em paralelo um FAK limite a 0.99 e um market FAK com preço limite 0.99. Se o CLOB responder que não havia ordens para preencher, o bucket é marcado como `attempted`.

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
MIN_SHARES=5         # mínimo de shares para ordem FAK pré-assinada
PROD=false           # true: aplica guarda de custo mínimo (≥ $1.00 por ordem)
DRY_RUN=false        # true: loga tudo mas nunca chama o CLOB
STRATEGY=no          # no | peak | both  (ver secção Estratégias)

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
  evaluator.ts      — lógica pura das três estratégias (evaluateBuckets, evaluatePeakDrop, evaluatePeakAtHour)
  trader.ts         — postOrder: book cache → FAK pré-assinado
  order-cache.ts    — prewarm de metadata CLOB e ordens assinadas
  book-cache.ts     — cache de order book via REST + websocket
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
