using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vest.Services;

namespace Vest.Services;

/// <summary>
/// Background service that polls pending limit orders every 60 s and fills them when the
/// live market price crosses the limit price.
/// </summary>
public class OrderExecutorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderExecutorService> _logger;
    private static readonly TimeSpan _interval = TimeSpan.FromSeconds(60);

    public OrderExecutorService(IServiceScopeFactory scopeFactory, ILogger<OrderExecutorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await CheckOrdersAsync(ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "Order executor tick failed"); }
            await Task.Delay(_interval, ct);
        }
    }

    private async Task CheckOrdersAsync(CancellationToken ct)
    {
        using var scope     = _scopeFactory.CreateScope();
        var portfolio       = scope.ServiceProvider.GetRequiredService<PortfolioService>();
        var finnhub         = scope.ServiceProvider.GetRequiredService<FinnhubService>();

        var orders = await portfolio.GetAllPendingOrdersAsync();
        if (orders.Count == 0) return;

        // Group by symbol so we only fetch each price once
        var bySymbol = orders.GroupBy(o => o.Symbol);

        foreach (var grp in bySymbol)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var q = await finnhub.GetFullQuoteAsync(grp.Key);
                if (q == null || !q.Value.TryGetProperty("c", out var priceEl)) continue;
                var livePrice = priceEl.GetDecimal();
                if (livePrice == 0) continue;

                foreach (var order in grp)
                {
                    bool shouldFill = order.Action == "BUY"
                        ? livePrice <= order.LimitPrice   // buy triggers when price drops to/below limit
                        : livePrice >= order.LimitPrice;  // sell triggers when price rises to/above limit

                    if (shouldFill)
                    {
                        _logger.LogInformation("Filling {Action} order #{Id} for {Symbol} @ {Price}",
                            order.Action, order.Id, order.Symbol, livePrice);
                        await portfolio.FillOrderAsync(order);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed checking orders for {Symbol}", grp.Key);
            }
        }
    }
}
