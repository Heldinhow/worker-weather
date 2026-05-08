using PolymarketBot.Framework.Contracts;

namespace PolymarketBot.Framework.Domain.Entities;

public class WorkerRegistration
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public WorkerStatus Status { get; set; }
    public Dictionary<string, string> Config { get; set; } = new();
    public DateTime RegisteredAt { get; set; }
}
