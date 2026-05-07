# Polymarket.Bot — Plano de Implementação

## 1. Resumo Executivo

Criar uma base .NET para um bot/monitor de trading na Polymarket com paper trading funcional, simulação realista do fluxo de trade, monitoramento de mercados, estratégia inicial baseada em entrada 99c, stop loss obrigatório, monitoramento de tendência e momentum BTC/ETH, registro estruturado de decisões, logs, traces e analytics de winrate.

**Stack:** .NET 8.0 (LTS), Clean Architecture, Minimal API, Worker Service, EF Core + PostgreSQL, Redis, Docker Compose, FluentResults, Swagger, Health Checks, xUnit.

> **Decisão de versão .NET:** .NET 10.0.100 está disponível mas é preview. A versão LTS estável disponível é **.NET 8.0.416**. Recomendação: usar **.NET 8** como target para produção. Documentar que .NET 12+ pode ser adotado quando sair e for LTS.

**Inspiração conceitual:** Repositório Python de referência (Heldinhow/polymarket-kalshi-weather-bot). Não é migração 1:1 — é redesign limpo com arquitetura .NET.

**Fora do escopo:** Kalshi, live trading real, frontend, controllers MVC.

---

## 2. Escopo do MVP

### Dentro do MVP
- Solution base .NET 8 com Clean Architecture
- Docker Compose com PostgreSQL + Redis
- Domínio mínimo de trading (Market, Order, Trade, Position, PnL, Signal, DecisionTrace)
- Value Objects (Probability, Money, MarketPrice, Edge, PositionSize, StopLossThreshold)
- Paper trading pipeline completo (mesmo fluxo do live, simulação de fill)
- Stop loss obrigatório como parte do pipeline
- Estratégia 99c com Trend/Momentum de BTC/ETH
- DecisionTrace estruturado para toda decisão
- Analytics de winrate por estratégia, tendência, momentum, fator de decisão
- Minimal API com Swagger e health checks
- Worker Service orquestrando ciclos de execução
- Testes unitários de domínio e pipeline

### Fora do MVP (para depois)
- Integração real com Polymarket Gamma API
- Integração real com feed de preço BTC/ETH (Binance/Coinbase)
- Live trading real (bloqueado por feature flag)
- Dashboard de frontend
- Alertas e notificações
- Backtest export para análise externa
- Deploy em produção

---

## 3. Projetos da Solution

```
src/
├── Polymarket.Bot.Domain/
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Enums/
│   ├── Events/               # Domain events (se usado)
│   └── Services/             # Domain services (lógica pura)
│
├── Polymarket.Bot.Application/
│   ├── UseCases/
│   ├── DTOs/
│   ├── Abstractions/         # Interfaces (IOrderExecutor, IMarketScanner, etc.)
│   ├── InputContexts/        # StrategyInputContext, TrendDecisionContext
│   └── Services/            # Application services
│
├── Polymarket.Bot.Infrastructure/
│   ├── Persistence/
│   │   ├── Configurations/   # EF Core configurations
│   │   ├── Repositories/
│   │   └── DbContext/
│   ├── Cache/
│   │   └── RedisCacheService (decorator pattern)
│   ├── External/
│   │   ├── PolymarketClient/  # Stub (MVP) + GammaApiClient (futuro)
│   │   └── CryptoPriceFeed/   # Stub (MVP) + BinanceClient (futuro)
│   └── Execution/
│       ├── PaperOrderExecutor
│       ├── StubLiveOrderExecutor
│       ├── OrderFillSimulator
│       └── TradeExecutionPipeline
│
├── Polymarket.Bot.Api/
│   ├── MinimalApiExtensions/
│   ├── Endpoints/           # Um arquivo por feature group
│   └── Program.cs
│
└── Polymarket.Bot.Worker/
    ├── Services/
    │   ├── TradingOrchestrator
    │   ├── StopLossMonitor
    │   ├── MarketScanner
    │   ├── TrendAnalyzer
    │   └── AnalyticsCollector
    ├── Program.cs
    └── Worker.cs

tests/
├── Polymarket.Bot.Domain.Tests/
├── Polymarket.Bot.Application.Tests/
├── Polymarket.Bot.Infrastructure.Tests/
├── Polymarket.Bot.Api.Tests/
└── Polymarket.Bot.Worker.Tests/
```

---

## 4. Domínio Inicial

### Entidades (Phase 2)

| Entidade | Responsabilidade | Relaciones |
|---|---|---|
| `Market` | Representa um mercado Polymarket. Contém outcomes, preços, volume. | 1:N MarketOutcome, 1:N Signal |
| `MarketOutcome` | Um resultado específico (UP/DOWN, YES/NO). Preço, token_id, best_bid/ask, spread, liquidez. | N:1 Market |
| `Strategy` | Configuração de uma estratégia (tipo, parâmetros, enabled). | 1:N Signal, 1:N TradingDecision |
| `Signal` | Sinal gerado por uma estratégia — contém direção, probabilidade, edge, confiança. | N:1 Market, N:1 Strategy, 1:N TradingDecision |
| `TradingDecision` | Decisão final após avaliação de estratégia + risco. Status Approved/Rejected/Skipped. | N:1 Signal, N:1 Order, 1:1 DecisionTrace |
| `Order` | Ordem criada a partir de uma decisão aprovada. Pode ser Pending/Filled/Rejected/Cancelled. | N:1 TradingDecision, 1:N Trade |
| `Trade` | Trade executado (paper ou live). Contém entry_price, size, shares_filled, filled_at. | N:1 Order, N:1 Position, 1:1 DecisionTrace |
| `Position` | Posição aberta ou fechada. Entry_price, stop_loss_price, current_price, status, PnL realized/unrealized. | 1:N Trade, N:1 Position, 1:1 DecisionTrace |
| `StopLossEvent` | Evento de stop loss. Posição que foi fechada por stop loss. | N:1 Position, 1:1 DecisionTrace |
| `BotRun` | Sessão de execução do bot (start/stop). | 1:N ExecutionCycle |
| `ExecutionCycle` | Um ciclo do worker. Coleta dados, avalia mercados, executa trades, registra snapshots. | N:1 BotRun, 1:N PnLSnapshot |
| `PnLSnapshot` | Snapshot de PnL por ciclo. Bankroll, total_invested, total_pnl. | N:1 ExecutionCycle |
| `AssetPriceSnapshot` | Preço de BTC/ETH em um timestamp. | N:1 Asset |
| `AssetTrendSnapshot` | Tendência calculada para BTC/ETH. Direction, momentum, strength, confidence. | N:1 Asset |

