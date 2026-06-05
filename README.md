# weather-100-percent

Bot de trading para mercados de temperatura no Polymarket. Monitora observações METAR em tempo real e executa estratégias complementares: **Contested NO** (buckets impossíveis para o dia), **Peak Detection** (queda confirma o pico) e **Daily Peak Trigger** (safety net horário às 16h).

## Como funciona

### Domínio

O Polymarket publica eventos do tipo _"highest temperature in São Paulo on June 2, 2026"_. Cada evento tem vários **buckets**: outcomes binários finitos para um valor inteiro em °C (ex.: `18°C`) ou para um range inteiro em °F (ex.: `78-79°F`). O bot opera exclusivamente sobre buckets finitos do meio; buckets abertos `or below` e `or higher` continuam ignorados.

O bot mantém um **ObservedMax** por cidade — a temperatura máxima observada hoje via METAR. À medida que o ObservedMax sobe durante o dia, três oportunidades surgem:

1. **Buckets impossíveis** — qualquer bucket abaixo do ObservedMax nunca resolverá em YES. São compras de NO a preço próximo de 1.00 (*Contested NO*).
2. **Pico confirmado por queda** — quando a temperatura cai depois de uma máxima entre 12h–16h, o bucket do pico tem alta probabilidade de resolver YES e o bucket acima tem alta probabilidade de resolver NO (*Peak Detection*).
3. **Pico confirmado por horário** — se nenhuma queda foi detectada, o primeiro METAR da hora configurada (16h por padrão) serve de gatilho para a mesma posição, mesmo sem queda detectada (*Daily Peak Trigger*).

### Fontes de dados

As temperaturas vêm de METARs — relatórios padronizados de aviação emitidos a cada hora por estações identificadas por código ICAO. O bot busca em paralelo em dois endpoints:

- **NOAA TGFTP** (`tgftp.nws.noaa.gov`) — texto plano
- **AviationWeather** (`aviationweather.gov/api/data/metar`) — JSON

Dentro da Hot Window, a primeira resposta que elevar o `ObservedMax` dispara a avaliação dos buckets. Fora da Hot Window, o warm poll apenas semeia/atualiza `ObservedMax` e o Dashboard; não posta Contested NO.

### Hot Window

O bot opera em dois modos de polling:

- **Warm poll**: fora do hot window, busca METARs, mantém o book cache atualizado, prepara ordens assinadas e semeia `ObservedMax` sem postar Contested NO.
- **Hot window**: no timezone configurado da cidade, dos minutos configurados da hora anterior até os minutos configurados da hora alvo (ex.: 09:53–10:04 locais para o mercado das 10h), cada endpoint roda em loop próprio, contínuo e sem sleep. Cada resposta de METAR é processada assim que chega, sem esperar o outro endpoint.
- **Book stream**: o cache de asks recebe snapshots e updates pelo websocket de market data do Polymarket, com refresh REST como fallback.
- **Prewarm**: antes da disputa, o bot aquece metadata do CLOB e pré-assina ordens FAK de limite e market por bucket exact.

### Estratégias

O bot suporta três estratégias, controladas pela variável `STRATEGY`. Cada uma opera sobre os mesmos dados de METAR e pode ser combinada com as demais.

---

#### `no` — Contested NO (padrão)

Compra NO nos buckets que se tornaram meteorologicamente impossíveis para o dia.

**Condição:** `floor(convert(ObservedMax, bucket.unit)) > bucket.upperTemp`

Durante a Hot Window, quando o ObservedMax sobe, ele é convertido para a unidade do bucket (`°C` ou `°F`) e só então arredondado para baixo ao grau inteiro usado pelo mercado. Para cada bucket ainda não comprado cujo limite superior já foi ultrapassado, dispara um FAK imediatamente. Ex.: `12.2°C` não compra NO em `12°C`; só `13.0°C` ou mais. Em bucket `70-71°F`, só compra quando `floor(ObservedMax em °F) > 71`.

```
METAR na Hot Window (tempC > ObservedMax)
  └─ evaluateBuckets
       └─ postOrder (FAK NO, limite 0.99) — paralelo por bucket
```

---

#### `peak` — Peak Detection

