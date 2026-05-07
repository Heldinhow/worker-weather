using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Represents an asset (BTC, ETH) being tracked for trend/momentum analysis.
/// </summary>
public class Asset : Entity
{
    public string Symbol { get; private set; } = string.Empty;
    public AssetSymbol Type { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private Asset() { }

    public static Asset Create(string symbol, AssetSymbol type, string name)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be empty", nameof(symbol));

        return new Asset
        {
            Symbol = symbol.ToUpperInvariant(),
            Type = type,
            Name = name ?? symbol
        };
    }

    public static Asset BTC() => Create("BTC", AssetSymbol.BTC, "Bitcoin");
    public static Asset ETH() => Create("ETH", AssetSymbol.ETH, "Ethereum");

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
