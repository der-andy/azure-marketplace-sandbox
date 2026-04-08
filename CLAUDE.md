# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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
  Domain/Models/                — EF entities matching MS API response shapes (JsonPropertyName)
  Domain/Enums/                 — SaasSubscriptionStatus, OperationAction, OperationStatus, UsageEventStatus
  Data/MarketplaceDbContext.cs  — EF Core DbContext
  Data/Migrations/              — Auto-generated EF migrations
  Api/                          — Minimal API endpoint groups (Fulfillment, Operations, Metering)
  Api/Middleware/                — ApiVersion + RequestHeader middleware
  Auth/                         — SandboxBearerHandler (accepts any Bearer token)
  Services/                     — Business logic (SubscriptionService, OperationService, MeteringService, WebhookService, TokenService)
  Configuration/                — Options classes bound from appsettings.json
  Components/                   — Blazor pages and layout
```

API routes are identical to Microsoft's (`/api/saas/subscriptions/...`, `/api/usageEvent`, etc.) — ISV clients only need to change the base URL.

## Key Design Decisions

- Domain models use `[JsonPropertyName]` to exactly match Microsoft API response shapes
- Enums stored as strings in the database via `HasConversion<string>()`
- Auth is a sandbox pass-through: any `Bearer <token>` header is accepted
- Database provider selectable via `DatabaseProvider` config: `"Sqlite"` (default) or `"SqlServer"`

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

## Output Language

- IMPORTANT: ALL written output must be in English — no exceptions:
  - Code, comments, variable names
  - Commit messages, PR descriptions
  - Documentation, feature specs, context entries, PRD
  - Error messages, log messages, UI text
- Conversation with the user may be in any language the user prefers