using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.FileProviders;
using WhoIsSpy.Lib.Shared;
using WhoIsSpy.Lib.Shared.Entities;
using WhoIsSpy.Lib.Shared.Interfaces;
using WhoIsSpy.Lib.Room;
using WhoIsSpy.Lib.Player;
using WhoIsSpy.Lib.Round;
using WhoIsSpy.Lib.Vote;
using WhoIsSpy.Lib.State;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ── Azure Table Storage ──────────────────────────────────────────────────────
builder.Services.AddSingleton(sp =>
{
    string? accountName = builder.Configuration[Constants.EnvStorageAccount];
    if (!string.IsNullOrEmpty(accountName))
    {
        Uri endpoint = new($"https://{accountName}.table.core.windows.net");
        return new TableServiceClient(endpoint, new DefaultAzureCredential());
    }
    string connStr = builder.Configuration["StorageConnectionString"]
                     ?? "UseDevelopmentStorage=true";
    return new TableServiceClient(connStr);
});

// ── Per-table clients ─────────────────────────────────────────────────────────
builder.Services.AddSingleton<ITableService<RoomEntity>>(sp =>
    new TableServiceBase<RoomEntity>(
        sp.GetRequiredService<TableServiceClient>().GetTableClient(Constants.TableRooms)));
builder.Services.AddSingleton<ITableService<PlayerEntity>>(sp =>
    new TableServiceBase<PlayerEntity>(
        sp.GetRequiredService<TableServiceClient>().GetTableClient(Constants.TablePlayers)));
builder.Services.AddSingleton<ITableService<GameStateEntity>>(sp =>
    new TableServiceBase<GameStateEntity>(
        sp.GetRequiredService<TableServiceClient>().GetTableClient(Constants.TableGameState)));
builder.Services.AddSingleton<ITableService<VoteEntity>>(sp =>
    new TableServiceBase<VoteEntity>(
        sp.GetRequiredService<TableServiceClient>().GetTableClient(Constants.TableVotes)));

// ── Domain services ──────────────────────────────────────────────────────────
builder.Services.AddSingleton<IRoomService, RoomService>();
builder.Services.AddSingleton<IPlayerService, PlayerService>();
builder.Services.AddSingleton<IRoundService, RoundService>();
builder.Services.AddSingleton<IVoteService, VoteService>();
builder.Services.AddSingleton<IStateService, StateService>();
builder.Services.AddSingleton<ISchemaValidator, SchemaValidator>();

WebApplication app = builder.Build();

// ── Schema validation at startup (must not throw) ────────────────────────────
try
{
    ISchemaValidator validator = app.Services.GetRequiredService<ISchemaValidator>();
    await validator.ValidateAndInitializeAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[STARTUP] Schema validation error: {ex.Message}");
    AppState.SchemaValid = false;
}

// ── Static files (frontend) ──────────────────────────────────────────────────
string frontendPath = Path.Combine(AppContext.BaseDirectory, "frontend");
PhysicalFileProvider fileProvider = new(frontendPath);
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider, RequestPath = "" });
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = fileProvider,
    RequestPath = "",
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    }
});

// ── API routes ───────────────────────────────────────────────────────────────
RoomEndpoints.Map(app);
PlayerEndpoints.Map(app);
RoundEndpoints.Map(app);
VoteEndpoints.Map(app);
StateEndpoints.Map(app);

app.Run();
