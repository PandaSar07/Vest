using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vest.Services;

public partial class PortfolioService
{
    // ── Position risk rules (stop-loss / take-profit) ───────────────────────

    public async Task<List<PositionRiskRuleRow>> GetRiskRulesForUserAsync(string userId)
    {
        try
        {
            var resp = await _http.GetAsync(
                $"position_risk_rules?user_id=eq.{Uri.EscapeDataString(userId)}&status=eq.{RiskRuleStatus.Active}&select=*");
            if (!resp.IsSuccessStatusCode) return [];
            return JsonSerializer.Deserialize<List<PositionRiskRuleRow>>(
                await resp.Content.ReadAsStringAsync(), _json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<PositionRiskRuleRow?> GetRiskRuleAsync(string userId, string symbol)
    {
        try
        {
            var resp = await _http.GetAsync(
                $"position_risk_rules?user_id=eq.{Uri.EscapeDataString(userId)}&symbol=eq.{Uri.EscapeDataString(symbol)}&status=eq.{RiskRuleStatus.Active}&select=*&limit=1");
            if (!resp.IsSuccessStatusCode) return null;
            var rows = JsonSerializer.Deserialize<List<PositionRiskRuleRow>>(
                await resp.Content.ReadAsStringAsync(), _json);
            return rows?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<PositionRiskRuleRow>> GetActiveRiskRulesAsync()
    {
        try
        {
            var resp = await _http.GetAsync(
                $"position_risk_rules?status=eq.{RiskRuleStatus.Active}&select=*");
            if (!resp.IsSuccessStatusCode) return [];
            return JsonSerializer.Deserialize<List<PositionRiskRuleRow>>(
                await resp.Content.ReadAsStringAsync(), _json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<(bool ok, string error, PositionRiskRuleRow? rule)> UpsertRiskRuleAsync(
        string userId,
        string symbol,
        decimal entryPrice,
        decimal? stopLossPrice,
        decimal? stopLossPct,
        decimal? takeProfitPrice,
        decimal? takeProfitPct)
    {
        var (valid, err, levels) = RiskRuleValidator.ValidateLong(
            entryPrice, stopLossPrice, stopLossPct, takeProfitPrice, takeProfitPct);
        if (!valid) return (false, err, null);

        var holding = await GetHoldingAsync(userId, symbol);
        if (holding == null || holding.Shares <= 0)
            return (false, "Open a position before setting risk rules.", null);

        var now = DateTime.UtcNow.ToString("o");
        var existing = await GetRiskRuleAsync(userId, symbol);

        var body = new
        {
            user_id           = userId,
            symbol,
            status            = RiskRuleStatus.Active,
            entry_price       = entryPrice,
            stop_loss_price   = levels.StopLossPrice,
            take_profit_price = levels.TakeProfitPrice,
            stop_loss_pct     = stopLossPct,
            take_profit_pct   = takeProfitPct,
            updated_at        = now,
        };

        HttpResponseMessage resp;
        if (existing != null)
        {
            resp = await _http.PatchAsync(
                $"position_risk_rules?id=eq.{existing.Id}",
                Json(body));
        }
        else
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, "position_risk_rules")
            {
                Headers = { { "Prefer", "return=representation" } },
                Content = Json(body)
            };
            resp = await _http.SendAsync(msg);
        }

        if (!resp.IsSuccessStatusCode)
            return (false, "Could not save risk settings. Ensure the database migration has been applied.", null);

        var rows = JsonSerializer.Deserialize<List<PositionRiskRuleRow>>(
            await resp.Content.ReadAsStringAsync(), _json);
        return (true, string.Empty, rows?.FirstOrDefault() ?? existing);
    }

    public async Task<bool> CancelRiskRuleAsync(string userId, string symbol)
    {
        try
        {
            var resp = await _http.PatchAsync(
                $"position_risk_rules?user_id=eq.{Uri.EscapeDataString(userId)}&symbol=eq.{Uri.EscapeDataString(symbol)}&status=eq.{RiskRuleStatus.Active}",
                Json(new { status = RiskRuleStatus.Cancelled, updated_at = DateTime.UtcNow.ToString("o") }));
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task MarkRiskRuleTriggeredAsync(long ruleId, string triggerType)
    {
        try
        {
            await _http.PatchAsync(
                $"position_risk_rules?id=eq.{ruleId}",
                Json(new
                {
                    status        = RiskRuleStatus.Triggered,
                    trigger_type  = triggerType,
                    triggered_at  = DateTime.UtcNow.ToString("o"),
                    updated_at    = DateTime.UtcNow.ToString("o"),
                }));
        }
        catch { /* best effort */ }
    }
}

public class PositionRiskRuleRow
{
    [JsonPropertyName("id")]                public long     Id               { get; set; }
    [JsonPropertyName("user_id")]           public string   UserId           { get; set; } = "";
    [JsonPropertyName("symbol")]            public string   Symbol           { get; set; } = "";
    [JsonPropertyName("status")]            public string   Status           { get; set; } = "";
    [JsonPropertyName("entry_price")]       public decimal  EntryPrice       { get; set; }
    [JsonPropertyName("stop_loss_price")]   public decimal? StopLossPrice    { get; set; }
    [JsonPropertyName("take_profit_price")] public decimal? TakeProfitPrice  { get; set; }
    [JsonPropertyName("stop_loss_pct")]   public decimal? StopLossPct      { get; set; }
    [JsonPropertyName("take_profit_pct")] public decimal? TakeProfitPct    { get; set; }
    [JsonPropertyName("trigger_type")]      public string?  TriggerType      { get; set; }
}
