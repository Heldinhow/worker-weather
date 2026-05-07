# Polymarket.Bot

Bot de trading paper para Polymarket com .NET 8 + Clean Architecture.

## Stack

- **.NET 8.0 LTS** — C# 12
- **Clean Architecture** — Domain → Application → Infrastructure → Api/Worker
- **PostgreSQL 16** + **EF Core 8**
- **Redis 7**
- **Minimal API** + **Swagger** + **Health Checks**
- **Serilog** para logging estruturado
- **xUnit** + **FluentAssertions** + **Moq** para testes

## Estrutura

```
src/
├── Polymarket.Bot.Domain/      # Entidades, Value Objects, Enums (zero dependency)
├── Polymarket.Bot.Application/ # Use Cases, DTOs, Abstrações
├── Polymarket.Bot.Infrastructure/ # EF Core, Redis, Clients
├── Polymarket.Bot.Api/         # Minimal API endpoints
└── Polymarket.Bot.Worker/      # Background service (ciclos de trading)

tests/
├── Polymarket.Bot.Domain.Tests/
├── Polymarket.Bot.Application.Tests/
├── Polymarket.Bot.Infrastructure.Tests/
├── Polymarket.Bot.Api.Tests/
└── Polymarket.Bot.Worker.Tests/
```

## Quick Start

```bash
# 1. Subir banco e Redis
docker compose up -d

# 2. Configurar ambiente
cp .env.example .env

# 3. Restore e build
dotnet restore
dotnet build

# 4. Rodar API
dotnet run --project src/Polymarket.Bot.Api

# 5. Rodar Worker
dotnet run --project src/Polymarket.Bot.Worker

# 6. Rodar testes
dotnet test
```

## API

| Endpoint | Descrição |
|---|---|
| `GET /health` | Health check |
| `GET /swagger` | Swagger UI |

Mais endpoints serão adicionados nas próximas fases.

## Estratégia

### High Probability 99c (MVP 3+)

- Monitora mercados Polymarket com preço entre 95c-99c
- Consome análise de tendência/momentum BTC/ETH
- Stop loss obrigatório (padrão 20%)
- Paper trading por padrão
- Live trading bloqueado por feature flag

### Pipeline de Paper Trading

```
Signal → TradingDecision → Order → Fill (simulado) → Trade → Position → PnL → DecisionTrace
```

### Stop Loss

```
Worker Cycle
  → Verifica posições abertas
  → Atualiza preço atual
  → Se CurrentPrice <= StopLossPrice
      → Trigger stop loss
      → Simula exit no best_bid
      → Fecha posição
      → Registra StopLossEvent + PnL
```

## Roadmap

Ver [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md) para plano completo.

## Disclaimer

Este é um **simulador paper trading** para fins educacionais. Não coloca trades reais. Performance passada em simulação não garante resultados futuros.
