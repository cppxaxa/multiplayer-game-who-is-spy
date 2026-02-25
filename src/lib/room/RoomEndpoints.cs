using Microsoft.AspNetCore.Mvc;
using WhoIsSpy.Lib.Shared;
using WhoIsSpy.Lib.Shared.Interfaces;

namespace WhoIsSpy.Lib.Room;

/// <summary>Maps all room-related API routes.</summary>
public static class RoomEndpoints
{
    /// <summary>Registers room routes.</summary>
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/rooms", CreateRoom);
        app.MapGet("/api/rooms/{code}", GetRoom);
    }

    private static async Task<IResult> CreateRoom(
        HttpRequest req,
        [FromServices] IRoomService rooms,
        IConfiguration config)
    {
        if (!AppState.SchemaValid) return Results.StatusCode(503);

        string? providedPass = req.Headers["AdminPass"].FirstOrDefault();
        string? expectedPass = config[Constants.EnvAdminPass];
        if (string.IsNullOrEmpty(expectedPass) || providedPass != expectedPass)
            return Results.Unauthorized();

        CreateRoomRequest? body = await req.ReadFromJsonAsync<CreateRoomRequest>();
        int maxPlayers = body?.MaxPlayers > 0 ? body.MaxPlayers : Constants.MaxPlayers;
        int turnSecs = body?.TurnDurationSeconds > 0 ? body.TurnDurationSeconds : Constants.TurnSeconds;
        int votingSecs = body?.VotingDurationSeconds > 0 ? body.VotingDurationSeconds : Constants.VotingSeconds;

        var room = await rooms.CreateRoomAsync(maxPlayers, turnSecs, votingSecs);
        return Results.Ok(room);
    }

    private static async Task<IResult> GetRoom(
        string code,
        [FromServices] IRoomService rooms)
    {
        if (!AppState.SchemaValid) return Results.StatusCode(503);

        var room = await rooms.GetRoomAsync(code.ToUpperInvariant());
        return room is null ? Results.NotFound() : Results.Ok(room);
    }

    private sealed record CreateRoomRequest(int MaxPlayers, int TurnDurationSeconds, int VotingDurationSeconds);
}
