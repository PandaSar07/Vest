using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Vest.Models;

namespace Vest.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public IActionResult Index() => View();
    public IActionResult Privacy() => View();
    public IActionResult About() => View();

    // GET: /Home/Log
    [HttpGet]
    public IActionResult Log() => View(new LoginViewModel());

    // POST: /Home/Log
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Log(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var http = _httpClientFactory.CreateClient("supabase");

            // Fetch user by email
            var response = await http.GetAsync($"users?email=eq.{Uri.EscapeDataString(model.Email)}&select=id,email,username,password_hash");
            var json = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<StoredUser>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (users != null && users.Count > 0)
            {
                var user = users[0];
                if (BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    HttpContext.Session.SetString("UserId", user.Id);
                    HttpContext.Session.SetString("UserEmail", user.Email);
                    HttpContext.Session.SetString("Username", user.Username);
                    return RedirectToAction("Index", "Dashboard");
                }
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed");
            ModelState.AddModelError(string.Empty, "Login failed. Please try again.");
        }

        return View(model);
    }

    // GET: /Home/Signup
    [HttpGet]
    public IActionResult Signup() => View(new SignupViewModel());

    // POST: /Home/Signup
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Signup(SignupViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var http = _httpClientFactory.CreateClient("supabase");

            var payload = new
            {
                full_name     = model.FullName,
                email         = model.Email,
                username      = model.Username,
                password_hash = BCrypt.Net.BCrypt.HashPassword(model.Password)
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await http.PostAsync("users", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Account created! You can now log in.";
                return RedirectToAction("Log");
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Signup HTTP error: {Error}", error);

            if (error.Contains("duplicate") || error.Contains("unique"))
                ModelState.AddModelError(string.Empty, "That email or username is already taken.");
            else
                ModelState.AddModelError(string.Empty, "Signup failed. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Signup failed");
            ModelState.AddModelError(string.Empty, "Signup failed. Please try again.");
        }

        return View(model);
    }

    // POST: /Home/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

// Local DTO for reading back user rows
file class StoredUser
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id           { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string Email        { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("username")]
    public string Username     { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;
}
