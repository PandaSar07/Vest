using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Vest.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetUrl)
    {
        var smtp        = _config["Email:SmtpHost"]!;
        var port        = int.Parse(_config["Email:SmtpPort"]!);
        var user        = _config["Email:Username"]!;
        var pass        = _config["Email:Password"]!;
        var fromAddress = _config["Email:From"]!;
        var fromName    = _config["Email:FromName"] ?? "Vest";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress(toEmail, toEmail));
        message.Subject = "Reset your Vest password";

        message.Body = new TextPart("html")
        {
            Text = $"""
            <div style="font-family:Inter,sans-serif;background:#0a0e1a;color:#fff;padding:40px;max-width:520px;margin:auto;border-radius:16px;border:1px solid rgba(255,255,255,0.1);">
              <h2 style="font-size:1.5rem;margin-bottom:8px;color:#38bdf8;">Reset Your Password</h2>
              <p style="color:#94a3b8;margin-bottom:24px;">Click the button below to reset your Vest password. This link expires in <strong>1 hour</strong>.</p>
              <a href="{resetUrl}"
                 style="display:inline-block;background:#00c2ff;color:#0a0e1a;font-weight:700;text-decoration:none;padding:14px 28px;border-radius:8px;font-size:0.95rem;">
                Reset Password
              </a>
              <p style="color:#94a3b8;font-size:0.8rem;margin-top:24px;">
                If you didn't request this, you can safely ignore this email.<br/>
                Or copy this URL: <a href="{resetUrl}" style="color:#38bdf8;">{resetUrl}</a>
              </p>
            </div>
            """
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(smtp, port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(user, pass);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Password reset email sent to {Email}", toEmail);
    }

    public async Task SendOrderFilledAsync(
        string toEmail, string symbol, string action,
        decimal shares, decimal price, decimal total)
    {
        var smtp        = _config["Email:SmtpHost"]!;
        var port        = int.Parse(_config["Email:SmtpPort"]!);
        var user        = _config["Email:Username"]!;
        var pass        = _config["Email:Password"]!;
        var fromAddress = _config["Email:From"]!;
        var fromName    = _config["Email:FromName"] ?? "Vest";

        var isBuy      = action == "BUY";
        var actionWord = isBuy ? "Bought" : "Sold";
        var accentColor = isBuy ? "#10b981" : "#ef4444";
        var emoji      = isBuy ? "🟢" : "🔴";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress(toEmail, toEmail));
        message.Subject = $"{emoji} Limit order filled — {actionWord} {shares:N4} {symbol} @ ${price:N2}";

        message.Body = new TextPart("html")
        {
            Text = $"""
            <div style="font-family:Inter,sans-serif;background:#0a0e1a;color:#fff;padding:40px;max-width:520px;margin:auto;border-radius:16px;border:1px solid rgba(255,255,255,0.1);">
              <h2 style="font-size:1.4rem;margin-bottom:4px;color:{accentColor};">{emoji} Limit Order Filled</h2>
              <p style="color:#94a3b8;margin-bottom:24px;font-size:0.9rem;">Your limit order has been executed on Vest.</p>
              <table style="width:100%;border-collapse:collapse;margin-bottom:24px;">
                <tr style="border-bottom:1px solid rgba(255,255,255,0.08);">
                  <td style="padding:10px 0;color:#94a3b8;font-size:0.85rem;">Symbol</td>
                  <td style="padding:10px 0;text-align:right;font-weight:700;">{symbol}</td>
                </tr>
                <tr style="border-bottom:1px solid rgba(255,255,255,0.08);">
                  <td style="padding:10px 0;color:#94a3b8;font-size:0.85rem;">Action</td>
                  <td style="padding:10px 0;text-align:right;font-weight:700;color:{accentColor};">{actionWord}</td>
                </tr>
                <tr style="border-bottom:1px solid rgba(255,255,255,0.08);">
                  <td style="padding:10px 0;color:#94a3b8;font-size:0.85rem;">Shares</td>
                  <td style="padding:10px 0;text-align:right;font-weight:700;">{shares:N4}</td>
                </tr>
                <tr style="border-bottom:1px solid rgba(255,255,255,0.08);">
                  <td style="padding:10px 0;color:#94a3b8;font-size:0.85rem;">Fill price</td>
                  <td style="padding:10px 0;text-align:right;font-weight:700;">${price:N2}</td>
                </tr>
                <tr>
                  <td style="padding:10px 0;color:#94a3b8;font-size:0.85rem;">Total value</td>
                  <td style="padding:10px 0;text-align:right;font-weight:700;font-size:1.1rem;">${total:N2}</td>
                </tr>
              </table>
              <a href="https://localhost:5230/Dashboard"
                 style="display:inline-block;background:#00c2ff;color:#0a0e1a;font-weight:700;text-decoration:none;padding:14px 28px;border-radius:8px;font-size:0.95rem;">
                View Dashboard
              </a>
              <p style="color:#94a3b8;font-size:0.75rem;margin-top:24px;">
                This is an automated notification from Vest paper trading. You can turn off email notifications in your account settings.
              </p>
            </div>
            """
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(smtp, port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(user, pass);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Order filled email sent to {Email} — {Action} {Shares} {Symbol} @ {Price}",
            toEmail, action, shares, symbol, price);
    }
}
