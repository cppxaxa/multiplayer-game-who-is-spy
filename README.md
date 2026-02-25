# 🕵️ Who Is the Spy?

A multiplayer party game for 12–30 players. Civilians share a secret word; the spy gets a similar one. Players discuss, vote, and try to eliminate the spy before they're outnumbered.

No accounts required — nicknames only.

---

## Deploy to Azure

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FYOUR_ORG%2FYOUR_REPO%2Fmain%2Fsrc%2Fiac%2Fazuredeploy.json)

> **Before clicking:** replace `YOUR_ORG` and `YOUR_REPO` in the URL above with your GitHub organisation/username and repository name. The button links directly to the Azure Portal custom-deployment page with the ARM template pre-loaded.

The template provisions:

| Resource | Notes |
|---|---|
| Storage Account | Standard LRS — holds all game state in Table Storage |
| App Service Plan | Linux, configurable SKU (default B1) |
| Web App | .NET 10, serves the frontend and all APIs |
| System-Assigned Managed Identity | Grants `Storage Table Data Contributor` on the storage account — no connection strings |

**Required parameters when deploying:**

| Parameter | Description |
|---|---|
| `storageAccountName` | Globally unique storage account name |
| `webAppName` | Globally unique Web App name |
| `adminPass` | Secret password used to authenticate admin actions |
| `appServicePlanSku` | App Service Plan SKU — `B1` is the default |

---

## Stack

- **Backend:** .NET 10 Minimal API (Azure Web App)
- **Frontend:** Vanilla JS / jQuery + Bootstrap 5 — no build step
- **Database:** Azure Storage Tables
- **Auth:** Admin password via `AdminPass` request header; no player accounts

---

## Local Development

**Prerequisites:** .NET 10 SDK, Azure Functions Core Tools, Azure Storage Emulator (Azurite) or a real storage account.

```bash
# Build
dotnet build

# Run
func start

# Run all tests
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

Set these environment variables (or use `local.settings.json`):

```json
{
  "Values": {
    "AdminPass": "your-local-password",
    "StorageAccountName": "devstoreaccount1"
  }
}
```

---

## Project Structure

```
src/
├── frontend/          # Static files served by the Web App (no build step)
│   ├── js/
│   │   ├── shared/    # api.js, polling.js, toast.js
│   │   ├── admin.js   # Admin panel
│   │   ├── game.js    # Player game view
│   │   └── vote.js    # Voting UI
│   ├── css/
│   ├── admin.html
│   └── game.html
├── lib/
│   ├── room/          # Room creation & lookup
│   ├── player/        # Join, leave, kick
│   ├── round/         # Full round lifecycle (Discussion → Voting → Ended)
│   ├── vote/          # Vote casting & tallying
│   ├── state/         # Game-state snapshot endpoints (polled by frontend)
│   └── shared/        # Entities, DTOs, interfaces, constants
├── iac/
│   ├── azuredeploy.json
│   └── azuredeploy.parameters.json
└── schema/            # Azure Table schema definitions (auto-created on startup)
```

---

## How to Play

1. **Admin** opens `admin.html`, enters the admin password, and creates a room.
2. **Players** open the shareable link, enter a nickname, and join.
3. Admin clicks **Start Round** — each player receives their secret word.
4. Players take turns describing their word without saying it (**Discussion** phase).
5. When discussion ends, everyone votes to eliminate the suspected spy (**Voting** phase).
6. Most-voted player is eliminated (ties = no elimination). Win conditions are checked.
7. Repeat until the spy is caught (civilians win) or the spy outlasts the civilians (spy wins).
