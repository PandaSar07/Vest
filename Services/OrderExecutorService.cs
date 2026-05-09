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
        using var scope  = _scopeFactory.CreateScope();
        var portfolio    = scope.ServiceProvider.GetRequiredService<PortfolioService>();
        var finnhub      = scope.ServiceProvider.GetRequiredService<FinnhubService>();
        var email        = scope.ServiceProvider.GetRequiredService<EmailService>();
        var push         = scope.ServiceProvider.GetRequiredService<PushNotificationService>();
        var http         = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("supabase");

        var orders = await portfolio.GetAllPendingOrdersAsync();
        if (orders.Count == 0) return;

        // Group by symbol so we only fetch each price once
        var bySymbol = orders.GroupBy(o => o.Symbol);

        foreach (var grp in bySymbol)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var q = await finnhub.GetFullQuoteAsync(QuoteSymbolResolver.ForFinnhubQuote(grp.Key));
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

                        // ── Notifications ──────────────────────────────────────
                        var total     = Math.Round(order.Shares * livePrice, 2);
                        var userEmail = await portfolio.GetUserEmailAsync(order.UserId);

                        // Email notification (respects user preference stored in Supabase)
                        var emailPref = await GetUserPrefAsync(http, order.UserId, "email_notifications");
                        if (emailPref != "false" && !string.IsNullOrEmpty(userEmail))
                        {
                            try
                            {
                                await email.SendOrderFilledAsync(
                                    userEmail, order.Symbol, order.Action,
                                    order.Shares, livePrice, total);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to send order-filled email to {Email}", userEmail);
                            }
                        }

                        // Push notification
                        var pushPref = await GetUserPrefAsync(http, order.UserId, "push_notifications");
                        if (pushPref != "false")
                        {
                            await SendPushAsync(http, push, order.UserId, order.Symbol, order.Action, livePrice, ct);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed checking orders for {Symbol}", grp.Key);
            }
        }
    }

    /// <summary>Fetches a single user preference value from the user_prefs table.</summary>
    private static async Task<string?> GetUserPrefAsync(HttpClient http, string userId, string key)
    {
        try
        {
            var resp = await http.GetAsync(
                $"user_prefs?user_id=eq.{Uri.EscapeDataString(userId)}&key=eq.{key}&select=value");
            if (!resp.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var arr = doc.RootElement;
            if (arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0)
                return arr[0].TryGetProperty("value", out var v) ? v.GetString() : null;
        }
        catch { /* preference table may not exist yet — treat as "enabled" */ }
        return null; // null = not set = notifications enabled
    }

    /// <summary>Sends a Web Push to all stored subscriptions for the given user.</summary>
    private async Task SendPushAsync(
        HttpClient http, PushNotificationService push,
        string userId, string symbol, string action, decimal price,
        CancellationToken ct)
    {
        try
        {
            var resp = await http.GetAsync(
                $"push_subscriptions?user_id=eq.{Uri.EscapeDataString(userId)}&select=endpoint,p256dh,auth");
            if (!resp.IsSuccessStatusCode) return;

            using var doc  = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var subs        = doc.RootElement;
            if (subs.ValueKind != JsonValueKind.Array) return;

            var isBuy  = action == "BUY";
            var emoji  = isBuy ? "🟢" : "🔴";
            var verb   = isBuy ? "Bought" : "Sold";
            var title  = $"{emoji} Limit order filled — {symbol}";
            var body   = $"{verb} @ ${price:N2}";

            foreach (var sub in subs.EnumerateArray())
            {
                if (ct.IsCancellationRequested) break;
                var endpoint = sub.GetProperty("endpoint").GetString()!;
                var p256dh   = sub.GetProperty("p256dh").GetString()!;
                var auth     = sub.GetProperty("auth").GetString()!;

                var ok = await push.SendAsync(endpoint, p256dh, auth, title, body, "/Dashboard");
                if (!ok)
                {
                    // Subscription expired — remove it
                    await http.DeleteAsync(
                        $"push_subscriptions?user_id=eq.{Uri.EscapeDataString(userId)}&endpoint=eq.{Uri.EscapeDataString(endpoint)}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send push for user {UserId}", userId);
        }
    }
}
