namespace Vest.Services;

/// <summary>Validates required configuration before the app starts in non-development environments.</summary>
public static class ConfigurationValidator
{
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
            || allowedHosts == "*"
            || allowedHosts.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Set AllowedHosts to your production domain(s), e.g. AllowedHosts=yourdomain.com;www.yourdomain.com");
        }
    }

    private static string ToEnvVarName(string configKey) =>
        configKey.Replace(":", "__");
}
