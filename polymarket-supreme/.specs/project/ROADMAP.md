# Polymarket.Bot — Roadmap

## MVP 1: Solution Base + Clean Architecture
**Fases:** 1
**Entrega:** Solution compila, API sobe, Docker Compose sobe PostgreSQL + Redis, health checks OK

- [ ] Criar todos os projetos .NET 8
- [ ] Definir dependencies entre projetos (Layered Architecture)
- [ ] Configurar Dependency Injection base
- [ ] Minimal API com Swagger
- [ ] Health checks (liveness + readiness)
- [ ] Docker Compose: PostgreSQL + Redis
- [ ] EF Core context + initial migration
- [ ] Redis setup + decorator pattern
- [ ] FluentResults configuration
- [ ] .env.example
- [ ] README inicial

## MVP 2: Domínio Mínimo + Paper Trading + Stop Loss
**Fases:** 2, 3, 4
**Entrega:** Paper trade funcional, stop loss fecha posições, PnL calculado

### Fase 2 — Domínio Mínimo de Trading
- [ ] Value Objects: Probability, Money, MarketPrice, Edge, PositionSize, StopLossThreshold, PnL
- [ ] Enums: MarketStatus, SignalAction, TradingMode, TradingDecisionStatus, OrderStatus, OrderSide, TradeStatus, PositionStatus, ExitReason, StrategyType
- [ ] Entidades: Market, MarketOutcome, Strategy, Signal, TradingDecision, Order, Trade, Position, StopLossEvent, BotRun, ExecutionCycle, PnLSnapshot
- [ ] Entidades: Asset, AssetPriceSnapshot, AssetTrendSnapshot
- [ ] Validations em Value Objects
- [ ] Domain invariants (Position não abre sem StopLossPrice)
- [ ] Testes de domínio

### Fase 3 — Paper Trading Realista
- [ ] Abstrações: ITradeExecutionPipeline, IOrderExecutor, IPaperOrderExecutor, ILiveOrderExecutor, IOrderFillSimulator
- [ ] Abstrações: ITradeRepository, IOrderRepository, IPositionRepository, IPnLCalculator
- [ ] Implementação: TradeExecutionPipeline
- [ ] Implementação: PaperOrderExecutor
- [ ] Implementação: StubLiveOrderExecutor
- [ ] Implementação: OrderFillSimulator (spread, liquidity, slippage, fee, rejections)
- [ ] Fluxo completo: Decision → Order → Fill → Trade → Position → PnL → Trace
- [ ] Testes do pipeline

### Fase 4 — Stop Loss
- [ ] Entidade: StopLossEvent
- [ ] Abstrações: IStopLossService, IStopLossMonitor, IExitTradeExecutor
- [ ] Implementação: StopLossMonitor
- [ ] Implementação: PaperExitTradeExecutor
- [ ] Regra: StopLossPrice = EntryPrice × (1 - StopLossPercentage), padrão 20%
- [ ] Fluxo completo: trigger → simulate exit → close position → event → PnL update
- [ ] Testes

## MVP 3: Polymarket Client Stub + Estratégia 99c + Trend/Momentum
**Fases:** 5, 6
**Entrega:** Polymarket client stub, scanner, estratégia 99c, trend analysis BTC/ETH

### Fase 5 — Market Trend e Momentum BTC/ETH
- [ ] Entidades: Asset, AssetPriceSnapshot, AssetTrendSnapshot
- [ ] Enums: AssetSymbol, TrendDirection, MomentumState, TrendTimeframe
- [ ] Abstrações: IAssetPriceFeed, IMarketTrendAnalyzer, IMomentumAnalyzer
- [ ] Implementação: StubAssetPriceFeed (synthetic data)
- [ ] Implementação: TrendAnalyzer (RSI-like, momentum, SMA, VWAP)
- [ ] TrendDecisionContext com ShouldAllowBullishTrade, ShouldAllowBearishTrade
- [ ] Testes de trend/momentum

### Fase 6 — Estratégia Inicial 99c
- [ ] HighPriceEntry99CStrategyEvaluator use case
- [ ] Input: StrategyInputContext (market, outcome, btc_price, trend, momentum, spread, liquidity)
- [ ] Output: Signal com Action Buy/Sell/Hold/Skip com reasoning estruturado
- [ ] Regras: EntryPriceThreshold=0.99, StopLossPercentage=0.20, RequireTrendAlignment=true
- [ ] Skip reasons: tendência contrária, spread > 1%, liquidez < $25, confiança < 0.5
- [ ] Configurações via settings
- [ ] Paper trading apenas
- [ ] Testes: Buy quando critérios batem, Skip quando tendência contrária

