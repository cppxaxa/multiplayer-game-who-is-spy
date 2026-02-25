# Tasks available in src/lib

Each sub-folder under `src/lib/{task}` owns all code related to one logical task/domain.

| Folder | Responsibility |
|--------|---------------|
| `src/lib/room` | Room creation (`POST /api/rooms`), get room (`GET /api/rooms/{code}`). AdminPass header required for creation. |
| `src/lib/player` | Join room (`POST /api/rooms/{code}/players`), list players, leave room, admin-kick (`POST .../players/{id}/kick` — **deletes** the player row entirely, identical to leaving). |
| `src/lib/round` | Full round lifecycle: start round (word assignment, spy pick, Discussion phase), advance (next turn → Voting → Ended, tally votes, eliminate). `YieldTurnAsync` / `POST .../rounds/yield` allows the current speaker to self-advance their turn (unauthenticated). AdminPass required for start/advance. |
| `src/lib/vote` | Submit/update vote (`POST /api/rooms/{code}/votes`). One vote per player per round (upsert). |
| `src/lib/state` | Game state snapshots: full state (`GET /api/rooms/{code}/state`) and player state with word (`GET /api/rooms/{code}/state/player/{id}`). |
| `src/lib/shared` | Entities (RoomEntity, PlayerEntity, GameStateEntity, VoteEntity), Models (DTOs), Interfaces, Constants (`MinPlayers = 3`, word pairs, etc.), TableServiceBase, SchemaValidator, AppState. |

**Conventions:**
- Minimal API pattern (no MVC controllers) — each task folder exposes one `*Endpoints.cs` that maps routes.
- All service classes follow PascalCase; async methods end with `Async`.
- Dependency injection registered in `Program.cs`.
- Task folders **only import from `src/lib/shared`** — never directly from each other. This prevents circular dependencies and keeps each folder independently testable.

# Schema in src/schema folder

Azure Storage Table schemas used by the application. Tables are auto-created on startup if missing; a schema mismatch returns a failure from all public APIs.

| Table | Partition Key | Row Key | Key Columns |
|-------|--------------|---------|-------------|
| `Rooms` | `RoomCode` | `RoomCode` | `Status` (`Waiting`/`InProgress`/`Ended`), `CurrentRound` (int, 0 when not started), `CreatedAt`, `MaxPlayers` |
| `Players` | `RoomCode` | `PlayerId` | `Nickname`, `IsSpy` (bool), `IsEliminated` (bool), `JoinedAt` |
| `GameState` | `RoomCode` | `RoundNumber` | `CivilianWord`, `SpyWord`, `CurrentTurnPlayerId`, `TurnEndsAt`, `Phase` (`Discussion`/`Voting`/`Ended`) |
| `Votes` | `RoomCode_R{RoundNumber}` | `VoterPlayerId` | `TargetPlayerId`, `CastAt` |

**Notes:**
- All timestamps stored as ISO 8601 UTC strings.
- `IsSpy` is only readable server-side; it is never returned raw in API responses — the frontend receives only its own assigned word.
- `Votes` partition key format: `{RoomCode}_R{RoundNumber}` e.g. `ABC123_R2` — always use this exact pattern when reading/writing votes.
- `Rooms.CurrentRound` is the single source of truth for the active round number; look up `GameState` using `PartitionKey = RoomCode` and `RowKey = CurrentRound.ToString()`.
- Schema definitions live as JSON files in `src/schema/` and are read at startup for validation.

# IAC in src/iac

ARM template (`azuredeploy.json`) and parameter file (`azuredeploy.parameters.json`) that provision all required Azure resources.

**Resources defined:**

| Resource | Type | Notes |
|----------|------|-------|
| Storage Account | `Microsoft.Storage/storageAccounts` | Standard LRS, used for Table Storage |
| App Service Plan | `Microsoft.Web/serverfarms` | Hosts the web app |
| Web App | `Microsoft.Web/sites` | .NET 10, serves frontend and APIs |
| System-Assigned Managed Identity | (on Web App) | Grants `Storage Table Data Contributor` role to the storage account — no connection strings needed |
| Role Assignment | `Microsoft.Authorization/roleAssignments` | Scoped to the storage account |

**Environment variables expected on the Web App:**

| Variable | Purpose |
|----------|---------|
| `AdminPass` | Single global admin password; checked server-side on every room-creation request — never stored in the table |
| `StorageAccountName` | Azure Storage account name (e.g. `mystorageaccount`). Code constructs the Table endpoint as `https://{StorageAccountName}.table.core.windows.net` and connects via `DefaultAzureCredential`, which automatically uses the Web App's System-Assigned Managed Identity on Azure and local developer credentials during development |

**Deployment:**
```bash
az deployment group create \
  --resource-group <rg-name> \
  --template-file src/iac/azuredeploy.json \
  --parameters @src/iac/azuredeploy.parameters.json
```
