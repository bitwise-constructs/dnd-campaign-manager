# CLAUDE.md

Project context for AI agents working on the D&D Campaign Manager codebase.

## What this is

A web application for managing D&D campaigns between and during sessions. The DM uses it to manage magic items, treasure tables, and view party info. Players use it to manage their characters and wishlists. Characters sync from D&D Beyond via an undocumented API.

## Tech stack

- **Backend**: ASP.NET Core 8, C#, EF Core 8 + SQL Server
- **Frontend**: React 18, TypeScript, Vite
- **Auth**: Microsoft Entra ID (Azure AD) via MSAL.js (SPA) and Microsoft.Identity.Web (API)
- **Infrastructure**: Terraform on Azure (App Service, SQL, Static Web App)
- **Patterns**: Clean Architecture, CQRS via MediatR, FluentValidation, AutoMapper

## Architecture — the dependency rule

```
API → Application → Domain ← Infrastructure
```

Domain has zero dependencies. Application references only Domain. API and Infrastructure both reference Application. Infrastructure implements the interfaces that Application defines. Never add a reference that violates this direction.

## Project structure

```
src/
  Domain/           # Entities, enums, value objects. No external dependencies.
  Application/      # MediatR commands/queries, validators, DTOs, interfaces.
    Features/       # One folder per feature (Characters, MagicItems, Wishlists, etc.)
    Common/         # Shared interfaces, models, mapping profiles.
    Behaviors/      # MediatR pipeline behaviors (validation).
  Infrastructure/   # EF Core, external services, identity.
    Persistence/    # DbContext, entity configurations.
    DndBeyond/      # DDB character import service.
    Services/       # Magic item search service (Open5e, dnd5eapi.co).
    Identity/       # CurrentUserService (reads Entra JWT claims).
  API/              # Controllers, middleware, Program.cs.
client-app/         # React SPA.
  src/
    components/     # UI components grouped by feature.
    services/       # API client, MSAL auth config.
    types/          # TypeScript types mirroring backend DTOs.
    context/        # React context (campaign selection).
    hooks/          # Custom hooks (useApi).
infra/              # Terraform configs for Azure deployment.
```

## How to add a new feature

This is the most common task. Follow this exact pattern:

1. **Domain** — Add entity in `Domain/Entities/`. Inherit from `BaseEntity` (gives you Id, CreatedAt, UpdatedAt). Add any new enums in `Domain/Enums/Enums.cs`.

2. **Application** — Create `Features/YourFeature/` folder. Add Commands (write operations) and Queries (read operations) as MediatR `IRequest<T>` records with their handlers. Add validators as `AbstractValidator<T>`. Add DTOs in `Common/Models/Dtos.cs`.

3. **Infrastructure** — Add `DbSet<YourEntity>` to both `IApplicationDbContext` and `ApplicationDbContext`. Add entity configuration in `ApplicationDbContext.OnModelCreating`. If you need an external service, define the interface in `Application/Common/Interfaces/` and implement it in `Infrastructure/Services/`.

4. **API** — Add a controller in `API/Controllers/`. Use `[Authorize]` for authenticated endpoints, `[Authorize(Policy = "DmOnly")]` for DM-restricted endpoints. Inject `IMediator` and dispatch commands/queries.

5. **Frontend** — Add TypeScript types in `types/index.ts`. Add API methods in `services/api.ts`. Add components in `components/your-feature/`.

6. **Terraform** — If the feature needs new Azure resources, add them in `infra/main.tf`.

## Key entities and relationships

```
Campaign (1) ──→ (N) Character
Campaign (1) ──→ (N) MagicItem
Campaign (1) ──→ (1) DmItemPool
Campaign (1) ──→ (N) TreasureTable

Character (1) ──→ (1) CharacterPrivacySettings
Character (1) ──→ (N) WishlistItem
Character (1) ──→ (N) InventoryItem

DmItemPool (1) ──→ (N) WishlistItem

TreasureTable (1) ──→ (N) TreasureTableEntry ──→ MagicItem

WishlistItem ──→ MagicItem (nullable, for linked items)
WishlistItem ──→ Character (nullable, for player wishlists)
WishlistItem ──→ DmItemPool (nullable, for DM general pool)
```

