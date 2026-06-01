namespace Vest.Services;

/// <summary>Validates required configuration before the app starts in non-development environments.</summary>
public static class ConfigurationValidator
{
    /// <summary>
    /// Builds the production AllowedHosts list: explicit hosts plus platform URL (Render, Railway, Fly).
    /// Returns null in development (no override).
    /// </summary>
    public static string? ResolveProductionAllowedHosts(IConfiguration configuration, bool isDevelopment)
    {
        if (isDevelopment)
            return null;

        var hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var current = configuration["AllowedHosts"];
        if (IsValidProductionAllowedHosts(current))
        {
            foreach (var host in current!.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                hosts.Add(host);
        }

        AddPlatformHosts(hosts);

        if (hosts.Count > 0)
            return string.Join(';', hosts);

        // Render sets RENDER=true; allow requests when external URL is not injected yet
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER")))
            return "*";

        return null;
    }

    private static void AddPlatformHosts(HashSet<string> hosts)
    {
        var renderUrl = Environment.GetEnvironmentVariable("RENDER_EXTERNAL_URL");
        if (Uri.TryCreate(renderUrl, UriKind.Absolute, out var renderUri))
            hosts.Add(renderUri.Host);

        var railway = Environment.GetEnvironmentVariable("RAILWAY_PUBLIC_DOMAIN");
        if (!string.IsNullOrWhiteSpace(railway))
        {
            if (Uri.TryCreate(railway, UriKind.Absolute, out var railwayUri))
                hosts.Add(railwayUri.Host);
            else
                hosts.Add(railway.Trim().TrimEnd('/'));
        }

        var flyApp = Environment.GetEnvironmentVariable("FLY_APP_NAME");
        if (!string.IsNullOrWhiteSpace(flyApp))
            hosts.Add($"{flyApp.Trim()}.fly.dev");
    }

    private static bool IsValidProductionAllowedHosts(string? allowedHosts) =>
        !string.IsNullOrWhiteSpace(allowedHosts)
        && !allowedHosts.Contains("localhost", StringComparison.OrdinalIgnoreCase)
        && !allowedHosts.Contains("YOUR_DOMAIN", StringComparison.OrdinalIgnoreCase);

    private static readonly string[] RequiredKeys =
    [
        "Finnhub:ApiKey",
        "Supabase:Url",
        "Supabase:ServiceRoleKey",
        "Authentication:Google:ClientId",
        "Authentication:Google:ClientSecret",
        "Email:SmtpHost",
        "Email:SmtpPort",
        "Email:Username",
        "Email:Password",
        "Email:From",
        "Vapid:Subject",
        "Vapid:PublicKey",
        "Vapid:PrivateKey",
    ];

    public static void ValidateForProduction(IConfiguration configuration, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            return;

        var missing = RequiredKeys
            .Where(key => string.IsNullOrWhiteSpace(configuration[key]))
            .ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Missing required production configuration. Set these environment variables on your host: "
                + string.Join(", ", missing.Select(ToEnvVarName)));
        }

        var allowedHosts = configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(allowedHosts)
            || allowedHosts.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || allowedHosts.Contains("YOUR_DOMAIN", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "AllowedHosts must include your public hostname. On Render, remove a bad AllowedHosts value "
                + "or redeploy so RENDER_EXTERNAL_URL is applied automatically.");
        }
    }

    private static string ToEnvVarName(string configKey) =>
        configKey.Replace(":", "__");
}