Compra YES no bucket do pico e NO no bucket imediatamente acima quando uma queda confirma que o pico passou. Também ativa o gatilho horário configurado por `PEAK_TRIGGER_HOUR` (16h por padrão). As duas sub-estratégias partilham o flag `peakTriggered`: quem disparar primeiro vence, a outra torna-se no-op.

**Peak Detection** (`evaluatePeakDrop`)

Dispara quando uma observação de temperatura mais baixa confirma que o pico passou. Ativo entre 12h–16h (hora local da cidade).

- Condição: `obs.tempC < ObservedMax` enquanto `localHour ∈ [12, 16)`
- Compra: YES no bucket que contém `floor(convert(ObservedMax, bucket.unit))` a 0.95 + NO no próximo bucket finito acima a 0.97

```
METAR (tempC < ObservedMax, 12h–16h local)
  └─ evaluatePeakDrop
       ├─ postPeakYes (FAK YES, limite 0.95)
       └─ postPeakNo  (FAK NO,  limite 0.97)
```

**Daily Peak Trigger** (`evaluatePeakAtHour`)

Safety net baseado em horário. Se o Peak Detection ainda não disparou, o primeiro METAR da hora de disparo (`PEAK_TRIGGER_HOUR`, 16h local por padrão) encerra a posição com o ObservedMax do momento — mesmo sem ter observado queda de temperatura.

- Condição: `DAILY_PEAK_TRIGGER !== false`, `metarLocalHour === PEAK_TRIGGER_HOUR` e `peakTriggered === false`
- Se a temperatura subiu no METAR da hora de gatilho, o ObservedMax é actualizado antes do disparo.
- Compra: YES no bucket que contém `floor(convert(ObservedMax, bucket.unit))` a 0.95 + NO no próximo bucket finito acima a 0.97

```
METAR (observedAt hora local === PEAK_TRIGGER_HOUR)
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
       │    ├─ [Hot Window] evaluateBuckets → postOrder (NO, 0.99) por bucket contestado
       │    └─ evaluatePeakAtHour → postPeakYes + postPeakNo se hora local = PEAK_TRIGGER_HOUR
       └─ [tempC ≤ ObservedMax, ObservedMax ≠ -∞]
            ├─ evaluatePeakDrop   → postPeakYes + postPeakNo se queda 12h–16h
            └─ evaluatePeakAtHour → postPeakYes + postPeakNo se hora local = PEAK_TRIGGER_HOUR
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
DAILY_PEAK_TRIGGER=true   # false desativa o gatilho horário sem queda confirmada
PEAK_TRIGGER_HOUR=16      # hora local usada pelo Daily Peak Trigger

TARGET_HOURS=10,11,12,13,14,15,16        # horas locais da cidade com hot window ativo
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

O script dispara compras para todos os buckets onde `floor(convert(--obs, bucket.unit)) > bucket.upperTemp`. Com `DRY_RUN=false` as ordens chegam ao CLOB — se a wallet não tiver saldo ou o mercado já estiver encerrado, o `postOrder` captura o erro e loga sem travar.

### Exemplo de saída

```
[sim] fetching buckets for sao-paulo (SBGR)...
[sim] 9 buckets loaded. exact: 16°C, 17°C, 18°C, 19°C, 20°C, 21°C, 22°C, 23°C, 24°C
[sim] obs=19.5°C  initial-max=17.0°C  resolved=19°C
[sim] expected triggers: 16°C, 17°C, 18°C
[sim] firing handleObs...

2026-06-02 21:38:27 BRT [sim/SBGR] tempC=19.5 metar=2026-06-02 21:38:27 BRT
2026-06-02 21:38:27 BRT [sim/SBGR] observedMax 17 → 19.5
2026-06-02 21:38:27 BRT [trader] attempt tokenId=... bucket=16°C price≤0.99 shares=4.04
2026-06-02 21:38:27 BRT [trader] attempt tokenId=... bucket=17°C price≤0.99 shares=4.04
2026-06-02 21:38:27 BRT [trader] attempt tokenId=... bucket=18°C price≤0.99 shares=4.04
2026-06-02 21:38:28 BRT [trader] result=400 tokenId=... bucket=16°C
2026-06-02 21:38:28 BRT [trader] result=400 tokenId=... bucket=17°C
2026-06-02 21:38:28 BRT [trader] result=400 tokenId=... bucket=18°C
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
