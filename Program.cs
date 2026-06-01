using Microsoft.AspNetCore.HttpOverrides;
using Vest.Services;

var builder = WebApplication.CreateBuilder(args);

// Render/Railway/Fly inject PORT; bind Kestrel when not using launchSettings.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var resolvedHosts = ConfigurationValidator.ResolveProductionAllowedHosts(
    builder.Configuration, builder.Environment.IsDevelopment());
if (resolvedHosts is not null)
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["AllowedHosts"] = resolvedHosts
    });
}

ConfigurationValidator.ValidateForProduction(builder.Configuration, builder.Environment);

var isDev = builder.Environment.IsDevelopment();

builder.Services.AddControllersWithViews();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.Name = ".Vest.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = isDev ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = ".Vest.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = isDev ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
        ?? throw new InvalidOperationException("Authentication:Google:ClientId is not configured.");
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
        ?? throw new InvalidOperationException("Authentication:Google:ClientSecret is not configured.");
});

builder.Services.AddHttpClient<FinnhubService>();

builder.Services.AddScoped<PortfolioService>();
builder.Services.AddScoped<UserPrefsService>();
builder.Services.AddScoped<PortfolioValuationService>();
builder.Services.AddScoped<LeaderboardBuilder>();
builder.Services.AddSingleton<LeaderboardService>();
builder.Services.AddHostedService<LeaderboardRefreshHostedService>();
builder.Services.AddHostedService<RiskMonitorService>();
builder.Services.AddHostedService<OrderExecutorService>();

builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AvatarService>();
builder.Services.AddSingleton<PushNotificationService>();

builder.Services.AddHttpClient("supabase", client =>
{
    var url      = builder.Configuration["Supabase:Url"]
        ?? throw new InvalidOperationException("Supabase:Url is not configured.");
    var adminKey = builder.Configuration["Supabase:ServiceRoleKey"]
        ?? throw new InvalidOperationException("Supabase:ServiceRoleKey is not configured.");
    client.BaseAddress = new Uri(url.TrimEnd('/') + "/rest/v1/");
    client.DefaultRequestHeaders.Add("apikey", adminKey);
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + adminKey);
    client.DefaultRequestHeaders.Add("Prefer", "return=minimal");
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