A WishlistItem belongs to EITHER a Character's wishlist OR the DM's pool (never both). It links to EITHER a known MagicItem OR carries custom plain-text fields (CustomItemName, CustomItemRarity). Check `IsCustom` / `MagicItemId == null` to distinguish.

## Critical design decisions

### Privacy model

Character sheet fields are gated by `CharacterPrivacySettings`. The privacy filtering happens server-side in `Features/Characters/Queries/GetCharacters.cs`. The query handler checks viewer identity and returns `null` for redacted fields. Rules:

- The **owning player** always sees their full sheet and their privacy settings.
- The **DM** always sees everything regardless of privacy settings.
- **Other players** see only fields the owner has toggled on.
- Each field is independently togglable. `ShowAll` overrides everything to public.
- Default is **everything private**.

Never return unfiltered Character data from a query. Always go through the `ToPrivacyAwareDto` helper or equivalent.

### D&D Beyond integration

DDB has no official API. We use the undocumented `character-service.dndbeyond.com/character/v5/character/{id}` endpoint. It can break at any time. The design handles this:

- On successful sync: character data is written to entity fields AND the raw JSON is stored in `DndBeyondRawJson`.
- On failed sync: status flips to `SyncFailed`, error message is stored, but ALL previously cached data is preserved. The UI shows stale data with a warning banner.
- JSON upload fallback: players can paste the raw JSON when the API is down.
- Inventory sync: DDB-sourced items (`DndBeyondItemId != null`) are fully replaced on each sync. Manually-added items (`DndBeyondItemId == null`) are preserved.
- Traits: DDB's `traits.personalityTraits/ideals/bonds/flaws` are extracted when available but never overwrite local values with null (DDB sometimes omits them for privacy).
- The base URL is configurable in `appsettings.json` under `DndBeyond:CharacterServiceBaseUrl` for when DDB inevitably moves it.

### WishlistItem dual-mode

WishlistItem supports both linked items (FK to MagicItem) and custom plain-text entries:

- **Linked**: `MagicItemId` is set, `CustomItemName` is null. Display name comes from the MagicItem entity.
- **Custom**: `MagicItemId` is null, `CustomItemName` is set. These don't sync to any known entity. Players and DMs can add free-text items.
- `DisplayName` computed property handles both cases.
- `Weight` field (default 1) is DM-assigned for treasure table generation probability.

### Magic item search

`IMagicItemSearchService` queries three sources in parallel: local campaign DB, Open5e (`api.open5e.com`), and dnd5eapi.co. Results are merged, deduplicated by name (local wins), and each result carries a `searchSource` tag. When a player adds a search result to their wishlist, if it came from an external source it's added as a custom item (no local MagicItem entity created). Only the DM creates MagicItem entities in the campaign collection.

### Authentication and authorization

Two Entra app registrations: API (validates JWT bearer tokens) and SPA (acquires tokens via MSAL popup). The API uses `Microsoft.Identity.Web` middleware. Roles are Entra App Roles (`DM`, `Player`), enforced via `[Authorize(Policy = "DmOnly")]` on controllers.

`ICurrentUserService` extracts the user's Entra Object ID from the `oid` JWT claim. `IsDm(campaignId)` checks whether the current user is the DM of a specific campaign by querying `Campaign.DmUserId`.

### Treasure table generation

The DM selects items (from both the general pool and player wishlists) via checkboxes, assigns weights, then either:
- **Send to generator**: creates a persisted `TreasureTable` with `TreasureTableEntry` rows and d100 roll ranges proportional to weights.
- **Pick top N**: weighted random selection without creating a persisted table. Rolls N times with replacement.