### Value Objects (Phase 2)

| VO | Validação | Immutable |
|---|---|---|
| `Probability` | 0 ≤ value ≤ 1 | ✅ |
| `Money` | value ≥ 0, Currency não vazio | ✅ |
| `MarketPrice` | price ≥ 0, best_bid ≥ 0, best_ask ≥ price ≥ best_bid | ✅ |
| `Edge` | Calculado: model_prob - market_prob. Em decimal. | ✅ |
| `PositionSize` | shares ≥ 0, notional ≥ 0 | ✅ |
| `StopLossThreshold` | 0 < value ≤ 1 (padrão 0.20 = 20%) | ✅ |
| `PnL` | decimal (pode ser negativo) | ✅ |

### Enums (Phase 2)

```csharp
MarketStatus         { Open, Closed, Resolved }
SignalAction        { Buy, Sell, Hold, Skip }
TradingMode         { Paper, Live }
TradingDecisionStatus { Approved, Rejected, Skipped, Pending }
OrderStatus        { Pending, Filled, PartiallyFilled, Rejected, Cancelled }
OrderSide           { Buy, Sell }
TradeStatus        { Open, Closed, StopLossTriggered }
PositionStatus     { Open, Closed, StopLossTriggered }
ExitReason          { Manual, StopLoss, TakeProfit, MarketSettled, TimeExpired }
StrategyType        { HighProb99C, Endgame99C, Adaptive }
AssetSymbol         { BTC, ETH }
TrendDirection      { Up, Down, Sideways, Unknown }
MomentumState       { StrongBullish, Bullish, Neutral, Bearish, StrongBearish }
TrendTimeframe       { OneMinute, FiveMinutes, FifteenMinutes, OneHour }
```

### Decision Trace Entities (Phase 7)

| Entidade | Responsabilidade |
|---|---|
| `DecisionTrace` | Raiz de rastreamento. Aponta para Signal, TradingDecision, Order, Trade, Position. Status: Started/Completed |
| `DecisionTraceStep` | Passos sequenciais do trace. Tipo, description, timestamp, metadata_json |
| `DecisionInputSnapshot` | Snapshot dos inputs no momento da decisão (preços, spreads, liquidez, tempo restante) |
| `RiskEvaluationTrace` | Avaliação de cada regra de risco. Regra, resultado, aprovado/rejeitado, reason |
| `StrategyReasoningTrace` | Raciocínio da estratégia. Score breakdown, positive/negative/blocking factors, reason |
| `DecisionOutcomeReview` | Revisão post-hoc. Comparação do que foi decidido vs resultado real |
| `StrategyDecisionFactor` | Fator individual que influenciou a decisão. Nome, direction, weight, actual_outcome |
| `MissedOpportunity` | Trade que foi pulado (Skip) mas que teria ganhado. Análise para self-improvement |

### Analytics Entities (Phase 8)

| Entidade | Responsabilidade |
|---|---|
| `TradeOutcome` | Resultado de um trade settled. win/loss/push, pnl, exit_reason |
| `StrategyPerformanceSnapshot` | Performance agregada por estratégia por período |
| `DecisionFactorPerformance` | Winrate agrupado por fator de decisão (ex: factor X → 65% winrate) |
| `SelfImprovementReport` | Relatório gerado com recomendações |

---

## 5. Fluxos Principais

### 5.1 Fluxo de Paper Trading (Phase 3)

```
TradingDecision (Approved)
  → RiskManager (check spread, liquidity, daily loss limit, position limit)
    → RiskEvaluationTrace recorded
    → If Rejected → Stop
  → IOrderExecutor.PlaceOrder(candidate) → Order (Pending)
  → IOrderFillSimulator.SimulateFill(order, candidate) → fills or rejects
    → Order updated to Filled or Rejected
    → Trade created
    → Position opened (or updated if existing)
    → StopLossPrice calculated: entry_price × (1 - StopLossPercentage)
    → PnLSnapshot registered
    → DecisionTrace completed
    → DecisionOutcomeReview pending
```

### 5.2 Fluxo de Stop Loss (Phase 4)

```
Worker Cycle Start
  → For each Open Position
    → Fetch current market price (best_bid)
    → Compare current_price with StopLossPrice
    → If current_price ≤ StopLossPrice
      → Trigger IExitTradeExecutor.ExitPosition(position, "stop_loss")
      → Simulate sell at best_bid (or at stop_loss_price - slippage)
      → Position closed (Status = StopLossTriggered)
      → StopLossEvent created
      → PnLSnapshot updated
      → DecisionTrace completed with exit trace
      → TradeOutcome registered (loss)
    → If price improved
      → Update unrealized_pnl
      → Optionally: check take-profit conditions (backlog)
  → Continue to Market Scanning
```

### 5.3 Fluxo do Worker (Phase 10)

