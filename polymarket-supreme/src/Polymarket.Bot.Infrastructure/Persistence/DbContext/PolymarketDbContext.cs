using Microsoft.EntityFrameworkCore;
using Polymarket.Bot.Domain.Entities;
using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Infrastructure.Persistence.DbContext;

public class PolymarketDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DbSet<Market> Markets => Set<Market>();
    public DbSet<MarketOutcome> MarketOutcomes => Set<MarketOutcome>();
    public DbSet<Strategy> Strategies => Set<Strategy>();
    public DbSet<Signal> Signals => Set<Signal>();
    public DbSet<TradingDecision> TradingDecisions => Set<TradingDecision>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<StopLossEvent> StopLossEvents => Set<StopLossEvent>();
    public DbSet<BotRun> BotRuns => Set<BotRun>();
    public DbSet<ExecutionCycle> ExecutionCycles => Set<ExecutionCycle>();
    public DbSet<PnLSnapshot> PnlSnapshots => Set<PnLSnapshot>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetPriceSnapshot> AssetPriceSnapshots => Set<AssetPriceSnapshot>();
    public DbSet<AssetTrendSnapshot> AssetTrendSnapshots => Set<AssetTrendSnapshot>();
    public DbSet<DecisionTrace> DecisionTraces => Set<DecisionTrace>();
    public DbSet<DecisionTraceStep> DecisionTraceSteps => Set<DecisionTraceStep>();
    public DbSet<DecisionInputSnapshot> DecisionInputSnapshots => Set<DecisionInputSnapshot>();
    public DbSet<RiskEvaluationTrace> RiskEvaluationTraces => Set<RiskEvaluationTrace>();
    public DbSet<StrategyReasoningTrace> StrategyReasoningTraces => Set<StrategyReasoningTrace>();
    public DbSet<DecisionOutcomeReview> DecisionOutcomeReviews => Set<DecisionOutcomeReview>();
    public DbSet<MissedOpportunity> MissedOpportunities => Set<MissedOpportunity>();
    public DbSet<StrategyPerformanceSnapshot> StrategyPerformanceSnapshots => Set<StrategyPerformanceSnapshot>();
    public DbSet<TradeOutcome> TradeOutcomes => Set<TradeOutcome>();

    public PolymarketDbContext(DbContextOptions<PolymarketDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Market
        modelBuilder.Entity<Market>(e =>
        {
            e.ToTable("markets");
            e.HasKey(m => m.Id);
            e.Property(m => m.MarketId).HasMaxLength(256).IsRequired();
            e.Property(m => m.Slug).HasMaxLength(512);
            e.Property(m => m.Question).HasMaxLength(2000);
            e.Property(m => m.Platform).HasMaxLength(50).HasDefaultValue("polymarket");
            e.HasIndex(m => m.MarketId);
            e.HasIndex(m => m.Slug);
            e.HasIndex(m => m.Status);
            e.HasMany(m => m.Outcomes).WithOne(o => o.Market).HasForeignKey(o => o.MarketId);
        });

        // MarketOutcome
        modelBuilder.Entity<MarketOutcome>(e =>
        {
            e.ToTable("market_outcomes");
            e.HasKey(o => o.Id);
            e.Property(o => o.OutcomeId).HasMaxLength(256);
            e.Property(o => o.Name).HasMaxLength(256);
            e.Property(o => o.Price).HasPrecision(18, 8);
            e.Property(o => o.BestBid).HasPrecision(18, 8);
            e.Property(o => o.BestAsk).HasPrecision(18, 8);
            e.Property(o => o.Liquidity).HasPrecision(18, 2);
            e.Property(o => o.Volume24h).HasPrecision(18, 2);
            e.HasIndex(o => o.OutcomeId);
        });

        // Strategy
        modelBuilder.Entity<Strategy>(e =>
        {
            e.ToTable("strategies");
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).HasMaxLength(256).IsRequired();
            e.Property(s => s.ParametersJson).HasColumnType("jsonb");
            e.HasIndex(s => s.Type);
        });

        // Signal
        modelBuilder.Entity<Signal>(e =>
        {
            e.ToTable("signals");
            e.HasKey(s => s.Id);
            e.Property(s => s.Reasoning).HasColumnType("text");
            e.Property(s => s.SkipReason).HasColumnType("text");
            e.Property(s => s.ModelProbability).HasPrecision(18, 8);
            e.Property(s => s.MarketProbability).HasPrecision(18, 8);
            e.Property(s => s.Edge).HasPrecision(18, 8);
            e.Property(s => s.Confidence).HasPrecision(18, 4);
            e.Property(s => s.SuggestedSize).HasPrecision(18, 2);
            e.HasIndex(s => s.MarketId);
            e.HasIndex(s => s.StrategyId);
            e.HasIndex(s => s.GeneratedAt);
        });

        // TradingDecision
        modelBuilder.Entity<TradingDecision>(e =>
        {
            e.ToTable("trading_decisions");
            e.HasKey(d => d.Id);
            e.Property(d => d.RejectionReason).HasColumnType("text");
            e.HasIndex(d => d.SignalId);
            e.HasIndex(d => d.Status);
        });

        // Order
        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(o => o.Id);
            e.Property(o => o.OrderId).HasMaxLength(256);
            e.Property(o => o.MarketId).HasMaxLength(256);
            e.Property(o => o.OutcomeId).HasMaxLength(256);
            e.Property(o => o.Price).HasPrecision(18, 8);
            e.Property(o => o.RequestedPrice).HasPrecision(18, 8);
            e.Property(o => o.FilledPrice).HasPrecision(18, 8);
            e.Property(o => o.Slippage).HasPrecision(18, 8);
            e.Property(o => o.Notional).HasPrecision(18, 2);
            e.Property(o => o.RejectReason).HasColumnType("text");
            e.HasIndex(o => o.OrderId);
            e.HasIndex(o => o.TradingDecisionId);
        });

        // Trade
        modelBuilder.Entity<Trade>(e =>
        {
            e.ToTable("trades");
            e.HasKey(t => t.Id);
            e.Property(t => t.MarketId).HasMaxLength(256);
            e.Property(t => t.MarketTicker).HasMaxLength(256);
            e.Property(t => t.EventSlug).HasMaxLength(512);
            e.Property(t => t.EntryPrice).HasPrecision(18, 8);
            e.Property(t => t.Size).HasPrecision(18, 2);
            e.Property(t => t.Shares).HasPrecision(18, 4);
            e.Property(t => t.SettlementValue).HasPrecision(18, 8);
            e.Property(t => t.MarketType).HasMaxLength(100);
            e.HasIndex(t => t.MarketId);
            e.HasIndex(t => t.OrderId);
            e.HasIndex(t => t.PositionId);
            e.HasIndex(t => t.Settled);
        });

        // Position
        modelBuilder.Entity<Position>(e =>
        {
            e.ToTable("positions");
            e.HasKey(p => p.Id);
            e.Property(p => p.MarketTicker).HasMaxLength(256);
            e.Property(p => p.EntryPrice).HasPrecision(18, 8);
            e.Property(p => p.StopLossPrice).HasPrecision(18, 8);
            e.Property(p => p.CurrentPrice).HasPrecision(18, 8);
            e.Property(p => p.Shares).HasPrecision(18, 4);
            e.Property(p => p.Size).HasPrecision(18, 2);
            e.Property(p => p.MetadataJson).HasColumnType("jsonb");
            e.HasIndex(p => p.MarketId);
            e.HasIndex(p => p.Status);
            e.HasIndex(p => p.OpenedAt);
        });

        // StopLossEvent
        modelBuilder.Entity<StopLossEvent>(e =>
        {
            e.ToTable("stop_loss_events");
            e.HasKey(s => s.Id);
            e.Property(s => s.EntryPrice).HasPrecision(18, 8);
            e.Property(s => s.StopLossPrice).HasPrecision(18, 8);
            e.Property(s => s.ExitPrice).HasPrecision(18, 8);
            e.Property(s => s.Shares).HasPrecision(18, 4);
            e.Property(s => s.Size).HasPrecision(18, 2);
            e.Property(s => s.Slippage).HasPrecision(18, 8);
            e.Property(s => s.RejectReason).HasColumnType("text");
            e.Property(s => s.OrderId).HasMaxLength(256);
            e.HasIndex(e => e.PositionId);
            e.HasIndex(e => e.TradeId);
        });

        // BotRun
        modelBuilder.Entity<BotRun>(e =>
        {
            e.ToTable("bot_runs");
            e.HasKey(b => b.Id);
            e.Property(b => b.InitialBankroll).HasPrecision(18, 2);
            e.Property(b => b.CurrentBankroll).HasPrecision(18, 2);
            e.Property(b => b.TotalPnl).HasPrecision(18, 2);
        });

        // ExecutionCycle
        modelBuilder.Entity<ExecutionCycle>(e =>
        {
            e.ToTable("execution_cycles");
            e.HasKey(c => c.Id);
            e.Property(c => c.ErrorMessage).HasColumnType("text");
            e.HasIndex(c => c.BotRunId);
        });

        // PnLSnapshot
        modelBuilder.Entity<PnLSnapshot>(e =>
        {
            e.ToTable("pnl_snapshots");
            e.HasKey(p => p.Id);
            e.Property(p => p.Bankroll).HasPrecision(18, 2);
            e.Property(p => p.TotalInvested).HasPrecision(18, 2);
            e.Property(p => p.MetadataJson).HasColumnType("jsonb");
            e.HasIndex(e => e.ExecutionCycleId);
        });

        // Asset
        modelBuilder.Entity<Asset>(e =>
        {
            e.ToTable("assets");
            e.HasKey(a => a.Id);
            e.Property(a => a.Symbol).HasMaxLength(10).IsRequired();
            e.Property(a => a.Name).HasMaxLength(100);
            e.HasIndex(a => a.Symbol).IsUnique();
        });

        // AssetPriceSnapshot
        modelBuilder.Entity<AssetPriceSnapshot>(e =>
        {
            e.ToTable("asset_price_snapshots");
            e.HasKey(a => a.Id);
            e.Property(a => a.Price).HasPrecision(18, 2);
            e.Property(a => a.High24h).HasPrecision(18, 2);
            e.Property(a => a.Low24h).HasPrecision(18, 2);
            e.Property(a => a.Volume24h).HasPrecision(18, 2);
            e.Property(a => a.Source).HasMaxLength(50);
            e.HasIndex(a => a.AssetId);
            e.HasIndex(a => a.CapturedAt);
        });

        // AssetTrendSnapshot
        modelBuilder.Entity<AssetTrendSnapshot>(e =>
        {
            e.ToTable("asset_trend_snapshots");
            e.HasKey(a => a.Id);
            e.Property(a => a.TrendStrength).HasPrecision(18, 4);
            e.Property(a => a.Confidence).HasPrecision(18, 4);
            e.Property(a => a.Rsi).HasPrecision(18, 4);
            e.Property(a => a.Momentum1m).HasPrecision(18, 6);
            e.Property(a => a.Momentum5m).HasPrecision(18, 6);
            e.Property(a => a.Momentum15m).HasPrecision(18, 6);
            e.Property(a => a.VwapDeviation).HasPrecision(18, 6);
            e.Property(a => a.SmaCrossover).HasPrecision(18, 6);
            e.Property(a => a.MetadataJson).HasColumnType("jsonb");
            e.HasIndex(a => a.AssetId);
            e.HasIndex(a => a.CalculatedAt);
        });

        // DecisionTrace
        modelBuilder.Entity<DecisionTrace>(e =>
        {
            e.ToTable("decision_traces");
            e.HasKey(d => d.Id);
            e.Property(d => d.TraceType).HasMaxLength(50);
            e.HasIndex(d => d.ExecutionCycleId);
            e.HasIndex(d => d.SignalId);
            e.HasIndex(d => d.TradeId);
            e.HasIndex(d => d.PositionId);
        });

        // DecisionTraceStep
        modelBuilder.Entity<DecisionTraceStep>(e =>
        {
            e.ToTable("decision_trace_steps");
            e.HasKey(s => s.Id);
            e.Property(s => s.StepType).HasMaxLength(100);
            e.Property(s => s.Description).HasColumnType("text");
            e.Property(s => s.MetadataJson).HasColumnType("jsonb");
            e.HasIndex(s => s.DecisionTraceId);
        });

        // DecisionInputSnapshot
        modelBuilder.Entity<DecisionInputSnapshot>(e =>
        {
            e.ToTable("decision_input_snapshots");
            e.HasKey(d => d.Id);
            e.Property(d => d.MarketId).HasMaxLength(256);
            e.Property(d => d.MarketSlug).HasMaxLength(512);
            e.Property(d => d.OutcomeId).HasMaxLength(256);
            e.Property(d => d.BtcPrice).HasPrecision(18, 2);
            e.Property(d => d.EthPrice).HasPrecision(18, 2);
            e.Property(d => d.MetadataJson).HasColumnType("jsonb");
            e.HasIndex(d => d.DecisionTraceId);
        });

        // RiskEvaluationTrace
        modelBuilder.Entity<RiskEvaluationTrace>(e =>
        {
            e.ToTable("risk_evaluation_traces");
            e.HasKey(r => r.Id);
            e.Property(r => r.RuleName).HasMaxLength(100);
            e.Property(r => r.Reason).HasColumnType("text");
            e.Property(r => r.MetadataJson).HasColumnType("jsonb");
            e.HasIndex(r => r.DecisionTraceId);
        });

        // StrategyReasoningTrace
        modelBuilder.Entity<StrategyReasoningTrace>(e =>
        {
            e.ToTable("strategy_reasoning_traces");
            e.HasKey(s => s.Id);
            e.Property(s => s.Reasoning).HasColumnType("text");
            e.Property(s => s.MetadataJson).HasColumnType("jsonb");
            e.HasIndex(s => s.DecisionTraceId);
        });

        // DecisionOutcomeReview
        modelBuilder.Entity<DecisionOutcomeReview>(e =>
        {
            e.ToTable("decision_outcome_reviews");
            e.HasKey(d => d.Id);
            e.Property(d => d.Notes).HasColumnType("text");
            e.HasIndex(d => d.DecisionTraceId);
        });

        // MissedOpportunity
        modelBuilder.Entity<MissedOpportunity>(e =>
        {
            e.ToTable("missed_opportunities");
            e.HasKey(m => m.Id);
            e.Property(m => m.MarketId).HasMaxLength(256);
            e.Property(m => m.MarketSlug).HasMaxLength(512);
            e.Property(m => m.SkipReason).HasColumnType("text");
            e.Property(m => m.TrendDirection).HasMaxLength(50);
            e.Property(m => m.MomentumState).HasMaxLength(50);
            e.HasIndex(m => m.DecisionTraceId);
            e.HasIndex(m => m.MarketId);
        });

        // StrategyPerformanceSnapshot
        modelBuilder.Entity<StrategyPerformanceSnapshot>(e =>
        {
            e.ToTable("strategy_performance_snapshots");
            e.HasKey(s => s.Id);
            e.Property(s => s.MetadataJson).HasColumnType("jsonb");
            e.HasIndex(s => s.StrategyId);
        });

        // TradeOutcome
        modelBuilder.Entity<TradeOutcome>(e =>
        {
            e.ToTable("trade_outcomes");
            e.HasKey(t => t.Id);
            e.Property(t => t.MarketId).HasMaxLength(256);
            e.Property(t => t.MarketSlug).HasMaxLength(512);
            e.Property(t => t.EntryPrice).HasPrecision(18, 8);
            e.Property(t => t.SettlementValue).HasPrecision(18, 8);
            e.Property(t => t.Result).HasMaxLength(50);
            e.Property(t => t.TrendDirection).HasMaxLength(50);
            e.Property(t => t.MomentumState).HasMaxLength(50);
            e.Property(t => t.StrategyType).HasMaxLength(100);
            e.Property(t => t.SkipReason).HasColumnType("text");
            e.HasIndex(t => t.TradeId);
            e.HasIndex(t => t.DecisionTraceId);
            e.HasIndex(t => t.MarketId);
        });
    }
}