The "Include weights" toggle makes Pick top N use equal odds instead.

## Conventions

### Backend

- Every write operation is a MediatR Command (`IRequest<T>` or `IRequest`). Every read is a Query.
- Validation goes in FluentValidation validators, not in handlers.
- Exceptions are mapped to HTTP status codes by `ExceptionHandlingMiddleware`: `ValidationException` → 400, `KeyNotFoundException` → 404, `UnauthorizedAccessException` → 403, `InvalidOperationException` → 409.
- Controller methods are thin — they dispatch to MediatR and return the result.
- Entity configuration goes in `ApplicationDbContext.OnModelCreating`, not in data annotations on entities.
- Connection strings use the `ConnectionStrings:DefaultConnection` config key.

### Frontend

- TypeScript types mirror backend DTOs exactly (camelCase). Keep them in sync manually in `types/index.ts`.
- API calls go through `services/api.ts` which handles MSAL token acquisition automatically.
- Components are grouped by feature in `components/`. Each feature folder has a `.tsx` and `.css` file.
- The app currently uses demo data in `App.tsx` for development. Replace demo handlers with real API calls when connecting to the backend.
- CSS uses custom properties defined in `index.css` (the D&D parchment theme). The design tokens are `--bg-deep`, `--bg-surface`, `--bg-card`, `--text-primary`, `--accent-gold`, `--border`, etc.

### Infrastructure

- All Azure resources are managed by Terraform in `infra/`.
- Sensitive values (SQL password, tenant IDs) go in `terraform.tfvars` which is gitignored.
- The API's app settings and connection string are injected by Terraform, not manually configured.
- Free tier defaults: App Service F1, SQL Serverless Gen5 with auto-pause, Static Web App Free.

## Database notes

- EF Core migrations are in the Infrastructure project. Run from `src/API`:
  ```
  dotnet ef migrations add MigrationName -p ../Infrastructure -s .
  dotnet ef database update -p ../Infrastructure -s .
  ```
- Cascade deletes: Campaign → Characters, Campaign → MagicItems, Character → Wishlist, Character → Inventory, Character → PrivacySettings, DmItemPool → its WishlistItems.
- NoAction deletes: WishlistItem → MagicItem, TreasureTableEntry → MagicItem (prevents cascade conflicts through Campaign).
- `DndBeyondRawJson` and `Backstory` are `nvarchar(max)`. Most string fields have explicit `HasMaxLength`.
- Unique index on `(CampaignId, DndBeyondCharacterId)` filtered for non-null DDB IDs.
- Unique index on `(CharacterId, MagicItemId)` filtered for non-null values (prevents duplicate linked items on a wishlist but allows multiple custom items).

## External dependencies to be aware of

- **D&D Beyond character-service**: undocumented, can change without notice. The URL is in config, the parser is in `Infrastructure/DndBeyond/DndBeyondImportService.cs`. If the JSON schema changes, that's where to fix it.
- **Open5e API** (`api.open5e.com/v1/magicitems/`): public, stable, SRD + OGL content.
- **dnd5eapi.co** (`/api/magic-items`): public, stable, SRD only. We fetch the full list and filter client-side since there's no search endpoint.
- **Microsoft Entra ID**: auth provider. Both the API (`Microsoft.Identity.Web`) and SPA (`@azure/msal-react`) depend on it.

## What's not built yet

These are planned features mentioned in design discussions but not yet implemented:

- Session notes / session log
- Character creation/edit form modal (currently placeholder)
- Encounter tracker
- Initiative roller
- Campaign selector (multi-campaign support — entity model supports it, UI doesn't)
- Bulk magic item import from dnd5eapi.co to seed the campaign collection
- Frontend components for the rebuilt wishlist (DM pool, custom items, weights, Pick top N) — the backend is complete but the React components still use the older WishlistView
