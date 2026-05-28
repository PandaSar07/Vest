namespace Vest.Helpers
{
    /// <summary>
    /// Utility methods for formatting currency values consistently across the app.
    /// </summary>
    public static class CurrencyHelper
    {
        /// <summary>
        /// Formats a decimal value as USD (e.g. "$1,234.56").
        /// Values over 1 million are abbreviated (e.g. "$1.23M").
        /// Values over 1 billion are abbreviated (e.g. "$4.56B").
        /// </summary>
        public static string FormatUsd(decimal amount)
        {
            if (Math.Abs(amount) >= 1_000_000_000m)
                return $"${amount / 1_000_000_000m:F2}B";

            if (Math.Abs(amount) >= 1_000_000m)
                return $"${amount / 1_000_000m:F2}M";

            return amount.ToString("C2");
        }
    }
}
