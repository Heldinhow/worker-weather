using PolymarketBot.Framework.Domain.ValueObjects;

namespace PolymarketBot.Framework.Domain.Entities;

public class TradeSignal
{
    public string MarketId { get; set; } = string.Empty;
    public string WorkerType { get; set; } = string.Empty;
    public Direction Direction { get; set; }
    public Price EntryPrice { get; set; } = null!;
    public Edge Edge { get; set; } = null!;
    public decimal Size { get; set; }
    public decimal Probability { get; set; }
    public decimal Confidence { get; set; }
    public decimal KellySize { get; set; }
    public DateTime Timestamp { get; set; }
}
