using Polymarket.Bot.Application.Abstractions.Results;
using Polymarket.Bot.Domain.Entities;
using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Application.Abstractions.Interfaces;

/// <summary>
/// Client for fetching Polymarket markets and prices.
/// </summary>
public interface IPolymarketClient
{
    /// <summary>
    /// Fetch active markets from Polymarket.
    /// </summary>
    Task<IReadOnlyList<MarketDto>> GetActiveMarketsAsync(CancellationToken ct = default);

    /// <summary>
    /// Fetch market by ID.
    /// </summary>
    Task<MarketDto?> GetMarketAsync(string marketId, CancellationToken ct = default);

    /// <summary>
    /// Fetch order book for a token.
    /// </summary>
    Task<OrderBookDto?> GetOrderBookAsync(string tokenId, CancellationToken ct = default);

    /// <summary>
    /// Check if client is connected (stub always returns true).
    /// </summary>
    Task<bool> IsConnectedAsync(CancellationToken ct = default);
}

/// <summary>
/// Market data from Polymarket.
/// </summary>
public record MarketDto(
    string MarketId,
    string Slug,
    string Question,
    MarketStatus Status,
    decimal YesPrice,
    decimal NoPrice,
    decimal BestBid,
    decimal BestAsk,
    decimal Liquidity,
    decimal Volume,
    DateTime? WindowStart,
    DateTime? WindowEnd,
    string? UpTokenId,
    string? DownTokenId);

/// <summary>
/// Order book data.
/// </summary>
public record OrderBookDto(
    string TokenId,
    decimal BestBid,
    decimal BestAsk,
    decimal Spread,
    decimal Liquidity,
    IReadOnlyList<OrderBookLevel> Bids,
    IReadOnlyList<OrderBookLevel> Asks);

/// <summary>
/// Single order book level.
/// </summary>
public record OrderBookLevel(decimal Price, decimal Size);
