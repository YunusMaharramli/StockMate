# StockMate

Telegram bot and Telegram Mini App for running a small inventory and order business: products and categories, stock purchases, customer carts and orders, partial deliveries, shipments and reports.

## Features

- **Telegram bot + Mini App** (React, TypeScript) for staff and customers, with invite-based onboarding
- **Inventory and orders:** product catalog with categories and subcategories, stock purchases, shortages, order assignment to staff, partial delivery and shipments
- **Reliability:** idempotent operations, optimistic concurrency and a transactional outbox processed by a background service
- **Security:** Telegram Mini App init-data validation, role-based staff access, customer blocking, protected downloads
- **Reports:** Excel exports with ClosedXML
- **Tests:** domain logic covered by xUnit tests

## Tech stack

.NET 10 · ASP.NET Core · Entity Framework Core · PostgreSQL · Telegram.Bot · React · TypeScript · Vite · Docker · xUnit

## Project structure

| Project | Purpose |
| --- | --- |
| `StockMate.Domain` | Entities, enums and business rules |
| `StockMate.Application` | Use-case services, idempotency and outbox contracts |
| `StockMate.Infrastructure` | EF Core persistence, migrations and configurations |
| `StockMate.Bot` | ASP.NET Core host: Telegram bot, Mini App API, outbox processor |
| `StockMate.MiniApp` | React + TypeScript Telegram Mini App |
| `StockMate.Domain.Tests` | xUnit tests for the domain |

## Running locally

1. Copy `.env.example` to `.env` and set `STOCKMATE_DB_PASSWORD`.
2. Start PostgreSQL: `docker compose up -d`
3. Copy `StockMate.Bot/appsettings.example.json` to `appsettings.json` and set the connection string, `Telegram:BotToken`, `Telegram:AdminUserId` and `Telegram:MiniAppUrl`.
4. Run the API and bot: `dotnet run --project StockMate.Bot`
5. Run the Mini App: `cd StockMate.MiniApp && npm install && npm run dev`

Run the tests with `dotnet test`.