```
Worker Loop (configurable interval)
  1.  Start ExecutionCycle → BotRun
  2.  Capture BTC/ETH price → AssetPriceSnapshot
  3.  Analyze trend/momentum → AssetTrendSnapshot
  4.  Check open positions → StopLossMonitor
  5.  Update positions with current price
  6.  Trigger stop losses (if any)
  7.  Update PnL snapshots
  8.  Scan Polymarket markets → Market list (stub in MVP)
  9.  Build StrategyInputContext per market
 10.  For each market
      a. Start DecisionTrace
      b. Record DecisionInputSnapshot
      c. Evaluate strategy → StrategyReasoningTrace
      d. Generate Signal
      e. Evaluate RiskManager → RiskEvaluationTrace
      f. Create TradingDecision
      g. If Approved → execute TradeExecutionPipeline (paper)
      h. Record Trade, Position, PnLSnapshot
      i. Complete DecisionTrace
11.  Run analytics aggregation
12.  Complete ExecutionCycle
13.  Wait for next interval
```

### 5.4 Fluxo de Analytics (Phase 8)

```
Post-Settlement
  → RecordTradeOutcome (win/loss/push)
  → RecordDecisionOutcomeReview (compare predicted vs actual)
  → On demand or scheduled
      → AnalyzeStrategyPerformance
      → AnalyzeDecisionFactors
      → AnalyzeMissedOpportunities
      → GenerateSelfImprovementReport
```

---

## 6. Decisões Técnicas

### DT-01: .NET Version
**Decisão:** .NET 8.0 LTS.  
**Rationale:** .NET 10.0.100 disponível mas é preview. .NET 8 é LTS, suportado até 2026-11. .NET 12+ será considerado quando sair como LTS.  
**Alternativa descartada:** .NET 10 preview — não adequado para projeto de produção.

### DT-02: Minimal API vs Controllers
**Decisão:** Minimal API apenas.  
**Rationale:** Menos boilerplate, composition over inheritance, mapeamento direto para use cases. Controllers adicionam complexidade desnecessária para API interna.  
**Alternativa descartada:** Controllers MVC — mais código, mais abstraction layers.

### DT-03: Domain Independence
**Decisão:** Domain é zero-dependency (sem EF Core, sem HTTP, sem Redis).  
**Rationale:** Domain deve ser puro e testável sem infraestrutura. Isso força boundaries claras e facilita migration de infraestrutura.  
**Trade-off:** Necessita duplicar primitivos (ex: decimal vs Money) ou usar shared primitives library.

### DT-04: Paper Trading = Live Pipeline
**Decisão:** Paper trading usa o MESMO pipeline de execution que live usaria. Apenas o executor é diferente.  
**Rationale:** Evita "dual path" problem — código de paper nunca testa o mesmo código de live. O pipeline completo é exercitado em paper mode, garantindo que live mode funciona quando ativado.

### DT-05: DecisionTrace como Source of Truth
**Decisão:** DecisionTrace é a fonte primária de auditoria — logs textuais não substituem trace estruturado.  
**Rationale:** Permite analytics de self-improvement, permite replay de decisões, permite identificar padrões de erro. Logs são para debugging operacional, trace é para análise.

### DT-06: Polymarket Client = Stub First
**Decisão:** Client Polymarket Gamma API começa como stub/mocked. Integração real em MVP 6.  
**Rationale:** Não travar desenvolvimento esperando API externa. Design de contracts (interfaces) permite swap posterior. Stub retorna dados sintéticos mas realistas.

### DT-07: BTC/ETH Price Feed = Stub First
**Decisão:** Feed de preço BTC/ETH começa como stub. Integração real (Binance/Coinbase/CoinGecko) em MVP 6.  
**Rationale:** Mesma lógica do DT-06. Stub permite testar estratégia e trend/momentum analysis sem dependência externa.

### DT-08: EF Core Migrations
**Decisão:** Usar EF Core migrations com --template generate para criar migrations SQL manuais editáveis.  
**Rationale:** Migrations automáticas EF Core podem ser opacas. Migrations SQL manuais permitem review, tuning de índices, e rollback controlado. Para MVP com schema evolving, isso é importante.

### DT-09: FluentResults
**Decisão:** FluentResults para retorno de use cases.  
**Rationale:** Não usar throw para controle de fluxo. Result patterns permite discriminated unions de Success/Error com detalhes estruturados. Mapeia bem para HTTP responses em Minimal API.  
**Padrão:** `Result<T>`, `Result` (sem valor), `Result<T>.WithState()` para incluir metadata.

### DT-10: Redis como Cache Decorator
**Decisão:** Redis envolvido em decorator pattern: `ICacheService → RedisCacheDecorator → original implementation`.  
**Rationale:** Cache é primário, Redis é implementação. Interface exposta permite trocar Redis por in-memory em testes. Decorator adiciona cache sem mudar comportamento do serviço original.

---

## 7. Trade-offs

| Trade-off | Escolha | Impacto |
|---|---|---|
| PostgreSQL vs SQLite | PostgreSQL | Mais robusto, melhor para produção, mas mais setup local |
| EF Core vs Dapper | EF Core + raw SQL para queries complexas | EF Core para CRUD simples, raw SQL para analytics |
| Redis vs Memory | Redis | Mais complexo para setup, mas compartilhado entre API e Worker |
| Value Objects vs primitives | Value Objects | Mais código inicial, mais type safety, validação centralizada |
| DecisionTrace em todas as decisões | Sim | Overhead de写入, mas valor analítico altíssimo |
| Polymarket stub vs real API | Stub | Desenvolvimento não bloqueado, mas estratégia testada com dados reais depois |
| Strategy pattern vs hardcoded | Strategy pattern | Mais complexo, mas permite múltiplas estratégias no futuro |

---

