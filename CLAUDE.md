# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Rules in `.claude/rules/` are mandatory and must be followed without exception.** They take precedence over any conflicting guidance in this file.

## Purpose

A local sandbox that mimics the Azure Marketplace **Fulfillment API v2**, **Operations API**, and **Metering API**, so ISV API clients can be tested against it before hitting the real Microsoft APIs. A Blazor-based Admin UI provides Partner Center-like configuration and manual webhook triggering.

API specifications:
- Fulfillment Subscription API: https://learn.microsoft.com/en-us/partner-center/marketplace-offers/pc-saas-fulfillment-subscription-api
- Fulfillment Operations API: https://learn.microsoft.com/en-us/partner-center/marketplace-offers/pc-saas-fulfillment-operations-api
- Metering API: https://learn.microsoft.com/en-us/partner-center/marketplace-offers/marketplace-metering-service-apis
- Webhook: https://learn.microsoft.com/en-us/partner-center/marketplace-offers/pc-saas-fulfillment-webhook
- Lifecycle: https://learn.microsoft.com/en-us/partner-center/marketplace-offers/pc-saas-fulfillment-life-cycle

## Tech Stack

- **.NET 10**, Blazor Web App with Interactive Server rendering
- **Single project** — `dotnet run` starts both API and Admin UI
- **Minimal APIs** with `MapGroup` for REST endpoints
- **Entity Framework Core** — SQLite locally, Azure SQL in production
- DB migrations run automatically on startup (`Database.Migrate()`)
- **xUnit** + `WebApplicationFactory` for integration and unit tests
- **GitHub Actions** CI/CD — build, test, deploy to Azure Web App

## Common Commands

```bash
dotnet build                    # Build the solution
dotnet run --project src/AzureMarketplaceSandbox  # Run the app
dotnet test                     # Run all tests
dotnet ef migrations add <Name> --project src/AzureMarketplaceSandbox --output-dir Data/Migrations  # Add migration
```

## Architecture

```
src/AzureMarketplaceSandbox/
  Program.cs                    — Host setup, DI, middleware, endpoint mapping
  Api/
    FulfillmentSubscriptionEndpoints.cs — /api/saas/subscriptions CRUD
    FulfillmentOperationsEndpoints.cs   — /api/saas/subscriptions/{id}/operations
    MeteringEndpoints.cs                — /api/usageEvent
    Middleware/                  — ApiVersionMiddleware, RequestHeaderMiddleware
  Auth/
    SandboxBearerHandler.cs     — Accepts any Bearer token (sandbox mode)
  Configuration/
    SandboxOptions.cs           — SandboxOptions, AuthOptions, SeedDataOptions
  Data/
    MarketplaceDbContext.cs     — EF Core DbContext
    SeedDataService.cs          — IHostedService that seeds demo data on startup
    Migrations/                 — Auto-generated EF migrations
  Domain/
    Enums/                      — SaasSubscriptionStatus, OperationAction, OperationStatus, UsageEventStatus
    Models/                     — EF entities (Subscription, Offer, Plan, Operation, UsageEvent, SubscriptionTerm, WebhookPayload, etc.)
  Services/
    SubscriptionService.cs      — Subscription lifecycle logic
    OperationService.cs         — Async operation management
    MeteringService.cs          — Usage event processing
    WebhookService.cs           — Webhook delivery + logging
    TokenService.cs             — Marketplace token resolve
  Components/
    App.razor, Routes.razor     — Blazor root components
    Layout/                     — MainLayout, NavMenu, ReconnectModal
    Pages/
      Home.razor, Error.razor, NotFound.razor
      LandingPage/              — LandingPageSimulator.razor
      Offers/                   — OfferList, OfferEdit
      Subscriptions/            — SubscriptionList, SubscriptionDetail, CreateSubscription
      Metering/                 — UsageLog
      Webhooks/                 — WebhookTester

tests/AzureMarketplaceSandbox.Tests/
  Api/                          — Integration tests (FulfillmentSubscription, FulfillmentOperations, Metering)
  Services/                     — Unit tests (SubscriptionService)
  Infrastructure/               — SandboxWebApplicationFactory, TokenProtectedWebApplicationFactory
```

API routes are identical to Microsoft's (`/api/saas/subscriptions/...`, `/api/usageEvent`, etc.) — ISV clients only need to change the base URL.

## Key Design Decisions

- Domain models use `[JsonPropertyName]` to exactly match Microsoft API response shapes
- Enums stored as strings in the database via `HasConversion<string>()`
- Auth is a sandbox pass-through: any `Bearer <token>` header is accepted (configurable via `AuthOptions.RequiredToken`)
- Database provider selectable via `DatabaseProvider` config: `"Sqlite"` (default) or `"SqlServer"`
- Tests use `WebApplicationFactory` with EF Core InMemory provider — no external dependencies needed

## CI/CD

GitHub Actions workflow (`.github/workflows/ci-cd.yml`):
- **build** — restore, build (Release), test — runs on every push/PR to `main`
- **deploy** — publish + deploy to Azure Web App — runs after build, requires `production` environment

## Permissions

### Allowed (no confirmation needed)

- Create, edit, delete, rename, and move files within the project
- Run shell commands that only affect the project directory
- Install project-local dependencies (npm install, pip install in venv, etc.)
- Run build processes, linters, formatters, and tests
- Git operations (commit, branch, merge, rebase, etc.)
- Start dev servers, open local ports
- Create/modify configuration files within the project
- Read files and directories outside the project (read-only)

### Not allowed

- Modify or delete files outside the project directory
- Install global packages or tools (npm install -g, brew install, apt install, etc.)
- Modify system-wide configurations (~/.bashrc, ~/.gitconfig, /etc/*, etc.)
- Run destructive commands (rm -rf, drop database, force push to main) without explicit confirmation
- Start, stop, or configure system services
- Change network or firewall settings
- Set environment variables outside the project

### Rule of thumb

> **Inside the project: everything is allowed. Outside the project: read-only. System-level: nothing.**

If a task requires tools, packages, or system changes not already present: stop and ask before proceeding.

## Coding Principles

- Use only dependencies already in the project. If a new dependency is needed: ask first, explain why, and suggest alternatives
- Don't refactor unrelated code while working on a task — stay focused
- When fixing bugs: fix the root cause, not the symptom. Add a regression test