## MVP 4: Decision Trace Completo
**Fase:** 7
**Entrega:** Todo trade e skip gera DecisionTrace, endpoints funcionando

- [ ] Entidades: DecisionTrace, DecisionTraceStep, DecisionInputSnapshot, RiskEvaluationTrace, StrategyReasoningTrace, DecisionOutcomeReview, StrategyDecisionFactor, MissedOpportunity
- [ ] Abstrações: IDecisionTraceRepository
- [ ] Implementação: DecisionTraceRepository
- [ ] Pipeline: todos os passos de trace
- [ ] Skip tracing (MissedOpportunity)
- [ ] Integração no Worker e nos use cases
- [ ] Endpoints: GET /api/decision-traces/*
- [ ] Testes de trace

## MVP 5: Analytics e Self-Improvement
**Fase:** 8
**Entrega:** Analytics permite análise por estratégia/tendência/momentum, relatório de self-improvement

- [ ] Entidades: TradeOutcome, StrategyPerformanceSnapshot, DecisionFactorPerformance, MissedOpportunity, SelfImprovementReport
- [ ] Abstrações: ITradeOutcomeAnalyzer, IStrategyPerformanceAnalyzer, IWinRateAnalyzer, ISelfImprovementAnalyzer
- [ ] Use cases: RecordTradeOutcome, AnalyzeTradeOutcomes, AnalyzeStrategyPerformance, AnalyzeDecisionFactors, AnalyzeMissedOpportunities, GenerateSelfImprovementReport
- [ ] SelfImprovementReport com: winrate total, PnL total, estratégias performance, fatores wins/losses, recomendações
- [ ] Endpoints: GET /api/analytics/summary, /win-rate/by-strategy, /by-trend, /self-improvement-report
- [ ] Testes de analytics

## MVP 6: API Completa + Worker Orchestrator + Banco Persistente
**Fases:** 9, 10, 11
**Entrega:** API completa, Worker orchestrando tudo, banco PostgreSQL persistente

### Fase 9 — API Minimal
- [ ] Endpoints: Health, Markets, Trading, StopLoss, DecisionTrace, Analytics
- [ ] FluentResults → HTTP mapping
- [ ] Swagger com todos os endpoints
- [ ] DTOs por endpoint
- [ ] Testes de API

### Fase 10 — Worker Orchestration
- [ ] Worker com timer configurável
- [ ] CancellationToken em todo pipeline
- [ ] Exception handling por ciclo
- [ ] Serilog structured logging
- [ ] ExecutionCycle tracking
- [ ] Integração: trends + stop loss + scanner + strategy + execution + analytics
- [ ] Testes de Worker

### Fase 11 — Banco e Persistência
- [ ] EF Core configurations para todas as entidades
- [ ] Migrations SQL manuais
- [ ] Índices: BotRunId, MarketId, StrategyId, TradeId, PositionId, CreatedAt
- [ ] Campos MetadataJson
- [ ] Testes de persistência

## MVP 7: Testes + Docker Compose
**Fase:** 12
**Entrega:** Testes cobrindo domínio, pipeline, API, Worker; Docker Compose testável

- [ ] Domain Tests
- [ ] Application Tests
- [ ] Infrastructure Tests
- [ ] API Tests
- [ ] Worker Tests
- [ ] Integration Tests com Docker Compose
- [ ] Test coverage report

## Milestones

| Milestone | Entrega | Critério de Aceite |
|---|---|---|
| M1: Solution Base | Dia 1 | `dotnet build` passa, `docker compose up` sobe DB+Redis |
| M2: Domínio Funcional | Dia 2 | Value objects validam, Position exige stop loss, domain tests passam |
| M3: Paper Trading | Dia 3 | Order→Fill→Trade→Position→PnL funciona end-to-end |
| M4: Stop Loss | Dia 4 | Stop loss fecha posição, gera evento e PnL update |
| M5: Estratégia + Trend | Dia 5 | Estratégia 99c gera Buy/Sell/Skip, trend analysis funciona |
| M6: DecisionTrace | Dia 6 | Todo trade e skip gera trace, endpoints funcionam |
| M7: Analytics | Dia 7 | Winrate analysis funciona, self-improvement report gera |
| M8: API + Worker | Dia 8 | API completa, Worker orchestrator roda ciclos |
| M9: Testes | Dia 9 | Testes passam, Docker Compose integration OK |

## Backlog (pós-MVP)

- Integração real Polymarket Gamma API
- Integração real Binance/Coinbase price feed
- Live trading unlock
- Dashboard frontend React/TS
- Alertas (Discord/Slack/webhook)
- Backtest export (CSV/JSON)
- Observabilidade (Prometheus/Grafana)
- Deploy (Docker, k8s)