## 8. Riscos

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| Stub de Polymarket divergindo da API real | Alta | Média | Contract interfaces definidos agora; swap fácil |
| Schema migrations causing downtime | Média | Alta | Manual SQL migrations, rollback tested |
| DecisionTrace overhead afetando performance | Baixa | Média | Async write, batching de writes |
| Complexidade de StopLoss em paper mode | Média | Alta | Simulação de sell no best_bid atual, não no preço de entrada |
| Redis unavailability em produção | Baixa | Alta | In-memory fallback, circuit breaker pattern |
| .NET version mismatch (8 vs 12) | Baixa | Baixa | .NET 8 LTS, pode migrar depois |
| Worker crash affecting all cycles | Média | Alta | CancellationToken em tudo, exception handling por ciclo |
| Overfitting da estratégia 99c em paper | Alta | Alta | Analytics robusto, missed opportunity tracking |

---

## 9. Plano por Fases

### Fase 0 — Discovery e Design ✅
- [x] Inspecionar repositório Python de referência
- [x] Mapear módulos e responsabilidades
- [x] Identificar partes de Kalshi (descartadas)
- [x] Criar mapa Python → .NET
- [x] Listar migração conceitual vs fora do MVP
- [x] Identificar riscos técnicos
- [x] Definir decisões arquiteturais
- [x] **Este documento = entrega da Fase 0**

**Entregáveis:**
- `.specs/project/PROJECT.md` — Visão e objetivos
- `.specs/project/ROADMAP.md` — Features e milestones
- `.specs/features/[feature]/discovery.md` — Problema, hipótese, métricas
- Este plano de implementação

### Fase 1 — Solution Base e Clean Architecture
- [x] Criar todos os projetos .NET 8
- [x] Definir dependencies entre projetos
- [x] Configurar Dependency Injection base
- [x] Minimal API com Swagger
- [x] Health checks (liveness + readiness)
- [x] Docker Compose: PostgreSQL + Redis
- [x] EF Core context + initial migration
- [x] Redis setup + decorator pattern
- [x] FluentResults configuration
- [x] .env.example
- [x] README inicial
- [x] **Critério de aceite:** Solution compila, API sobe, Worker sobe, Health check OK, Docker Compose sobe Postgres e Redis

### Fase 2 — Domínio Mínimo de Trading
- [x] Value Objects: Probability, Money, MarketPrice, Edge, PositionSize, StopLossThreshold, PnL
- [x] Enums: todos listados
- [x] Entidades: Market, MarketOutcome, Strategy, Signal, TradingDecision, Order, Trade, Position, StopLossEvent, BotRun, ExecutionCycle, PnLSnapshot
- [x] Entidade: Asset, AssetPriceSnapshot, AssetTrendSnapshot
- [x] Validations em Value Objects
- [x] Domain invariants (ex: Position não abre sem StopLossPrice)
- [x] Testes de domínio cobrindo validações principais
- [x] **Critério de aceite:** Domínio compila, Probability valida 0-1, StopLoss valida 0-1, Position não abre sem stop loss, testes passam

### Fase 3 — Paper Trading Realista
- [x] Abstrações: ITradeExecutionPipeline, IOrderExecutor, IPaperOrderExecutor, ILiveOrderExecutor, IOrderFillSimulator
- [x] Abstrações: ITradeRepository, IOrderRepository, IPositionRepository, IPnLCalculator
- [x] Implementação: TradeExecutionPipeline
- [x] Implementação: PaperOrderExecutor
- [x] Implementação: StubLiveOrderExecutor
- [x] Implementação: OrderFillSimulator (spread, liquidity, slippage, fee, rejections)
- [x] Fluxo completo de paper trading: Decision → Order → Fill → Trade → Position → PnL → Trace
- [x] Testes do pipeline de paper trading
- [x] **Critério de aceite:** Paper trade cria Order, simula fill, cria Trade, abre Position, calcula StopLossPrice, gera PnLSnapshot, Live permanece bloqueado

### Fase 4 — Stop Loss
- [x] Entidade: StopLossEvent
- [x] Abstrações: IStopLossService, IStopLossMonitor, IPositionMonitor, IExitTradeExecutor
- [x] Implementação: StopLossMonitor
- [x] Implementação: PaperExitTradeExecutor
- [x] Regra: StopLossPrice = EntryPrice × (1 - StopLossPercentage), padrão 20%
- [x] Regra: Se CurrentPrice ≤ StopLossPrice → stop acionado
- [x] Fluxo completo de stop loss (como em Phase 5.2)
- [x] Testes de stop loss (cálculo e acionamento)
- [x] **Critério de aceite:** Toda posição aberta tem StopLossPrice, Worker verifica antes de novas entradas, stop gera evento + trace + log + decisão + PnL

### Fase 5 — Market Trend e Momentum BTC/ETH
- [x] Entidades: Asset, AssetPriceSnapshot, AssetTrendSnapshot
- [x] Enums: AssetSymbol, TrendDirection, MomentumState, TrendTimeframe
- [x] Abstrações: IAssetPriceFeed, IMarketTrendAnalyzer, IMomentumAnalyzer, ITrendSignalProvider, IAssetTrendRepository
- [x] Implementação: StubAssetPriceFeed (synthetic data)
- [x] Implementação: TrendAnalyzer (RSI-like, momentum, SMA, VWAP — traduzido do Python)
- [x] TrendDecisionContext com ShouldAllowBullishTrade, ShouldAllowBearishTrade
- [x] Configurações: TrendTimeframe, MomentumState thresholds
- [x] Testes de trend/momentum analysis
- [x] **Critério de aceite:** Worker captura snapshots, cria AssetTrendSnapshot, estratégia consome TrendDecisionContext, estratégia pula trades contrários

### Fase 6 — Estratégia Inicial 99c
- [x] Entidade: HighPriceEntry99CStrategyEvaluator (use case)
- [x] Input: StrategyInputContext (market, outcome, btc_price, trend, momentum, spread, liquidity, volume)
- [x] Output: Signal com Action Buy/Sell/Hold/Skip
- [x] Regras de avaliação: EntryPriceThreshold, StopLossPercentage, RequireTrendAlignment
- [x] Regras de Skip: tendência contrária, spread alto, liquidez baixa, baixa confiança
- [x] Output: Structured reasoning (positive/negative/blocking factors)
- [x] Configurações via Settings
- [x] Paper trading apenas (EnableRealTrading = false)
- [x] Testes: estratégia gera Buy quando critérios batem, Skip quando tendência contrária
- [x] **Critério de aceite:** Estratégia gera Buy/Sell/Hold/Skip, registra motivo estruturado, usa tendência/momentum como insumo

