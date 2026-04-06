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
    private readonly Vest.Services.EmailService _emailService;

    public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, IConfiguration config, Vest.Services.EmailService emailService)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _emailService = emailService;
    }

    public IActionResult Index()
    {
        ViewData["FluidMain"] = true;
        return View();
    }
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

    // GET: /Home/ForgotPassword
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    // POST: /Home/ForgotPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var http = _httpClientFactory.CreateClient("supabase");

            // Check if user with this email exists
            var response = await http.GetAsync(
                $"users?email=eq.{Uri.EscapeDataString(model.Email)}&select=id,email");
            var json = await response.Content.ReadAsStringAsync();
            var users = System.Text.Json.JsonSerializer.Deserialize<List<StoredUser>>(
                json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Always show the same success message regardless of whether the email exists
            // (prevents user enumeration attacks)
            if (users != null && users.Count > 0)
            {
                var user   = users[0];
                var token  = Guid.NewGuid().ToString("N"); // 32-char hex token
                var expiry = DateTime.UtcNow.AddHours(1).ToString("o"); // ISO-8601

                // PATCH reset token + expiry onto the user row
                var patch = new { reset_token = token, reset_token_expiry = expiry };
                var patchContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(patch),
                    Encoding.UTF8,
                    "application/json");

                var patchReq = new HttpRequestMessage(HttpMethod.Patch,
                    $"users?id=eq.{Uri.EscapeDataString(user.Id)}")
                {
                    Content = patchContent
                };
                await http.SendAsync(patchReq);

                // Build and email the reset link
                var resetUrl = Url.Action("ResetPassword", "Home",
                    new { token }, Request.Scheme)!;

                await _emailService.SendPasswordResetAsync(user.Email, resetUrl);
            }

            TempData["ForgotSuccess"] = "true";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ForgotPassword failed");
            ModelState.AddModelError(string.Empty, "Something went wrong. Please try again.");
        }

        return View(model);
    }

    // GET: /Home/ResetPassword?token=...
    [HttpGet]
    public IActionResult ResetPassword(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("ForgotPassword");

        return View(new ResetPasswordViewModel { Token = token });
    }

    // POST: /Home/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var http = _httpClientFactory.CreateClient("supabase");

            // Look up user by token
            var response = await http.GetAsync(
                $"users?reset_token=eq.{Uri.EscapeDataString(model.Token)}&select=id,reset_token_expiry");
            var json = await response.Content.ReadAsStringAsync();
            var users = System.Text.Json.JsonSerializer.Deserialize<List<StoredUser>>(
                json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (users == null || users.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Reset link is invalid or has already been used.");
                return View(model);
            }

            var user = users[0];

            // Check expiry — use DateTimeOffset to correctly handle the +00:00 offset Supabase returns
            if (!DateTimeOffset.TryParse(user.ResetTokenExpiry, out var expiry) || expiry.UtcDateTime < DateTime.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "This reset link has expired. Please request a new one.");
                return View(model);
            }

            // Hash new password and clear the token in one PATCH
            var newHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            var patch = new
            {
                password_hash      = newHash,
                reset_token        = (string?)null,
                reset_token_expiry = (string?)null
            };
            var patchContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(patch),
                Encoding.UTF8,
                "application/json");

            var patchReq = new HttpRequestMessage(HttpMethod.Patch,
                $"users?id=eq.{Uri.EscapeDataString(user.Id)}")
            {
                Content = patchContent
            };
            await http.SendAsync(patchReq);

            TempData["SuccessMessage"] = "Password updated! You can now log in with your new password.";
            return RedirectToAction("Log");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ResetPassword failed");
            ModelState.AddModelError(string.Empty, "Something went wrong. Please try again.");
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
    public string Id { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("reset_token_expiry")]
    public string? ResetTokenExpiry { get; set; }
}
