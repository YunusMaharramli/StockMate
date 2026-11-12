# StockMate

[![CI](https://github.com/YunusMaharramli/StockMate/actions/workflows/ci.yml/badge.svg)](https://github.com/YunusMaharramli/StockMate/actions/workflows/ci.yml)

**A Telegram bot and Telegram Mini App for running a small inventory and order business.**

Staff manage products, stock and orders inside Telegram. Customers browse the catalog, fill a cart and place orders without installing anything. A single ASP.NET Core host runs the bot, the Mini App API and a background outbox processor.

Solo project · Aug – Sep 2026 · deployed on Render with Docker

---

## What it does

| Area | Features |
| --- | --- |
| **Catalog** | Products with categories and subcategories, product images, stock levels |
| **Stock** | Stock purchases, shortage tracking |
| **Orders** | Customer carts, orders, assignment to staff, partial delivery, shipments with delivery companies |
| **Customers** | Customer profiles and addresses, customer blocking |
| **Staff** | Invite-based onboarding, role-based access |
| **Reports** | Excel exports (ClosedXML) |

Access is invite-based: the admin sends a link, and new staff join through Telegram.

## How it is built

**Reliability**
- **Idempotent operations:** a repeated request (for example a double tap in Telegram) does not create a second order.
- **Optimistic concurrency:** two staff members editing the same order cannot silently overwrite each other.
- **Transactional outbox:** side effects are saved in the same database transaction as the change, then a background service processes them. Nothing is lost if the app restarts.

**Security**
- Telegram Mini App init data is validated on every request, so the API knows the request really comes from Telegram and from which user.
- Role-based staff access, customer blocking and protected file downloads.

## Architecture

Clean Architecture. Dependencies point inward, toward the domain.

```
StockMate.Bot  ──►  StockMate.Application  ──►  StockMate.Domain
      │                                               ▲
      └──────────►  StockMate.Infrastructure ─────────┘
```

| Project | Responsibility |
| --- | --- |
| `StockMate.Domain` | Entities, enums and business rules |
| `StockMate.Application` | Use-case services, idempotency and outbox contracts |
| `StockMate.Infrastructure` | EF Core persistence, migrations, repositories |
| `StockMate.Bot` | ASP.NET Core host: Telegram bot, Mini App API controllers, outbox processor |
| `StockMate.MiniApp` | React + TypeScript Telegram Mini App |
| `StockMate.Domain.Tests` | xUnit tests for domain rules |

## Tech stack

.NET 10 · ASP.NET Core · Entity Framework Core · PostgreSQL · Telegram.Bot · ClosedXML · React · TypeScript · Vite · Docker · xUnit

## Run locally

**Requirements:** .NET 10 SDK, Node.js, Docker, a Telegram bot token from [@BotFather](https://t.me/BotFather).

1. Copy `.env.example` to `.env` and set `STOCKMATE_DB_PASSWORD`.
2. Start PostgreSQL:
   ```bash
   docker compose up -d
   ```
3. Copy `StockMate.Bot/appsettings.example.json` to `StockMate.Bot/appsettings.json` and fill in:
   - `ConnectionStrings` — the PostgreSQL connection string
   - `Telegram:BotToken` — your bot token
   - `Telegram:AdminUserId` — your Telegram user ID
   - `Telegram:MiniAppUrl` — the public URL of the Mini App
4. Run the API and bot:
   ```bash
   dotnet run --project StockMate.Bot
   ```
5. Run the Mini App:
   ```bash
   cd StockMate.MiniApp
   npm install
   npm run dev
   ```

## Tests

```bash
dotnet test
```

The tests cover domain rules for products, categories, customers, orders, order assignment and delivery companies.

## Deployment

The `Dockerfile` builds and publishes `StockMate.Bot` into an ASP.NET Core runtime image that listens on port `8080`. The production instance runs on Render.