### Fase 7 — Decision Trace Obrigatório
- [x] Entidades: DecisionTrace, DecisionTraceStep, DecisionInputSnapshot, RiskEvaluationTrace, StrategyReasoningTrace, DecisionOutcomeReview, StrategyDecisionFactor, MissedOpportunity
- [x] Abstrações: IDecisionTraceRepository
- [x] Implementação: DecisionTraceRepository
- [x] Pipeline: todos os passos conforme fluxo Phase 5.1 e 5.2
- [x] Skip tracing: DecisionTrace para skips também (para missed opportunities)
- [x] Integration no Worker e nos use cases
- [x] Endpoints de trace (GET /api/decision-traces/*)
- [x] Testes de trace creation
- [x] **Critério de aceite:** Toda decisão gera DecisionTrace, todo Skip gera DecisionTrace, RiskEvaluationTrace e StrategyReasoningTrace completos, Order/Trade/Position aponta para DecisionTrace

### Fase 8 — Analytics e Self-Improvement
- [x] Entidades: TradeOutcome, StrategyPerformanceSnapshot, DecisionFactorPerformance, MissedOpportunity, SelfImprovementReport
- [x] Abstrações: ITradeOutcomeAnalyzer, IStrategyPerformanceAnalyzer, IWinRateAnalyzer, ISelfImprovementAnalyzer, IBacktestDataExporter
- [x] Use cases: RecordTradeOutcome, AnalyzeTradeOutcomes, AnalyzeStrategyPerformance, AnalyzeDecisionFactors, AnalyzeMissedOpportunities, GenerateSelfImprovementReport, ExportBacktestDataset
- [x] SelfImprovementReport com conteúdo conforme especificação
- [x] Endpoints: GET /api/analytics/summary, /api/analytics/win-rate/by-strategy, /by-trend, /by-momentum, /self-improvement-report
- [x] MissedOpportunity tracking: trades que foram Skipados mas teriam vencido
- [x] Testes de analytics
- [x] **Critério de aceite:** Analytics usa DecisionTrace como fonte, permite análise por estratégia/tendência/momentum, relatório de self-improvement existe, estrutura de missed opportunities existe

### Fase 9 — API Minimal
- [x] Todos os endpoints conforme especificação (Health, Markets, Trading, StopLoss, DecisionTrace, Analytics)
- [x] Group organization por feature
- [x] FluentResults → HTTP mapping (200 OK, 400 BadRequest, 404 NotFound)
- [x] Swagger funcionando com todos os endpoints
- [x] Request/Response DTOs por endpoint
- [x] Testes de API (smoke tests)
- [x] **Critério de aceite:** Endpoints organizados, FluentResults mapeado, Swagger funcionando

### Fase 10 — Worker Orchestration
- [x] Worker com timer configurável
- [x] CancellationToken em todo o pipeline
- [x] Exception handling por ciclo (um ciclo não quebra o worker)
- [x] Logging estruturado (Serilog)
- [x] ExecutionCycle tracking
- [x] Integração de todos os componentes (trends, stop loss, scanner, strategy, execution, analytics)
- [x] Configurable intervals via settings
- [x] Testes de Worker (happy path, error handling)
- [x] **Critério de aceite:** Worker não quebra em falha de ciclo, usa CancellationToken, registra ExecutionCycle, verifica stop loss antes de entradas, registra traces

### Fase 11 — Banco e Persistência
- [x] EF Core configurations para todas as entidades
- [x] Migrations SQL manuais (ou via EF Core com review)
- [x] Índices: BotRunId, MarketId, StrategyId, TradeId, PositionId, CreatedAt
- [x] Campos MetadataJson para dados variáveis/experimentais
- [x] Relacionamentos verificados
- [x] Testes de persistência (repositories + EF Core)
- [x] **Critério de aceite:** Migrations aplicadas, schema correto, índices funcionais, relacionamentos mantidos

### Fase 12 — Testes
- [x] Domain Tests: Probability inválida, StopLossPrice, stop acionado, Position sem stop loss, PnL
- [x] Application Tests: estratégia 99c, RiskManager, paper pipeline, stop loss, DecisionTrace
- [x] Worker Tests: stop loss antes de entradas, continuação após erro, ExecutionCycle
- [x] API Tests: health, decision-traces, analytics/summary
- [x] Integration Tests: Docker Compose test fixture
- [x] **Critério de aceite:** Caminho feliz e principais rejeições cobertos, domínio e pipeline com coverage alto

### Fase 13 — Roadmap Incremental
- [x] MVP 1 a MVP 7 conforme especificação
- [x] Backlog técnico documentado
- [x] Critérios de aceite por fase
- [x] Lista de arquivos
- [x] Recomendações finais

---

## 10. Ordem Recomendada de Implementação

A implementação segue estritamente a ordem das fases (0 → 13). Cada fase depende da anterior. Recomendo não pular fases.

**Sequência de execução:**

```
Fase 0:  Discovery + Design          (este documento)
Fase 1:  Solution Base             (setup projeto, Docker, Swagger, Health)
Fase 2:  Domínio Mínimo             (entities, VOs, enums, validações)
Fase 3:  Paper Trading             (pipeline, fill simulator, repositories)
Fase 4:  Stop Loss                 (monitor, exit executor, events)
Fase 5:  Trend/Momentum           (BTC/ETH analysis, stub price feed)
Fase 6:  Estratégia 99c            (strategy evaluator, signals)
Fase 7:  Decision Trace            (trace entities, repository, endpoints)
Fase 8:  Analytics                (outcomes, performance, reports)
Fase 9:  API Minimal               (todos os endpoints)
Fase 10: Worker Orchestration     (orchestrator, worker loop)
Fase 11: Banco e Persistência     (migrations, configs, índices)
Fase 12: Testes                   (domínio, pipeline, API, Worker)
Fase 13: Roadmap + Backlog        (finalização do plano)
```

**MVP Rollup:**

```
MVP 1: Fase 1 + Fase 2
  → Solution compila, API sobe, Worker vazio, Docker Compose sobe

MVP 2: Fase 3 + Fase 4
  → Paper trading funciona, stop loss fecha posições, PnL calculado

MVP 3: Fase 5 + Fase 6
  → Polymarket client stub, market scanner, estratégia 99c, trend/momentum

MVP 4: Fase 7
  → DecisionTrace completo, trace endpoints, traces para trades e skips

MVP 5: Fase 8
  → Analytics de winrate, missed opportunities, self-improvement report

MVP 6: Fase 9 + Fase 10 + Fase 11
  → API completa, Worker orquestrando tudo, banco persistente

MVP 7: Fase 12
  → Testes, Docker Compose com integração testada
```

---

## 11. Backlog Técnico

| Item | Prioridade | Fase |
|---|---|---|
| Integração real Polymarket Gamma API | Alta | MVP 6 |
| Integração real Binance/Coinbase price feed | Alta | MVP 6 |
| Live trading unlock (feature flag removal) | Alta | MVP 6+ |
| Dashboard frontend React/TS | Média | MVP 7+ |
| Alertas (Discord/Slack/webhook) | Média | MVP 7+ |
| Backtest export (CSV/JSON dataset) | Média | MVP 6+ |
| Observabilidade avançada (Prometheus/Grafana) | Média | MVP 7+ |
| Multiple asset support (ETH, SOL markets) | Baixa | Backlog |
| Multiple strategy support | Baixa | Backlog |
| Take profit automation | Baixa | Backlog |
| Deploy (Docker, k8s, cloud) | Baixa | MVP 7+ |
| JWT/API key authentication para API | Baixa | Backlog |
| Performance optimization (batching, caching) | Baixa | Backlog |

---

## 12. Critérios de Aceite por Fase

Os critérios de aceite detalhados estão na Seção 9 (Plano por Fases). Resumo:

| Fase | Critério Principal |
|---|---|
| 0 | Este documento entregue e aprovado |
| 1 | Solution compila, API sobe, Docker Compose sobe PostgreSQL+Redis |
| 2 | Domínio compila, Probability valida 0-1, Position exige StopLoss |
| 3 | Paper trade: Order→Fill→Trade→Position→PnLSnapshot, Live bloqueado |
| 4 | Toda posição tem StopLossPrice, Worker verifica antes de entradas |
| 5 | TrendAnalysis gera AssetTrendSnapshot, estratégia usa TrendDecisionContext |
| 6 | Estratégia gera Buy/Sell/Hold/Skip com reasoning estruturado |
| 7 | Todo trade e skip gera DecisionTrace, endpoints funcionando |
| 8 | Analytics permite análise por estratégia/tendência/momentum |
| 9 | Todos os endpoints funcionam, Swagger OK, FluentResults mapeado |
| 10 | Worker Orchestrator completa sem quebrar em erros de ciclo |
| 11 | Schema aplicado, migrations SQL revisadas, índices funcionais |
| 12 | Testes cobrem domínio, pipeline, API, Worker |

---

## 13. Lista de Arquivos/Pastas a Criar

### Raiz
```
Polymarket.Bot.sln
README.md
.dockerignore
.env.example
docker-compose.yml
```

### src/Polymarket.Bot.Domain/
```
Polymarket.Bot.Domain.csproj
Entities/
  Market.cs
  MarketOutcome.cs
  Strategy.cs
  Signal.cs
  TradingDecision.cs
  Order.cs
  Trade.cs
  Position.cs
  StopLossEvent.cs
  BotRun.cs
  ExecutionCycle.cs
  PnLSnapshot.cs
  Asset.cs
  AssetPriceSnapshot.cs
  AssetTrendSnapshot.cs
ValueObjects/
  Probability.cs
  Money.cs
  MarketPrice.cs
  Edge.cs
  PositionSize.cs
  StopLossThreshold.cs
  PnL.cs
Enums/
  MarketStatus.cs
  SignalAction.cs
  TradingMode.cs
  TradingDecisionStatus.cs
  OrderStatus.cs
  OrderSide.cs
  TradeStatus.cs
  PositionStatus.cs
  ExitReason.cs
  StrategyType.cs
  AssetSymbol.cs
  TrendDirection.cs
  MomentumState.cs
  TrendTimeframe.cs
Domain/
  Services/
    PositionStopLossService.cs
    PnLCalculator.cs
```

### src/Polymarket.Bot.Application/
```
Polymarket.Bot.Application.csproj
UseCases/
  Markets/
    ScanMarkets.cs
    EvaluateMarket.cs
    GetMarket.cs
  Trading/
    PlaceOrder.cs
    SimulateFill.cs
    ExecutePaperTrade.cs
    ClosePosition.cs
  StopLoss/
    CheckStopLossForPosition.cs
    CheckStopLossForOpenPositions.cs
    TriggerStopLoss.cs
    GetStopLossEvents.cs
  Trends/
    CaptureAssetPriceSnapshot.cs
    AnalyzeAssetTrend.cs
    GetLatestAssetTrend.cs
    GetTrendDecisionContext.cs
  Strategy/
    EvaluateHighProb99CStrategy.cs
  Analytics/
    RecordTradeOutcome.cs
    AnalyzeTradeOutcomes.cs
    AnalyzeStrategyPerformance.cs
    AnalyzeDecisionFactors.cs
    AnalyzeMissedOpportunities.cs
    GenerateSelfImprovementReport.cs
    ExportBacktestDataset.cs
    GetWinRateByStrategy.cs
    GetWinRateByTrendDirection.cs
    GetWinRateByMomentumState.cs
  DecisionTrace/
    RecordDecisionTrace.cs
    GetDecisionTrace.cs
    GetDecisionTracesByMarket.cs
    GetFalsePositives.cs
    GetFalseNegatives.cs
DTOs/
  MarketDto.cs
  SignalDto.cs
  TradingDecisionDto.cs
  OrderDto.cs
  TradeDto.cs
  PositionDto.cs
  PnLDto.cs
  AssetTrendDto.cs
  AnalyticsSummaryDto.cs
  WinRateDto.cs
  DecisionTraceDto.cs
  SelfImprovementReportDto.cs
InputContexts/
  StrategyInputContext.cs
  TrendDecisionContext.cs
Abstractions/
  Interfaces/
    IOrderExecutor.cs
    IPaperOrderExecutor.cs
    ILiveOrderExecutor.cs
    IOrderFillSimulator.cs
    ITradeExecutionPipeline.cs
    IMarketScanner.cs
    IMarketRepository.cs
    IOrderRepository.cs
    ITradeRepository.cs
    IPositionRepository.cs
    IAssetPriceFeed.cs
    IMarketTrendAnalyzer.cs
    IMomentumAnalyzer.cs
    ITrendSignalProvider.cs
    IAssetTrendRepository.cs
    IStopLossService.cs
    IStopLossMonitor.cs
    IExitTradeExecutor.cs
    IStrategyEvaluator.cs
    ITradingSignalGenerator.cs
    IDecisionTraceRepository.cs
    ITradeOutcomeAnalyzer.cs
    IStrategyPerformanceAnalyzer.cs
    IWinRateAnalyzer.cs
    ISelfImprovementAnalyzer.cs
    IBacktestDataExporter.cs
    ICacheService.cs
    IPnLCalculator.cs
  Results/
    OrderPlacementResult.cs
    FillSimulationResult.cs
    TradeExecutionResult.cs
    StopLossCheckResult.cs
    StrategyEvaluationResult.cs
    AnalyticsResult.cs
Services/
  RiskManager.cs
  TradeLedgerService.cs
  PositionService.cs
  StopLossService.cs
```

### src/Polymarket.Bot.Infrastructure/
```
Polymarket.Bot.Infrastructure.csproj
Persistence/
  DbContext/
    PolymarketDbContext.cs
  Configurations/
    MarketConfiguration.cs
    MarketOutcomeConfiguration.cs
    StrategyConfiguration.cs
    SignalConfiguration.cs
    TradingDecisionConfiguration.cs
    OrderConfiguration.cs
    TradeConfiguration.cs
    PositionConfiguration.cs
    StopLossEventConfiguration.cs
    BotRunConfiguration.cs
    ExecutionCycleConfiguration.cs
    PnLSnapshotConfiguration.cs
    AssetPriceSnapshotConfiguration.cs
    AssetTrendSnapshotConfiguration.cs
    DecisionTraceConfiguration.cs
    DecisionTraceStepConfiguration.cs
    DecisionInputSnapshotConfiguration.cs
    RiskEvaluationTraceConfiguration.cs
    StrategyReasoningTraceConfiguration.cs
    DecisionOutcomeReviewConfiguration.cs
    StrategyDecisionFactorConfiguration.cs
    MissedOpportunityConfiguration.cs
    TradeOutcomeConfiguration.cs
    StrategyPerformanceSnapshotConfiguration.cs
    DecisionFactorPerformanceConfiguration.cs
  Repositories/
    MarketRepository.cs
    OrderRepository.cs
    TradeRepository.cs
    PositionRepository.cs
    DecisionTraceRepository.cs
    AssetTrendRepository.cs
Cache/
  RedisCacheService.cs
  RedisCacheDecorator.cs
External/
  PolymarketClient/
    IPolymarketClient.cs
    StubPolymarketClient.cs
    GammaApiPolymarketClient.cs
    Models/
      PolymarketMarketDto.cs
      PolymarketOrderBookDto.cs
  CryptoPriceFeed/
    IAssetPriceFeedClient.cs
    StubCryptoPriceFeedClient.cs
    BinanceCryptoPriceFeedClient.cs
    Models/
      CryptoPriceDto.cs
      KlinesDto.cs
Execution/
  PaperOrderExecutor.cs
  StubLiveOrderExecutor.cs
  OrderFillSimulator.cs
  TradeExecutionPipeline.cs
  PaperExitTradeExecutor.cs
  RealExitTradeExecutor.cs
Infrastructure/
  Mapping/
    EntityToDtoMapper.cs
```

### src/Polymarket.Bot.Api/
```
Polymarket.Bot.Api.csproj
Program.cs
MinimalApiExtensions/
  FluentResultsExtensions.cs
  HealthCheckExtensions.cs
Endpoints/
  HealthEndpoints.cs
  MarketsEndpoints.cs
  TradingEndpoints.cs
  StopLossEndpoints.cs
  DecisionTraceEndpoints.cs
  AnalyticsEndpoints.cs
```

### src/Polymarket.Bot.Worker/
```
Polymarket.Bot.Worker.csproj
Program.cs
Worker.cs
Services/
  TradingOrchestrator.cs
  StopLossMonitor.cs
  MarketScanner.cs
  TrendAnalyzer.cs
  AnalyticsCollector.cs
  ExecutionCycleTracker.cs
Configuration/
  WorkerSettings.cs
```

### tests/
```
tests/
  Polymarket.Bot.Domain.Tests/
    Polymarket.Bot.Domain.Tests.csproj
    ValueObjects/
      ProbabilityTests.cs
      StopLossThresholdTests.cs
      PnLTests.cs
    Entities/
      PositionTests.cs
  Polymarket.Bot.Application.Tests/
    Polymarket.Bot.Application.Tests.csproj
    UseCases/
      PaperTradingPipelineTests.cs
      StopLossServiceTests.cs
      HighProb99CEvaluationTests.cs
      RiskManagerTests.cs
      DecisionTraceTests.cs
  Polymarket.Bot.Infrastructure.Tests/
    Polymarket.Bot.Infrastructure.Tests.csproj
    Persistence/
      RepositoryTests.cs
    Cache/
      RedisCacheTests.cs
  Polymarket.Bot.Api.Tests/
    Polymarket.Bot.Api.Tests.csproj
    Endpoints/
      HealthEndpointTests.cs
      DecisionTraceEndpointTests.cs
      AnalyticsEndpointTests.cs
  Polymarket.Bot.Worker.Tests/
    Polymarket.Bot.Worker.Tests.csproj
    WorkerTests.cs
    OrchestratorTests.cs
```

### .specs/
```
.specs/
  project/
    PROJECT.md
    ROADMAP.md
    STATE.md
    DECISIONS.md
  features/
    paper-trading/
      discovery.md
      spec.md
      design.md
      tasks.md
    strategy-99c/
      discovery.md
      spec.md
      design.md
      tasks.md
    decision-trace/
      discovery.md
      spec.md
      design.md
      tasks.md
    analytics/
      discovery.md
      spec.md
      design.md
      tasks.md
```

---

## 14. Recomendações Finais

**1. Não antecipe complexidade.** Cada fase tem critério de aceite claro. Implemente só o necessário para passar no critério. Evite overengineering — o domínio de trading é complexo por natureza, não adicione complexidade acidental.

**2. Stub first, real later.** Os clients de Polymarket e Crypto Price Feed devem ser stubs com dados sintéticos mas realistas. Isso permite desenvolvimento paralelo e testing sem dependência externa. A integração real vem depois.

**3. DecisionTrace é investimento, não overhead.** Parece que cria muito código, mas é a base de analytics, self-improvement e debugging de decisões. Sem trace estruturado, não há como melhorar a estratégia.

**4. Teste o pipeline completo em cada fase.** Não teste apenas units isoladas. Teste o fluxo end-to-end: Signal → Decision → Order → Fill → Trade → Position → PnL. Testes de integração com Docker Compose são essenciais.

**5. Value Objects desde o início.** Probability, Money, StopLossThreshold — valide na fronteira, não no meio do código. Isso previne bugs sutis (ex: probability negativa, stop loss > 1).

**6. Serilog para logs estruturados.** Structured logging + correlation IDs (ExecutionCycleId, DecisionTraceId) permite tracing completo de um ciclo de trading. Logs textuais plain não são suficientes.

**7. Feature flags para tudo.** EnablePaperTrading, EnableRealTrading, EnableStopLoss, EnableTrendAlignment — tudo configurável via settings. Permite activar/desativar features sem deploy.

**8. O Worker deve sobreviver a falhas de ciclo.** Cada ciclo é isolado. Um mercado que falha não deve quebrar o próximo ciclo. CancellationToken em tudo.

**9. Revisitar a decisão de .NET 12.** Quando .NET 12 LTS sair, avaliar migração. O código deve ser compatível — evitar APIs preview.

**10. Analytics melhoram a estratégia.** O loop de self-improvement é o diferenciador. MissedOpportunity tracking é crítico — o bot aprende tanto dos trades que fez quanto dos que pulou.

---

## 15. Mapa Python → .NET (Reference)

| Conceito Python | Conceito .NET | Observação |
|---|---|---|
| `BtcMarket` dataclass | `Market` entity + `MarketOutcome` | Split em Market + Outcomes |
| `TradingSignal` | `Signal` entity + `StrategyEvaluationResult` DTO | Signal = persistido, Result = transient |
| `HighProbTradingSignal` | `Signal` + `StrategyInputContext` | Mesma entidade, contexto diferente |
| `TradeExecutionResult` | `OrderPlacementResult` + `TradeExecutionResult` | Separar placement de execution |
| `TradingExecutor` | `TradeExecutionPipeline` + `PaperOrderExecutor` | Pipeline composable |
| `SimulationExecutionAdapter` | `OrderFillSimulator` + `PaperExitTradeExecutor` | Simulação isolada |
| `RealExecutionAdapter` | `LiveOrderExecutor` (stub, bloqueado) | Live bloqueado no MVP |
| `Endgame99cStrategy` | `HighPriceEntry99CStrategyEvaluator` use case | Use case .NET |
| `OrderBookSnapshot` | `MarketPrice` VO + `IOrderBookClient` | Separar VO do client |
| `StopLossExitResult` | `StopLossExitResult` DTO | Mesmo conceito |
| `SimulationStopLossExitAdapter` | `PaperExitTradeExecutor` | Mesma semântica |
| `RiskState` | `RiskManager` + `RiskEvaluationTrace` | Estado vira trace |
| `EndgameCandidate` | `StrategyInputContext` | Contexto rico de input |
| `ExecutionResult` | `FillSimulationResult` + `TradeExecutionResult` | Separar simulação de execução |
| `Signal` model (SQLAlchemy) | `Signal` entity | 1:1 mapeamento |
| `Trade` model | `Trade` entity + `Order` entity | Trade = execução, Order = ordem |
| `BotState` model | `BotRun` + `ExecutionCycle` + `PnLSnapshot` | Mais granular |
| `EndgameTradeLog` model | `DecisionTrace` + `DecisionTraceStep` | Trace mais rico |
| `Settings` (Pydantic) | `Settings` pattern (Options) | appsettings.json + strongly-typed |
| `config.py` | `appsettings.json` + `WorkerSettings.cs` + `PolymarketSettings.cs` | Configuração tipada |

---

**Plano concluído. Pronto para execução fase a fase.**
