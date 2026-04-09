using Vest.Services;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Authentication for Google OAuth
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "YOUR_GOOGLE_CLIENT_ID";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "YOUR_GOOGLE_CLIENT_SECRET";
});

// Register FinnhubService with HttpClient
builder.Services.AddHttpClient<FinnhubService>();

// Register PortfolioService (uses the named "supabase" client)
builder.Services.AddScoped<PortfolioService>();

// Background worker: polls limit orders every 60 s and fills them
builder.Services.AddHostedService<OrderExecutorService>();

// Register EmailService for sending password reset emails
builder.Services.AddScoped<EmailService>();

// HttpClient for direct Supabase REST calls (signup, login)
builder.Services.AddHttpClient("supabase", client =>
{
    var url      = builder.Configuration["Supabase:Url"]!;
    var adminKey = builder.Configuration["Supabase:ServiceRoleKey"]!;
    client.BaseAddress = new Uri(url + "/rest/v1/");
    client.DefaultRequestHeaders.Add("apikey", adminKey);
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + adminKey);
    client.DefaultRequestHeaders.Add("Prefer", "return=minimal");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapGet("/test", () => "Hello Vest!");
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
