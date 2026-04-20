# Azure Marketplace Sandbox

A local sandbox that mimics the Azure Marketplace **Fulfillment API v2**, **Operations API**, and **Metering API**. Test your ISV SaaS integration locally before hitting the real Microsoft APIs.

## Quick Start

```bash
dotnet run --project src/AzureMarketplaceSandbox
```

The app starts on `https://localhost:5050` (default). Open the browser to access the Admin UI.

On first run, two default offers are seeded automatically:
- **contoso-saas-offer** — three plans (free, silver, gold) with metering dimensions (API Calls, Storage, Compute Hours)
- **radiusaas-transactable-prod-preview** — two plans (v4, v5) with metering dimensions for base fees, additional users, and sub-subscriptions

## Pointing Your Client at the Sandbox

Replace the Microsoft API base URL in your client:

```
https://marketplaceapi.microsoft.com  →  https://localhost:5050
```

All API routes are identical. The sandbox accepts any `Bearer <token>` in the Authorization header for API calls. The Admin UI requires Entra ID (OIDC) authentication.

## API Endpoints

### Fulfillment Subscription API (`/api/saas/subscriptions`)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/resolve?api-version=2018-08-31` | Resolve marketplace token |
| POST | `/{id}/activate?api-version=2018-08-31` | Activate subscription |
| GET | `/?api-version=2018-08-31` | List all subscriptions |
| GET | `/{id}?api-version=2018-08-31` | Get subscription |
| GET | `/{id}/listAvailablePlans?api-version=2018-08-31` | List available plans |
| PATCH | `/{id}?api-version=2018-08-31` | Change plan or quantity |
| DELETE | `/{id}?api-version=2018-08-31` | Cancel subscription |

### Operations API (`/api/saas/subscriptions/{id}/operations`)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/?api-version=2018-08-31` | List pending operations |
| GET | `/{opId}?api-version=2018-08-31` | Get operation status |
| PATCH | `/{opId}?api-version=2018-08-31` | Update operation (Success/Failure) |

### Metering API

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/usageEvent?api-version=2018-08-31` | Post single usage event |
| POST | `/api/batchUsageEvent?api-version=2018-08-31` | Post batch usage events (max 25) |
| GET | `/api/usageEvents?api-version=2018-08-31` | Retrieve usage events |

## Admin UI

The Blazor-based Admin UI provides:

- **Dashboard** — subscription counts by status, recent operations
- **Offers** — create/edit offers, plans, and metering dimensions
- **Subscriptions** — create subscriptions (simulate purchases), manage lifecycle (activate, suspend, reinstate, unsubscribe), change plan/quantity
- **Metering** — view usage event log with filters
- **Webhooks** — manually trigger webhook events, view delivery log with payload details
- **Landing Page** — generate marketplace tokens, simulate the landing page redirect flow

## Configuration

Edit `src/AzureMarketplaceSandbox/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=marketplace-sandbox;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<your-tenant-id>",
    "ClientId": "<your-client-id>",
    "CallbackPath": "/signin-oidc"
  },
  "Sandbox": {
    "WebhookUrl": "https://localhost:7100/api/webhook",
    "LandingPageUrl": "https://localhost:7100/landing",
    "BaseUrl": "https://localhost:5050"
  }
}
```

| Setting | Description |
|---------|-------------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `AzureAd:TenantId` | Entra ID tenant for Admin UI login |
| `AzureAd:ClientId` | Entra ID app registration client ID |
| `Sandbox:WebhookUrl` | URL where the sandbox sends webhook POST requests |
| `Sandbox:LandingPageUrl` | Your app's landing page URL for token redirect |
| `Sandbox:BaseUrl` | The sandbox's own base URL (used in Operation-Location headers) |

The API bearer token and publisher ID are **per-tenant** and managed from the
`/settings` page. Each Entra user gets their own tenant — isolated data, own
bearer token, own publisher ID — on first login.

## Azure Deployment

To deploy as an Azure Web App:

1. Set `ConnectionStrings:DefaultConnection` to your Azure SQL connection string
2. Configure `AzureAd:TenantId` and `AzureAd:ClientId` for Admin UI authentication
3. Configure GitHub Secrets: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`
4. Configure GitHub Variable: `AZURE_WEBAPP_NAME`
5. Push to `main` — the `ci-cd.yml` workflow handles the rest

Database migrations are applied automatically on startup.
