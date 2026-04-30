# Vest - User Guide

Welcome to Vest, your premier stock market and portfolio management platform. This guide will help you get started with the system, explain its core features, and provide examples of how to execute trades.

---

## 1. How to Start the System

To begin using Vest, follow these steps:
1. **Navigate to the Application**: Open your preferred web browser and go to the Vest URL provided by your system administrator (e.g., `https://vest-app.com`).
2. **Log In**: Click the "Sign In" button in the top right corner. You can log in securely using your Google Account (via the "Sign in with Google" button) or by entering your registered email and password.
3. **Dashboard Access**: Upon successful login, you will be automatically redirected to your personal **Dashboard**, where you can view your portfolio overview.

---

## 2. Main Features & Navigation

Vest offers several key features designed to give you complete control over your investments. 

### The Dashboard
The Dashboard is your home base. It provides a high-level overview of your account's health.
* **Portfolio Chart**: A visual representation of your balance over time (1D, 1W, 1M, YTD).
* **Holdings Summary**: A quick list of the assets you currently own, their current market price, and your total equity.
* **Recent Activity**: A feed of your latest transactions (buys, sells, and dividends).

![Vest Dashboard Overview](/Users/bensonxue/.gemini/antigravity/brain/9a374502-8bad-4298-b3fa-991403c77f3c/vest_dashboard_1777570564399.png)

### Markets & Trading
The Markets page allows you to research specific stocks, view interactive candlestick charts, and place trades.
* **Search & Watchlist**: Search for any ticker symbol (e.g., `AAPL`) and add it to your Watchlist for quick access.
* **Real-Time Data**: View the current open, high, low, and close prices powered by live market data.
* **Order Ticket**: The panel on the right side of the screen allows you to execute Market or Limit orders.

![Vest Market and Trading Interface](/Users/bensonxue/.gemini/antigravity/brain/9a374502-8bad-4298-b3fa-991403c77f3c/vest_market_1777570622314.png)

---

## 3. Example Inputs & Outputs

### Placing a Market Buy Order
A Market Order buys shares immediately at the current market price.

**Input Example:**
1. Navigate to the **Markets** page and select `AAPL` (Apple Inc.).
2. In the Order Ticket panel, select **BUY**.
3. Set the **Order Type** to `Market`.
4. Enter `10` in the **Shares** input field.
5. Click the green **Submit Order** button.

**Output Example:**
* **Success Message**: "Successfully purchased 10 shares of AAPL."
* **Portfolio Update**: The total cost (e.g., $1,745.20) is deducted from your Available Cash, and 10 shares of AAPL are added to your Portfolio Holdings.

### Placing a Limit Sell Order
A Limit Order sells shares only when the stock reaches your specified target price.

**Input Example:**
1. Select a stock you own, such as `TSLA`.
2. In the Order Ticket panel, select **SELL**.
3. Set the **Order Type** to `Limit`.
4. Enter `5` in the **Shares** input field.
5. Enter `$250.00` in the **Limit Price** field.
6. Click **Submit Order**.

**Output Example:**
* **Status Message**: "Limit order placed: Sell 5 TSLA @ $250.00."
* **Order Book**: The order will appear in your "Pending Orders" tab. Once TSLA hits $250.00, the system will automatically execute the trade, adding $1,250.00 to your cash balance and removing 5 shares from your holdings.

---

If you need further assistance navigating the platform, please check the **Tutorials** page located in the sidebar navigation!
