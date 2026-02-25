# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

"Who Is the Spy" — a multiplayer party game (12–30 players) where civilians share a secret word and the spy gets a similar word. Players discuss and vote to eliminate the spy each round. No accounts required; nicknames only.

**Stack:** .NET 10 Azure Web App + Vanilla JS/jQuery frontend + Azure Storage Tables.

## Build & Run Commands

```bash
# Build backend
dotnet build

# Run locally (Azure Functions Core Tools required)
func start

# Run tests
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Deploy infrastructure
az deployment group create \
  --resource-group <rg-name> \
  --template-file src/iac/azuredeploy.json \
  --parameters @src/iac/azuredeploy.parameters.json
```

**Frontend:** No build step. Files in `src/frontend/` are served directly — no npm, no bundler, no transpilation.

Always include a `nuget.config` in the project.

## Architecture

The Azure Web App both **hosts the frontend** and **exposes secure APIs**. All game state lives in Azure Storage Tables; the frontend polls `/state` endpoints for real-time updates (no WebSocket).

### Backend: `src/lib/{task}/`

Each subfolder owns all code for one domain. Task folders **only import from `src/lib/shared`** — never from sibling task folders.

| Folder | Responsibility |
|--------|----------------|
| `room` | Room creation, shareable code/link, `AdminPass` header check |
| `player` | Nickname registration, join/leave, player state |
| `round` | Full round lifecycle: start, word assignment, turn timer, phase advancement (`Discussion` → `Voting` → `Ended`) |
| `vote` | Submit vote, tally results, elimination logic |
| `state` | Game-state snapshot endpoints polled by the frontend |
| `shared` | Cross-cutting types, interfaces, enums, table entity models |

**Pattern:** Minimal APIs only — each task folder has one `*Endpoints.cs` that maps routes and calls a service. No business logic in endpoints. All services are interface-driven; DI registered in `Program.cs`.

### Azure Storage Tables (`src/schema/`)

| Table | Partition Key | Row Key | Key Columns |
|-------|--------------|---------|-------------|
| `Rooms` | `RoomCode` | `RoomCode` | `Status` (`Waiting`/`InProgress`/`Ended`), `CurrentRound`, `CreatedAt`, `MaxPlayers` |
| `Players` | `RoomCode` | `PlayerId` | `Nickname`, `IsSpy`, `IsEliminated`, `JoinedAt` |
| `GameState` | `RoomCode` | `RoundNumber` | `CivilianWord`, `SpyWord`, `CurrentTurnPlayerId`, `TurnEndsAt`, `Phase` |
| `Votes` | `{RoomCode}_R{RoundNumber}` | `VoterPlayerId` | `TargetPlayerId`, `CastAt` |

- `IsSpy` is **never returned raw** to the frontend — only the player's own assigned word.
- `Votes` partition key format: `ABC123_R2` (always this exact pattern).
- `Rooms.CurrentRound` is the single source of truth for the active round; look up `GameState` with `RowKey = CurrentRound.ToString()`.
- All timestamps: ISO 8601 UTC strings.

### Startup Behaviour

On startup: validate all tables exist with correct schema (defined in `src/schema/*.json`).
- Tables missing → auto-create from schema files.
- Schema wrong → all public APIs return failure responses.
- Azure Web App startup itself must **never** throw — schema issues surface as API errors.

### Infrastructure (`src/iac/`)

ARM template provisions: Storage Account (Standard LRS), Azure Web App, App Service Plan, System-Assigned Managed Identity with `Storage Table Data Contributor` role — no connection strings.

**Required env vars on the Web App:**
- `AdminPass` — global admin password, checked on every room-creation request.
- `StorageAccountName` — storage account name; code builds endpoint as `https://{name}.table.core.windows.net` and uses `DefaultAzureCredential`.

### Frontend: `src/frontend/`

- Vanilla JS (preferred) or jQuery; Bootstrap for layout/components; plain CSS for overrides.
- Each page/section is its own JS module exposing a single `init()` function with private internal state.
- Shared utilities (polling helper, API client, toast/notification) in `src/frontend/js/shared/`.
- CSS uses BEM naming for custom classes (`.player-card`, `.player-card--eliminated`).
- No inline JS or CSS in HTML files.

**UX style:** Control-panel density (RedHat Cockpit-inspired) — desktop-first (1280px primary target), all primary game actions visible without scrolling. Dialogs only for destructive confirmations and word reveal. Actions stay inline or in collapsible sections.

**Color palette** (CSS custom properties on `:root`):
```css
--color-primary:   #FF6B35;  /* orange — CTAs */
--color-secondary: #004E89;  /* deep blue — headers */
--color-success:   #10B981;  /* green — civilian safe */
--color-danger:    #EF4444;  /* red — elimination */
--color-warning:   #F59E0B;  /* amber — timer critical */
--color-bg:        #0F172A;  /* near-black — page bg */
--color-surface:   #1E293B;  /* dark card/panel bg */
--color-text:      #F1F5F9;
--color-muted:     #94A3B8;
```

## Coding Conventions

### C#
- PascalCase for classes/interfaces/methods; camelCase for parameters/locals.
- One class/interface/enum per file; all public members have XML doc comments.
- Async methods end with `Async`.
- Class member order: constants → private fields → private properties → public properties → constructors → public methods → protected methods → private methods.
- Explicit type declarations with target-typed `new()`: `Room room = new();`
- Collection init shorthand: `List<int> numbers = [];`
- Prefer expression-bodied members, LINQ over multi-line loops, modern C# (primary constructors, pattern matching, `switch` expressions, `??`, `?.`).
- Files stay under ~150 lines; no commented-out code; no filler comments (explain *why*, not *what*).
- Libraries: `Newtonsoft.Json`, `Microsoft.Extensions.Logging`, `xUnit` + `Moq` for tests.

### Accessibility (frontend)
- WCAG 2.1 AA: semantic HTML, `aria-live` for turn/timer updates, full keyboard navigation, ≥44×44px touch targets.
- Never use color as the only signal — pair with icon, label, or badge.
- Respect `prefers-reduced-motion`.

## Summary Files

The `summary/` folder contains compact AI-readable summaries:
- `summary/backend.md` — task folder responsibilities, table schemas, IAC details.
- `summary/frontend.md` — design guidelines, color palette, accessibility checklist.
- `summary/todo.md` — project overview and core requirements.

Keep these summaries up to date as the codebase grows.
