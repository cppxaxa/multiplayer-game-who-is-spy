# Coding Standards

- Use **PascalCase** for class, interface, and method names.
- Use **camelCase** for method parameters and local variables.
- Each class, enum, or interface should be defined in a **separate file**.
- All interfaces and classes should have **XML documentation comments**.
- All public methods must include **XML documentation comments**.
- Async methods must end with `Async`, e.g., `GetUserDataAsync`.
- Class structure should follow this order:
  1. Constants
  2. Private fields
  3. Private properties
  4. Public properties
  5. Constructors
  6. Public methods
  7. Protected methods
  8. Private methods
- Prefer **explicit type declarations** with target-typed `new()` when the type is known, e.g. `Anomaly anomaly = new();`
- Use **collection initialization shorthand** when the type is known, e.g. `List<int> numbers = [];`
- Keep string length **under 100 characters**. Break longer strings across lines if needed.

# Brevity & Readability

The goal is code a developer can fully read and understand in one sitting. Fewer lines = faster onboarding and easier fixes.

- **Prefer expression-bodied members** for simple one-liners:
  ```csharp
  public string RoomCode => _entity.PartitionKey;
  public async Task<Room> GetAsync(string code) => await _table.GetAsync(code);
  ```
- **Use LINQ** to replace multi-line loops where the intent is clearer:
  ```csharp
  // preferred
  var active = players.Where(p => !p.IsEliminated).ToList();
  // avoid
  List<Player> active = [];
  foreach (var p in players) { if (!p.IsEliminated) active.Add(p); }
  ```
- **Avoid redundant code** — no wrapper methods that do nothing but call another method with the same signature, no re-stating the type in a comment when the name is already clear.
- **No defensive over-engineering** — do not add abstraction layers, base classes, or helper utilities unless they are actually reused in two or more places.
- **Favour C# modern syntax** to reduce boilerplate: primary constructors, pattern matching, `switch` expressions, `??`, `?.`, and `is` checks over verbose `if/else` chains.
- **Small, focused files:** keep files under ~150 lines. If a file grows beyond that, it is doing too much — split it.
- **No commented-out code** — delete dead code; version control is the history.
- **No filler comments** — only comment *why*, never *what* (the code itself must be clear enough to show what).

# Modularity & Maintainability

- **Single Responsibility:** every class and method does one thing only. If a method needs a comment to explain what it does, it should be split into smaller named methods instead.
- **Program to interfaces:** business logic always depends on an interface (e.g. `IRoomService`), never on a concrete class directly. This allows any module to be swapped or mocked without touching its callers.
- **No cross-folder imports:** task folders under `src/lib` only import from `src/lib/shared`. Never import from a sibling task folder — communicate through shared interfaces and models only.
- **No magic values:** all constants, strings, and numeric literals must be named constants defined in one place (in `src/lib/shared` if shared, or at the top of the file if local).
- **Fail loudly at startup, silently never at runtime:** validate configuration and schema at startup (see project-instructions.md). Never swallow exceptions silently — always log and rethrow or return a structured error.
- **Thin endpoints:** `*Endpoints.cs` files only wire up routes and call a service. No business logic inside endpoint handlers.

# Frontend Modularity

- Each page view or major UI section lives in its own JS file (e.g. `room.js`, `game.js`, `vote.js`).
- No inline JavaScript in HTML files — all JS is in `src/frontend/js/`.
- No inline styles in HTML — all overrides go in `src/frontend/css/`.
- JS modules expose a single init function (e.g. `RoomModule.init()`) and keep all internal state private.
- Shared frontend utilities (polling helper, API client, toast/notification helper) live in `src/frontend/js/shared/` and are imported by page modules.
- CSS follows BEM naming for custom classes (e.g. `.player-card`, `.player-card--eliminated`) to keep styles scoped and replaceable.

# Libraries and Tools

- Use `Newtonsoft.Json` for JSON serialization.
- Use `Microsoft.Extensions.Logging` for logging and diagnostics.
- Use `xUnit` and `Moq` for unit testing.
