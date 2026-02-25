using Microsoft.AspNetCore.Mvc;
using WhoIsSpy.Lib.Shared;
using WhoIsSpy.Lib.Shared.Interfaces;

namespace WhoIsSpy.Lib.State;

/// <summary>Maps all game-state snapshot API routes.</summary>
public static class StateEndpoints
{
    /// <summary>Registers state routes.</summary>
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/rooms/{code}/state", GetState);
        app.MapGet("/api/rooms/{code}/state/player/{playerId}", GetPlayerState);
        app.MapGet("/api/rooms/{code}/state/admin", GetAdminState);
    }

    private static async Task<IResult> GetState(
        string code,
        [FromServices] IStateService state)
    {
        if (!AppState.SchemaValid) return Results.StatusCode(503);

        var result = await state.GetStateAsync(code.ToUpperInvariant());
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetPlayerState(
        string code,
        string playerId,
        [FromServices] IStateService state)
    {
        if (!AppState.SchemaValid) return Results.StatusCode(503);

        var result = await state.GetPlayerStateAsync(code.ToUpperInvariant(), playerId);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetAdminState(
        string code,
        HttpRequest req,
        [FromServices] IStateService state,
        IConfiguration config)
    {
        if (!AppState.SchemaValid) return Results.StatusCode(503);

        string? provided = req.Headers[Constants.EnvAdminPass].FirstOrDefault();
        string? expected = config[Constants.EnvAdminPass];
        if (string.IsNullOrEmpty(expected) || provided != expected)
            return Results.Unauthorized();

        var result = await state.GetAdminStateAsync(code.ToUpperInvariant());
        return result is null ? Results.NotFound() : Results.Ok(result);
    }
}
