using Microsoft.Extensions.DependencyInjection;

namespace Vest.Services;

/// <summary>
/// Monitors active stop-loss / take-profit rules and closes positions when levels are hit.
/// </summary>
public class RiskMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RiskMonitorService> _logger;
    private readonly TimeSpan _interval;

    public RiskMonitorService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<RiskMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
        var seconds = config.GetValue("Risk:MonitorIntervalSeconds", 30);
        _interval = TimeSpan.FromSeconds(Math.Clamp(seconds, 15, 120));
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(12), ct);
        while (!ct.IsCancellationRequested)
        {
            try { await CheckRiskRulesAsync(ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "Risk monitor tick failed"); }
            await Task.Delay(_interval, ct);
        }
    }

    private async Task CheckRiskRulesAsync(CancellationToken ct)
    {
        using var scope     = _scopeFactory.CreateScope();
        var portfolio       = scope.ServiceProvider.GetRequiredService<PortfolioService>();
        var finnhub         = scope.ServiceProvider.GetRequiredService<FinnhubService>();
        var email           = scope.ServiceProvider.GetRequiredService<EmailService>();
        var push            = scope.ServiceProvider.GetRequiredService<PushNotificationService>();
        var httpFactory     = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        var rules = await portfolio.GetActiveRiskRulesAsync();
        if (rules.Count == 0) return;

        foreach (var grp in rules.GroupBy(r => r.Symbol))
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var q = await finnhub.GetFullQuoteAsync(QuoteSymbolResolver.ForFinnhubQuote(grp.Key));
                if (q == null || !q.Value.TryGetProperty("c", out var priceEl)) continue;
                var livePrice = priceEl.GetDecimal();
                if (livePrice <= 0) continue;

                foreach (var rule in grp)
                {
                    if (ct.IsCancellationRequested) break;

                    var holding = await portfolio.GetHoldingAsync(rule.UserId, rule.Symbol);
                    if (holding == null || holding.Shares <= 0)
                    {
                        await portfolio.CancelRiskRuleAsync(rule.UserId, rule.Symbol);
                        continue;
                    }

                    var trigger = RiskRuleValidator.EvaluateTrigger(
                        livePrice, rule.StopLossPrice, rule.TakeProfitPrice);
                    if (trigger == null) continue;

                    _logger.LogInformation(
                        "Risk {Trigger} for {Symbol} user {UserId} @ {Price} (SL {Sl}, TP {Tp})",
                        trigger, rule.Symbol, rule.UserId, livePrice,
                        rule.StopLossPrice, rule.TakeProfitPrice);

                    var (ok, error) = await portfolio.ExecuteSellAsync(
                        rule.UserId, rule.Symbol, holding.Shares, livePrice, trigger);

                    if (!ok)
                    {
                        _logger.LogWarning("Risk exit failed for {Symbol}: {Error}", rule.Symbol, error);
                        continue;
                    }

                    await portfolio.MarkRiskRuleTriggeredAsync(rule.Id, trigger);

                    await NotifyRiskExitAsync(
                        httpFactory, email, push, portfolio,
                        rule.UserId, rule.Symbol, trigger, holding.Shares, livePrice, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Risk check failed for {Symbol}", grp.Key);
            }
        }
    }

    private static async Task NotifyRiskExitAsync(
        IHttpClientFactory httpFactory,
        EmailService email,
        PushNotificationService push,
        PortfolioService portfolio,
        string userId,
        string symbol,
        string trigger,
        decimal shares,
        decimal price,
        CancellationToken ct)
    {
        var http = httpFactory.CreateClient("supabase");
        var userEmail = await portfolio.GetUserEmailAsync(userId);
        var label = trigger == TradeExitReason.StopLoss ? "Stop-loss" : "Take-profit";
        var total = Math.Round(shares * price, 2);

        var emailPref = await GetUserPrefAsync(http, userId, "email_notifications");
        if (emailPref != "false" && !string.IsNullOrEmpty(userEmail))
        {
            try
            {
                await email.SendOrderFilledAsync(
                    userEmail, symbol, "SELL", shares, price, total,
                    $"{label} triggered");
            }
            catch { /* best effort */ }
        }

        var pushPref = await GetUserPrefAsync(http, userId, "push_notifications");
        if (pushPref == "false") return;

        try
        {
            var resp = await http.GetAsync(
                $"push_subscriptions?user_id=eq.{Uri.EscapeDataString(userId)}&select=endpoint,p256dh,auth");
            if (!resp.IsSuccessStatusCode) return;
            using var doc = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array) return;

            var title = trigger == TradeExitReason.StopLoss
                ? "🛑 Stop-loss triggered"
                : "🎯 Take-profit triggered";
            var body = $"Sold {shares:N2} × {symbol} @ ${price:N2}";

            foreach (var sub in doc.RootElement.EnumerateArray())
            {
                if (ct.IsCancellationRequested) break;
                var endpoint = sub.GetProperty("endpoint").GetString()!;
                var p256dh   = sub.GetProperty("p256dh").GetString()!;
                var auth     = sub.GetProperty("auth").GetString()!;
                await push.SendAsync(endpoint, p256dh, auth, title, body, "/Dashboard");
            }
        }
        catch { /* best effort */ }
    }

    private static async Task<string?> GetUserPrefAsync(System.Net.Http.HttpClient http, string userId, string key)
    {
        try
        {
            var resp = await http.GetAsync(
                $"user_prefs?user_id=eq.{Uri.EscapeDataString(userId)}&key=eq.{key}&select=value");
            if (!resp.IsSuccessStatusCode) return null;
            using var doc = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var arr = doc.RootElement;
            if (arr.ValueKind == System.Text.Json.JsonValueKind.Array && arr.GetArrayLength() > 0)
                return arr[0].TryGetProperty("value", out var v) ? v.GetString() : null;
        }
        catch { }
        return null;
    }
}
