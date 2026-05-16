namespace Vest.Services;

public static class TradeExitReason
{
    public const string Manual     = "MANUAL";
    public const string StopLoss   = "STOP_LOSS";
    public const string TakeProfit = "TAKE_PROFIT";
    public const string LimitFill  = "LIMIT_FILL";
}

public static class RiskRuleStatus
{
    public const string Active    = "ACTIVE";
    public const string Cancelled = "CANCELLED";
    public const string Triggered = "TRIGGERED";
}

public record ResolvedRiskLevels(decimal? StopLossPrice, decimal? TakeProfitPrice);

public static class RiskRuleValidator
{
    /// <summary>Validates long-only paper positions and resolves % inputs to price levels.</summary>
    public static (bool ok, string error, ResolvedRiskLevels levels) ValidateLong(
        decimal entryPrice,
        decimal? stopLossPrice,
        decimal? stopLossPct,
        decimal? takeProfitPrice,
        decimal? takeProfitPct)
    {
        if (entryPrice <= 0)
            return (false, "Entry price must be positive.", new ResolvedRiskLevels(null, null));

        var hasSl = stopLossPrice.HasValue || stopLossPct.HasValue;
        var hasTp = takeProfitPrice.HasValue || takeProfitPct.HasValue;
        if (!hasSl && !hasTp)
            return (false, "Set at least a stop-loss or take-profit level.", new ResolvedRiskLevels(null, null));

        decimal? sl = stopLossPrice;
        if (!sl.HasValue && stopLossPct.HasValue)
        {
            if (stopLossPct <= 0 || stopLossPct >= 100)
                return (false, "Stop-loss % must be between 0 and 100 (exclusive).", new ResolvedRiskLevels(null, null));
            sl = Math.Round(entryPrice * (1 - stopLossPct.Value / 100m), 4);
        }

        decimal? tp = takeProfitPrice;
        if (!tp.HasValue && takeProfitPct.HasValue)
        {
            if (takeProfitPct <= 0)
                return (false, "Take-profit % must be greater than 0.", new ResolvedRiskLevels(null, null));
            tp = Math.Round(entryPrice * (1 + takeProfitPct.Value / 100m), 4);
        }

        if (sl.HasValue)
        {
            if (sl <= 0)
                return (false, "Stop-loss price must be positive.", new ResolvedRiskLevels(null, null));
            if (sl >= entryPrice)
                return (false, "Stop-loss must be below entry price for long positions.", new ResolvedRiskLevels(null, null));
        }

        if (tp.HasValue)
        {
            if (tp <= 0)
                return (false, "Take-profit price must be positive.", new ResolvedRiskLevels(null, null));
            if (tp <= entryPrice)
                return (false, "Take-profit must be above entry price for long positions.", new ResolvedRiskLevels(null, null));
        }

        if (sl.HasValue && tp.HasValue && sl >= tp)
            return (false, "Stop-loss must be below take-profit.", new ResolvedRiskLevels(null, null));

        return (true, string.Empty, new ResolvedRiskLevels(sl, tp));
    }

    /// <summary>Long position: stop when price at or below SL; take profit at or above TP.</summary>
    public static string? EvaluateTrigger(decimal livePrice, decimal? stopLoss, decimal? takeProfit)
    {
        if (livePrice <= 0) return null;
        if (stopLoss.HasValue && livePrice <= stopLoss.Value)
            return TradeExitReason.StopLoss;
        if (takeProfit.HasValue && livePrice >= takeProfit.Value)
            return TradeExitReason.TakeProfit;
        return null;
    }
}
