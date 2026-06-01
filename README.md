# Vest

Vest is a stock market web application built using ASP.NET Core MVC. The platform uses Supabase for database management, Finnhub for real-time market data, and Google OAuth for user authentication.

https://vest-1.onrender.com/Dashboard

## Minimum Requirements

To build and run this project locally, you will need the following tools and credentials:

### Development Tools
* **.NET SDK 8.0** or later.
* A suitable IDE such as **Visual Studio 2022**, **JetBrains Rider**, or **Visual Studio Code** (with the C# Dev Kit extension).

### External Services & API Keys
The application depends on multiple external services to function correctly. You will need to configure credentials for the following:

1. **Supabase**: Used as the database and backend REST API.
   * Project URL (`Supabase:Url`)
   * Service Role Key (`Supabase:ServiceRoleKey`)
   * Anon Key (`Supabase:AnonKey`)

2. **Finnhub**: Used for fetching real-time stock and financial data.
   * API Key (`Finnhub:ApiKey`)

3. **Google Cloud Console**: Used for Google OAuth login.
   * OAuth 2.0 Client ID (`Authentication:Google:ClientId`)
   * OAuth 2.0 Client Secret (`Authentication:Google:ClientSecret`)

4. **SMTP Server**: Used by the `EmailService` for sending transactional emails (such as password resets).
   * SMTP Host, Port, Username, and App Password (e.g., via Gmail).

## Configuration

Secrets must **not** be committed. The repo ships a safe `appsettings.json` with empty placeholders.

**Local development:** copy your keys into `appsettings.Development.json` (gitignored). That file is loaded automatically when `ASPNETCORE_ENVIRONMENT=Development`.

**Production:** set environment variables on your host. See [`.env.example`](.env.example) for the full list and naming format (`Section__Key`).

### Required keys

| Config key | Environment variable |
|---|---|
| `Finnhub:ApiKey` | `Finnhub__ApiKey` |
| `Supabase:Url` | `Supabase__Url` |
| `Supabase:ServiceRoleKey` | `Supabase__ServiceRoleKey` |
| `Authentication:Google:ClientId` | `Authentication__Google__ClientId` |
| `Authentication:Google:ClientSecret` | `Authentication__Google__ClientSecret` |
| `Email:*` | `Email__SmtpHost`, `Email__Password`, etc. |
| `Vapid:*` | `Vapid__PublicKey`, `Vapid__PrivateKey`, etc. |
| `AllowedHosts` | `AllowedHosts=yourdomain.com;www.yourdomain.com` |

## Deployment

### Recommended platforms

Vest is an ASP.NET Core 8 app with **background workers** (limit-order executor, risk monitor, leaderboard refresh). Use an **always-on** host:

- **Railway**, **Render**, **Fly.io**, or **Azure App Service** — recommended
- **Vercel** — not recommended (serverless; background services will not run reliably)

### Render (full site)

Render does **not** offer .NET as a native runtime (only Node, Python, Ruby, Go, Rust, Elixir). Use **Docker** instead.

1. Create a **Web Service** (not Static Site), connect `PandaSar07/Vest`.
2. **Language / Environment:** **Docker** (not Node).
3. **Dockerfile path:** `./Dockerfile` (repo root). Leave build/start commands empty — the Dockerfile defines them.
4. **Environment:** add all variables from [`.env.example`](.env.example) in the Render dashboard.
5. Deploy. Optional: use the repo [`render.yaml`](render.yaml) Blueprint.

If you see `ENOENT ... package.json` at `/src/package.json`, you picked **Node** instead of **Docker**, or left a custom `npm run build` at the repo root.

### Vercel environment variables

Vercel does **not** use a file upload for env vars. Use one of these:

1. **Dashboard:** Project → Settings → Environment Variables → add each name/value from [`.env.example`](.env.example)
2. **CLI:** `vercel env add Finnhub__ApiKey` (repeat for each variable)

The reference file to copy variable **names** from is **`.env.example`** in the project root.

Before going live:

1. **Rotate all API keys** that were previously committed to git (Supabase, Google, Gmail, Vapid, Finnhub).
2. Apply Supabase migrations in `supabase/migrations/`.
3. Add your production URL to Google OAuth redirect URIs: `https://yourdomain.com/signin-google`.
4. Set `AllowedHosts` to your domain (required in production — `*` is rejected at startup).
5. Build the dashboard: `cd frontend/dashboard && npm run build`
6. Publish: `dotnet publish -c Release -o ./publish`

### Required Configuration Structure (local)
```json
{
  "Finnhub": {
    "ApiKey": "YOUR_FINNHUB_API_KEY"
  },
  "Supabase": {
    "Url": "YOUR_SUPABASE_URL",
    "AnonKey": "YOUR_SUPABASE_ANON_KEY",
    "ServiceRoleKey": "YOUR_SUPABASE_SERVICE_ROLE_KEY"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "Username": "your_email@gmail.com",
    "Password": "your_app_password",
    "From": "your_email@gmail.com",
    "FromName": "Vest"
  },
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    }
  }
}
```

## Running the Application

1. Open your terminal and navigate to the project root directory:
   ```bash
   cd /path/to/Vest
   ```

2. Restore the necessary NuGet packages:
   ```bash
   dotnet restore
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

4. The application will start and listen on the configured local ports (typically `http://localhost:5xxx` or `https://localhost:7xxx`). Check the terminal output for the exact local URL.

## Architecture & Technologies
* **Framework**: ASP.NET Core 8.0 MVC
* **Authentication**: ASP.NET Core Cookie Authentication & Google OAuth
* **Database & BaaS**: Supabase C# Client (`supabase-csharp`)
* **Security**: `BCrypt.Net-Next` for manual password hashing
* **Emails**: `MailKit` for SMTP dispatch
* **Background Tasks**: The app runs a background hosted service (`OrderExecutorService`) to continuously poll and execute limit orders every 60 seconds.

## Explanations of Error Messages & Troubleshooting

If you encounter issues while running Vest, refer to the following common error messages and their solutions:

### 1. "HTTP Error 500.30 - ANCM In-Process Start Failure"
* **Cause**: The application failed to start, usually due to a missing .NET 8.0 runtime or an unhandled exception during startup (like a malformed `appsettings.json`).
* **Solution**: Ensure the .NET 8.0 SDK is installed. Run `dotnet run` from the terminal to see the exact console output and fix any configuration errors.

### 2. "invalid_client" or "redirect_uri_mismatch" (Google Login)
* **Cause**: Your local URL (e.g., `https://localhost:7xxx`) is not authorized in your Google Cloud Console.
* **Solution**: Go to your Google Cloud Console, find your OAuth 2.0 Client ID, and add your exact local URL to the **Authorized redirect URIs** (e.g., `https://localhost:7xxx/signin-google`).

### 3. Supabase "Unauthorized" or "Invalid API Key"
* **Cause**: The `Supabase:AnonKey` or `Supabase:ServiceRoleKey` in your configuration is incorrect or expired.
* **Solution**: Double-check your Supabase project settings (Project Settings -> API) and ensure the keys match your `appsettings.Development.json` (local) or host environment variables (production).

### 4. Market Data Not Loading (Finnhub)
* **Cause**: You may have hit the Finnhub free-tier API rate limits, or your `Finnhub:ApiKey` is missing.
* **Solution**: Check your configuration for the API key. If you are being rate-limited, wait a minute before refreshing the page.

### 5. Email Fails to Send (Password Reset)
* **Cause**: Incorrect SMTP credentials or Gmail App Passwords.
* **Solution**: Ensure your `Email:Password` is a valid Gmail App Password (not your standard account password) and that 2-Step Verification is enabled on your Google account.

## Contact Information

If an undocumented question arises or you experience a critical system failure not covered in this manual, please reach out to the system developer:

* **Email**: mr.default0330@gmail.com
* **Project Maintainer**: Vest Development Team

Please include screenshots of the error, steps to reproduce the issue, and any relevant console output when reporting a bug.
