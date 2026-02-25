using Microsoft.AspNetCore.Mvc;
using WhoIsSpy.Lib.Shared;
using WhoIsSpy.Lib.Shared.Interfaces;

namespace WhoIsSpy.Lib.Player;

/// <summary>Maps all player-related API routes.</summary>
public static class PlayerEndpoints
{
    /// <summary>Registers player routes.</summary>
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/rooms/{code}/players", JoinRoom);
        app.MapGet("/api/rooms/{code}/players", ListPlayers);
        app.MapDelete("/api/rooms/{code}/players/{playerId}", LeaveRoom);
        app.MapPost("/api/rooms/{code}/players/{playerId}/kick", KickPlayer);
    }

    private static async Task<IResult> JoinRoom(
        string code,
        HttpRequest req,
        [FromServices] IPlayerService players)
    {
        if (!AppState.SchemaValid) return Results.StatusCode(503);

        JoinRoomRequest? body = await req.ReadFromJsonAsync<JoinRoomRequest>();
        if (string.IsNullOrWhiteSpace(body?.Nickname))
            return Results.BadRequest(new { error = "Nickname is required." });

        string? playerId = await players.JoinRoomAsync(code.ToUpperInvariant(), body.Nickname);
        if (playerId is null)
            return Results.BadRequest(new { error = "Room not found, not accepting players, or is full." });

        return Results.Ok(new { playerId });
    }

    private static async Task<IResult> ListPlayers(
        string code,
        [FromServices] IPlayerService players)
    {
        if (!AppState.SchemaValid) return Results.StatusCode(503);

        var result = await players.GetPlayersAsync(code.ToUpperInvariant());
        return Results.Ok(result);
    }

    private static async Task<IResult> LeaveRoom(
        string code,
        string playerId,
        [FromServices] IPlayerService players)
    {
        if (!AppState.SchemaValid) return Results.StatusCode(503);

        await players.LeaveRoomAsync(code.ToUpperInvariant(), playerId);
        return Results.NoContent();
    }

    private static async Task<IResult> KickPlayer(
        string code,
        string playerId,
        HttpRequest req,
        [FromServices] IPlayerService players,
        IConfiguration config)
    {
        if (!AppState.SchemaValid) return Results.StatusCode(503);

        string? provided = req.Headers[Constants.EnvAdminPass].FirstOrDefault();
        string? expected = config[Constants.EnvAdminPass];
        if (string.IsNullOrEmpty(expected) || provided != expected)
            return Results.Unauthorized();

        bool ok = await players.KickPlayerAsync(code.ToUpperInvariant(), playerId);
        return ok ? Results.Ok() : Results.NotFound();
    }

    private sealed record JoinRoomRequest(string Nickname);
}
