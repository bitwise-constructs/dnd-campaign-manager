# Arcane Ledger — D&D Campaign Manager

A clean-architecture web application for managing D&D campaigns between and during sessions. Built with **ASP.NET Core 8** (C#) and **React + TypeScript**, authenticated via **Microsoft Entra ID**.

## Architecture

```
┌──────────────────────────────────────────────────┐
│                    API Layer                      │
│  Controllers · Middleware · Auth Configuration    │
├──────────────────────────────────────────────────┤
│               Application Layer                   │
│  MediatR Handlers · Validators · DTOs · Mappers  │
├──────────────────────────────────────────────────┤
│                 Domain Layer                      │
│  Entities · Enums · Value Objects (no deps)       │
├──────────────────────────────────────────────────┤
│             Infrastructure Layer                  │
│  EF Core DbContext · Identity · Repositories      │
└──────────────────────────────────────────────────┘
```

**Dependency rule**: each layer only references the layer directly below it. Domain has zero external dependencies.

### Key Patterns
- **CQRS via MediatR** — every action is a discrete Command or Query
- **FluentValidation pipeline** — automatic request validation before handlers run
- **AutoMapper projections** — entities map to DTOs without leaking domain concerns
- **Feature folders** — each feature (Characters, MagicItems, Wishlists, TreasureTables) is self-contained, making it easy to add new features

## Features (v1)

| Feature | Player | DM |
|---|---|---|
| View party roster | ✓ | ✓ |
| Edit own character | ✓ | ✓ (all) |
| Magic item wishlist | ✓ (own) | ✓ (view all) |
| Magic items collection | read | full CRUD |
| Treasure table generator | — | ✓ |
| Roll on treasure tables | ✓ | ✓ |

### Treasure Table Generator
The DM's core tool. From the Magic Items page:
1. Check boxes next to items to include
2. Assign relative weights (higher = more likely)
3. Name the table and generate
4. The system calculates d100 roll ranges proportional to weights
5. Roll directly from the table view

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB works for dev)
- A [Microsoft Entra ID](https://entra.microsoft.com/) tenant (free tier works)

## Entra ID Setup

You need **two** app registrations: one for the API, one for the SPA.

### 1. API App Registration
1. Go to [Entra ID → App registrations → New registration](https://entra.microsoft.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade)
2. Name: `DndCampaignManager-API`
3. Supported account types: **Accounts in this organizational directory only**
4. No redirect URI needed
5. After creation:
   - Go to **Expose an API** → Set Application ID URI (accept the default `api://{client-id}`)
   - Add a scope: `access_as_user` (admin consent, display name "Access D&D Campaign Manager")
   - Go to **App roles** → Create:
     - Display name: `DM`, Value: `DM`, Allowed member types: Users/Groups
     - Display name: `Player`, Value: `Player`, Allowed member types: Users/Groups

### 2. SPA App Registration
1. New registration → Name: `DndCampaignManager-SPA`
2. Supported account types: same directory
3. Redirect URI: **Single-page application** → `http://localhost:5173`
4. After creation:
   - Go to **API permissions** → Add → My APIs → select `DndCampaignManager-API` → select `access_as_user`
   - Grant admin consent

### 3. Assign Roles to Users
1. Go to **Enterprise applications** → find `DndCampaignManager-API`
2. **Users and groups** → Add → assign yourself the `DM` role
3. Assign your players the `Player` role

### 4. Update Configuration
API — `src/API/appsettings.json`:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<your-tenant-id>",
    "ClientId": "<api-client-id>",
    "Audience": "api://<api-client-id>"
  }
}
```

SPA — `client-app/.env`:
```
VITE_AZURE_TENANT_ID=<your-tenant-id>
VITE_AZURE_CLIENT_ID=<spa-client-id>
VITE_API_CLIENT_ID=<api-client-id>
```

---

## Running Locally

### Backend
```bash
cd src/API

# Apply EF migrations (first time)
dotnet ef migrations add InitialCreate -p ../Infrastructure -s .
dotnet ef database update -p ../Infrastructure -s .

# Run
dotnet run
# API at https://localhost:7001, Swagger at https://localhost:7001/swagger
```

### Frontend
```bash
cd client-app
cp .env.example .env   # fill in your Entra IDs
npm install
npm run dev
# App at http://localhost:5173
```

---

## Adding New Features

The clean architecture makes this straightforward. For example, to add a **Session Notes** feature:

1. **Domain** — add `SessionNote` entity in `Domain/Entities/`
2. **Application** — create `Features/SessionNotes/` with Commands and Queries
3. **Infrastructure** — add `DbSet<SessionNote>` to `ApplicationDbContext`, add EF config
4. **API** — add `SessionNotesController`
5. **Client** — add components in `components/session-notes/`

Each step is isolated. The feature folder pattern means you won't touch existing code.

---

## Project Structure

```
dnd-campaign-manager/
├── DndCampaignManager.sln
├── src/
│   ├── Domain/                     # Zero-dependency domain model
│   │   ├── Entities/               # Campaign, Character, MagicItem, etc.
│   │   └── Enums/                  # Rarity, ItemCategory, CharacterClass
│   ├── Application/                # Business logic (MediatR CQRS)
│   │   ├── Common/
│   │   │   ├── Interfaces/         # IApplicationDbContext, ICurrentUserService
│   │   │   ├── Models/             # DTOs
│   │   │   └── MappingProfile.cs   # AutoMapper config
│   │   ├── Behaviors/              # Validation pipeline
│   │   ├── Features/
│   │   │   ├── Characters/         # Commands + Queries
│   │   │   ├── MagicItems/
│   │   │   ├── Wishlists/
│   │   │   └── TreasureTables/
│   │   └── DependencyInjection.cs
│   ├── Infrastructure/             # External concerns
│   │   ├── Persistence/            # EF Core DbContext + config
│   │   ├── Identity/               # CurrentUserService (Entra claims)
│   │   └── DependencyInjection.cs
│   └── API/                        # HTTP layer
│       ├── Controllers/
│       ├── Middleware/              # Global exception handling
│       ├── Program.cs              # App bootstrap + auth setup
│       └── appsettings.json
└── client-app/                     # React SPA
    ├── src/
    │   ├── components/
    │   │   ├── layout/             # AppShell (nav, auth chrome)
    │   │   ├── characters/         # Party roster
    │   │   ├── magic-items/        # Item table with checkboxes
    │   │   ├── wishlists/          # Per-character wishlists
    │   │   ├── treasure-tables/    # Generator + roller
    │   │   └── common/             # RarityBadge, shared UI
    │   ├── services/
    │   │   ├── authConfig.ts       # MSAL / Entra config
    │   │   └── api.ts              # Typed API client
    │   ├── context/                # CampaignContext
    │   ├── hooks/                  # useApi
    │   ├── types/                  # TypeScript types mirroring DTOs
    │   └── App.tsx                 # Main app (includes demo data)
    └── .env.example
```

## Tech Stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core 8, MediatR, FluentValidation, AutoMapper |
| Database | EF Core 8 + SQL Server |
| Auth | Microsoft Identity Web (Entra ID / Azure AD) |
| Frontend | React 18, TypeScript, MSAL.js |
| Build | Vite |
