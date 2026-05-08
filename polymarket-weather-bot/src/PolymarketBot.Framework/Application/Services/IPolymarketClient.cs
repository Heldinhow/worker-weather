using FluentResults;
using PolymarketBot.Framework.Domain.Entities;

namespace PolymarketBot.Framework.Application.Services;

public interface IPolymarketClient
{
    Task<Result<T>> GetMarketAsync<T>(string marketId) where T : class;
    Task<Result<OrderResult>> PlaceOrderAsync(OrderRequest request);
    Task<Result<MarketStatus>> GetMarketStatusAsync(string marketId);
}

public record OrderRequest(string MarketId, Direction Direction, decimal Size, decimal Price);
public record OrderResult(bool Success, string? OrderId = null, string? Error = null);
public enum MarketStatus { Open, Closed, Resolved }