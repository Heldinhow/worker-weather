# Polymarket.Bot

**Vision:** Um bot de trading paper para Polymarket que monitora mercados, executa estratégias baseadas em entrada 99c, aplica stop loss obrigatório, analisa tendências BTC/ETH e rastreia todas as decisões para auto-melhoria contínua via analytics de winrate.

**For:** Desenvolvedores e traders que querem uma base .NET para operar mercados de previsão na Polymarket em paper trading mode, com pipeline realista que pode ser ativado para live trading no futuro.

**Solves:** Falta de uma base .NET estruturada para trading em Polymarket com paper trading funcional, rastreamento completo de decisões, analytics de performance e estratégia 99c baseada em edge.

## Goals

- MVP funcional com paper trading que simula o fluxo real de trading — ordem → fill → trade → posição → PnL → stop loss
- Domínio puro de trading sem dependência de frameworks (Clean Architecture)
- DecisionTrace estruturado para toda decisão (trade aprovado ou skipado)
- Analytics de winrate por estratégia, tendência, momentum e fatores de decisão
- Worker Service orchestrando ciclos periódicos com error handling robusto
- Estratégia 99c com avaliação de tendência/momentum BTC/ETH como filtro
- Feature flag para enable/disable de paper trading e live trading

## Tech Stack

**Core:**

- Framework: .NET 8.0 LTS
- Language: C# 12
- Database: PostgreSQL 16 + EF Core 8
- Cache: Redis 7

**Key dependencies:**

- FluentResults (result pattern)
- Serilog (structured logging)
- xUnit + Moq (testing)
- Swagger/OpenAPI (Minimal API)
- Health Checks (liveness + readiness)

## Scope

**v1 includes:**

- Solution .NET 8 com Clean Architecture (Domain, Application, Infrastructure, Api, Worker)
- Docker Compose com PostgreSQL + Redis
- Domínio mínimo de trading (Market, Order, Trade, Position, PnL, Signal, DecisionTrace)
- Value Objects com validação (Probability, Money, MarketPrice, Edge, StopLossThreshold)
- Paper trading pipeline completo (mesmo pipeline que live usaria, executor simulado)
- Stop loss obrigatório como parte do pipeline
- Estratégia 99c com Trend/Momentum BTC/ETH como filtro
- DecisionTrace estruturado para toda decisão
- Analytics de winrate (por estratégia, tendência, momentum, fator de decisão)
- Minimal API com Swagger + health checks
- Worker Service com orchestration de ciclos
- Testes unitários (domínio, pipeline, API)

**Explicitly out of scope:**

- Integração real com Polymarket Gamma API (stub inicialmente)
- Integração real com Binance/Coinbase/CoinGecko (stub inicialmente)
- Live trading real (bloqueado por feature flag)
- Frontend/dashboard React
- Kalshi (qualquer menção foi removida)
- Controllers MVC
- Deploy em produção

## Constraints

- Timeline: Implementação incremental por MVPs
- Technical: .NET 8 LTS (não .NET 10 preview), Clean Architecture estrita
- Resources: Execução fase a fase, cada fase com critério de aceite claro
- Architecture: Domain zero-dependency, Application não referencia Infrastructure diretamente
