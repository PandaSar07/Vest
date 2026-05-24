using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
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
    public IActionResult Privacy()
    {
        ViewData["FluidMain"] = true;
        return View();
    }

    public IActionResult About()
    {
        ViewData["FluidMain"] = true;
        return View();

    }

    public async Task<IActionResult> Settings()
    {
        ViewData["FluidMain"] = false;

        var userId = HttpContext.Session.GetString("UserId");
        if (userId != null)
        {
            try
            {
                var http = _httpClientFactory.CreateClient("supabase");
                var resp = await http.GetAsync(
                    $"profile_cooldowns?user_id=eq.{Uri.EscapeDataString(userId)}&select=updated_at");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var arr  = doc.RootElement;
                    if (arr.ValueKind == System.Text.Json.JsonValueKind.Array && arr.GetArrayLength() > 0)
                    {
                        var row = arr[0];
                        if (row.TryGetProperty("updated_at", out var tsEl) &&
                            tsEl.ValueKind != System.Text.Json.JsonValueKind.Null &&
                            DateTime.TryParse(tsEl.GetString(), out var lastUpdated))
                        {
                            var cooldownEnds = lastUpdated.AddDays(30);
                            var daysLeft     = (int)Math.Ceiling((cooldownEnds - DateTime.UtcNow).TotalDays);
                            if (daysLeft > 0)
                                ViewData["ProfileCooldownDays"] = daysLeft;
                        }
                    }
                }
            }
            catch { /* Non-fatal — just don't show cooldown */ }
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string newUsername, string newEmail)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToAction("Log");

        newUsername = newUsername?.Trim() ?? "";
        newEmail    = newEmail?.Trim() ?? "";

        if (string.IsNullOrEmpty(newUsername) || string.IsNullOrEmpty(newEmail))
        {
            TempData["ProfileError"] = "Username and email cannot be empty.";
            return RedirectToAction("Settings");
        }

        // Basic email format check
        if (!newEmail.Contains('@') || !newEmail.Contains('.'))
        {
            TempData["ProfileError"] = "Please enter a valid email address.";
            return RedirectToAction("Settings");
        }

        try
        {
            var http = _httpClientFactory.CreateClient("supabase");

            // ── Cooldown check: read from profile_cooldowns table ──────────────
            try
            {
                var curResp = await http.GetAsync(
                    $"profile_cooldowns?user_id=eq.{Uri.EscapeDataString(userId)}&select=updated_at");
                if (curResp.IsSuccessStatusCode)
                {
                    var curJson = await curResp.Content.ReadAsStringAsync();
                    using var curDoc = System.Text.Json.JsonDocument.Parse(curJson);
                    var curArr = curDoc.RootElement;
                    if (curArr.ValueKind == System.Text.Json.JsonValueKind.Array && curArr.GetArrayLength() > 0)
                    {
                        var row = curArr[0];
                        if (row.TryGetProperty("updated_at", out var tsEl) &&
                            tsEl.ValueKind != System.Text.Json.JsonValueKind.Null &&
                            DateTime.TryParse(tsEl.GetString(), out var lastUpdated))
                        {
                            var cooldownEnds = lastUpdated.AddDays(30);
                            var daysLeft     = (int)Math.Ceiling((cooldownEnds - DateTime.UtcNow).TotalDays);
                            if (daysLeft > 0)
                            {
                                TempData["ProfileError"] = $"You can only change your profile once every 30 days. Try again in {daysLeft} day{(daysLeft == 1 ? "" : "s")}";
                                return RedirectToAction("Settings");
                            }
                        }
                    }
                }
            }
            catch { /* Non-fatal — skip cooldown if table not yet created */ }

            // Check if username is taken by another user
            var unCheck = await http.GetAsync(
                $"users?username=eq.{Uri.EscapeDataString(newUsername)}&id=neq.{Uri.EscapeDataString(userId)}&select=id");
            var unJson = await unCheck.Content.ReadAsStringAsync();
            var unRows = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(unJson);
            if (unRows != null && unRows.Count > 0)
            {
                TempData["ProfileError"] = "That username is already taken.";
                return RedirectToAction("Settings");
            }

            // Check if email is taken by another user
            var emCheck = await http.GetAsync(
                $"users?email=eq.{Uri.EscapeDataString(newEmail)}&id=neq.{Uri.EscapeDataString(userId)}&select=id");
            var emJson = await emCheck.Content.ReadAsStringAsync();
            var emRows = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(emJson);
            if (emRows != null && emRows.Count > 0)
            {
                TempData["ProfileError"] = "That email address is already in use.";
                return RedirectToAction("Settings");
            }

            // Patch 1: update username and email only
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                username = newUsername,
                email    = newEmail
            });
            var patch = await http.PatchAsync(
                $"users?id=eq.{Uri.EscapeDataString(userId)}",
                new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

            if (!patch.IsSuccessStatusCode)
            {
                var errBody = await patch.Content.ReadAsStringAsync();
                _logger.LogWarning("Profile patch returned {Status}: {Body}", (int)patch.StatusCode, errBody);
                TempData["ProfileError"] = "Failed to update profile. Please try again.";
                return RedirectToAction("Settings");
            }

            // Upsert cooldown timestamp into profile_cooldowns
            try
            {
                var tsPayload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    user_id    = userId,
                    updated_at = DateTime.UtcNow.ToString("o")
                });
                var upsertMsg = new HttpRequestMessage(HttpMethod.Post, "profile_cooldowns")
                {
                    Headers  = { { "Prefer", "resolution=merge-duplicates" } },
                    Content  = new StringContent(tsPayload, System.Text.Encoding.UTF8, "application/json")
                };
                await http.SendAsync(upsertMsg);
            }
            catch { /* Non-fatal — table may not exist yet */ }

            // Refresh session
            HttpContext.Session.SetString("Username", newUsername);
            HttpContext.Session.SetString("UserEmail", newEmail);

            TempData["ProfileSuccess"] = "Profile updated successfully! You can change it again in 30 days.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateProfile failed for user {UserId}", userId);
            TempData["ProfileError"] = "An unexpected error occurred.";
        }

        return RedirectToAction("Settings");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToAction("Log");

        // Basic validation
        if (string.IsNullOrWhiteSpace(currentPassword) ||
            string.IsNullOrWhiteSpace(newPassword) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            TempData["PasswordError"] = "All password fields are required.";
            return RedirectToAction("Settings");
        }

        if (newPassword != confirmPassword)
        {
            TempData["PasswordError"] = "New passwords do not match.";
            return RedirectToAction("Settings");
        }

        if (newPassword.Length < 8)
        {
            TempData["PasswordError"] = "New password must be at least 8 characters.";
            return RedirectToAction("Settings");
        }

        try
        {
            var http = _httpClientFactory.CreateClient("supabase");

            // Fetch current password_hash for this user
            var resp = await http.GetAsync(
                $"users?id=eq.{Uri.EscapeDataString(userId)}&select=password_hash");
            var json = await resp.Content.ReadAsStringAsync();
            var rows = JsonSerializer.Deserialize<List<JsonElement>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (rows == null || rows.Count == 0)
            {
                TempData["PasswordError"] = "User not found.";
                return RedirectToAction("Settings");
            }

            var storedHash = rows[0].GetProperty("password_hash").GetString() ?? "";

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, storedHash))
            {
                TempData["PasswordError"] = "Current password is incorrect.";
                return RedirectToAction("Settings");
            }

            // Hash and save the new password
            var newHash    = BCrypt.Net.BCrypt.HashPassword(newPassword);
            var payload    = JsonSerializer.Serialize(new { password_hash = newHash });
            var patch      = await http.PatchAsync(
                $"users?id=eq.{Uri.EscapeDataString(userId)}",
                new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

            if (!patch.IsSuccessStatusCode)
            {
                TempData["PasswordError"] = "Failed to update password. Please try again.";
                return RedirectToAction("Settings");
            }

            TempData["PasswordSuccess"] = "Password changed successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChangePassword failed for user {UserId}", userId);
            TempData["PasswordError"] = "An unexpected error occurred.";
        }

        return RedirectToAction("Settings");
    }

    public IActionResult Contact()
    {
        ViewData["FluidMain"] = true;
        var email = _config["Vest:PublicContactEmail"]?.Trim();
        ViewData["PublicContactEmail"] = string.IsNullOrEmpty(email) ? null : email;
        return View();
    }

    // POST: /Home/ResetPortfolio
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPortfolio()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToAction("Log");

        try
        {
            var http = _httpClientFactory.CreateClient("supabase");

            // 1. Delete all holdings for this user
            await http.DeleteAsync($"holdings?user_id=eq.{Uri.EscapeDataString(userId)}");

            // 2. Reset cash balance to $100,000 (upsert)
            var payload = JsonSerializer.Serialize(new { user_id = userId, cash = 100000.00m });
            var upsert  = new HttpRequestMessage(HttpMethod.Post, "portfolios")
            {
                Headers  = { { "Prefer", "resolution=merge-duplicates" } },
                Content  = new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
            };
            await http.SendAsync(upsert);

            TempData["PrivacySuccess"] = "Portfolio reset! Your cash has been restored to $100,000.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ResetPortfolio failed for user {UserId}", userId);
            TempData["PrivacyError"] = "Failed to reset portfolio. Please try again.";
        }

        return RedirectToAction("Settings");
    }

    // GET: /Home/ExportTrades
    public async Task<IActionResult> ExportTrades()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToAction("Log");

        try
        {
            var http = _httpClientFactory.CreateClient("supabase");
            var resp = await http.GetAsync(
                $"trades?user_id=eq.{Uri.EscapeDataString(userId)}&order=traded_at.desc&limit=10000&select=*");
            resp.EnsureSuccessStatusCode();

            var json  = await resp.Content.ReadAsStringAsync();
            var trades = JsonSerializer.Deserialize<List<JsonElement>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Date,Action,Symbol,Shares,Price,Total");
            foreach (var t in trades)
            {
                var date   = t.TryGetProperty("traded_at",  out var d) ? d.GetString() ?? "" : "";
                var action = t.TryGetProperty("action",     out var a) ? a.GetString() ?? "" : "";
                var symbol = t.TryGetProperty("symbol",     out var s) ? s.GetString() ?? "" : "";
                var shares = t.TryGetProperty("shares",     out var sh) ? sh.GetDecimal() : 0m;
                var price  = t.TryGetProperty("price",      out var p)  ? p.GetDecimal()  : 0m;
                var total  = Math.Round(shares * price, 2);
                sb.AppendLine($"{date},{action},{symbol},{shares},{price},{total}");
            }

            var bytes    = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var username = HttpContext.Session.GetString("Username") ?? "user";
            var filename = $"vest-trades-{username}-{DateTime.UtcNow:yyyy-MM-dd}.csv";
            return File(bytes, "text/csv", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExportTrades failed for user {UserId}", userId);
            TempData["PrivacyError"] = "Failed to export trade history. Please try again.";
            return RedirectToAction("Settings");
        }
    }

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

    [HttpGet]
    [HttpPost]
    public IActionResult GoogleLogin()
    {
        var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleCallback") };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (result.Principal == null)
            return RedirectToAction("Log");

        var email = result.Principal.FindFirstValue(ClaimTypes.Email);
        var name = result.Principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(email))
        {
            TempData["ErrorMessage"] = "Could not retrieve email from Google.";
            return RedirectToAction("Log");
        }

        try
        {
            var http = _httpClientFactory.CreateClient("supabase");
            var response = await http.GetAsync($"users?email=eq.{Uri.EscapeDataString(email)}&select=id,email,username,password_hash");
            var json = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<StoredUser>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (users != null && users.Count > 0)
            {
                // User exists, log them in
                var user = users[0];
                HttpContext.Session.SetString("UserId", user.Id);
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("Username", user.Username);

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Index", "Dashboard");
            }
            else
            {
                // New user, redirect to complete signup to pick a username
                TempData["GoogleEmail"] = email;
                TempData["GoogleName"] = name ?? email.Split('@')[0];
                
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("CompleteGoogleSignup");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GoogleCallback failed");
            TempData["ErrorMessage"] = "An error occurred during Google sign in.";
            return RedirectToAction("Log");
        }
    }

    [HttpGet]
    public IActionResult CompleteGoogleSignup()
    {
        var email = TempData["GoogleEmail"]?.ToString();
        var name = TempData["GoogleName"]?.ToString();

        if (string.IsNullOrEmpty(email))
            return RedirectToAction("Signup"); // Lost state, restart

        // Re-store in TempData for the POST
        TempData.Keep("GoogleEmail");
        TempData.Keep("GoogleName");

        var model = new CompleteGoogleSignupViewModel
        {
            Email = email,
            FullName = name!
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteGoogleSignup(CompleteGoogleSignupViewModel model)
    {
        var email = TempData["GoogleEmail"]?.ToString() ?? model.Email;
        var name = TempData["GoogleName"]?.ToString() ?? model.FullName;

        if (string.IsNullOrEmpty(email))
            return RedirectToAction("Signup");

        TempData.Keep("GoogleEmail");
        TempData.Keep("GoogleName");

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var http = _httpClientFactory.CreateClient("supabase");

            // Random placeholder password hash since they use Google auth
            var placeholderPassword = Guid.NewGuid().ToString("N");

            var payload = new
            {
                full_name = name,
                email = email,
                username = model.Username,
                password_hash = BCrypt.Net.BCrypt.HashPassword(placeholderPassword)
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await http.PostAsync("users", content);

            if (response.IsSuccessStatusCode)
            {
                // Fetch the newly created user to get the ID
                var fetchRes = await http.GetAsync($"users?email=eq.{Uri.EscapeDataString(email)}&select=id,email,username");
                if (fetchRes.IsSuccessStatusCode)
                {
                    var fetchJson = await fetchRes.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<StoredUser>>(fetchJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (users != null && users.Count > 0)
                    {
                        var user = users[0];
                        HttpContext.Session.SetString("UserId", user.Id);
                        HttpContext.Session.SetString("UserEmail", user.Email);
                        HttpContext.Session.SetString("Username", user.Username);

                        TempData.Remove("GoogleEmail");
                        TempData.Remove("GoogleName");
                        return RedirectToAction("Index", "Dashboard");
                    }
                }
            }

            var error = await response.Content.ReadAsStringAsync();
            if (error.Contains("duplicate") || error.Contains("unique"))
                ModelState.AddModelError(string.Empty, "That username is already taken. Please choose another.");
            else
                ModelState.AddModelError(string.Empty, "Signup failed. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CompleteGoogleSignup failed");
            ModelState.AddModelError(string.Empty, "Signup failed. Please try again.");
        }

        return View(model);
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
