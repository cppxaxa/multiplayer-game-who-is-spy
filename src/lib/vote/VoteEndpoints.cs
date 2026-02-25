using Microsoft.AspNetCore.Mvc;
using WhoIsSpy.Lib.Shared;
using WhoIsSpy.Lib.Shared.Interfaces;

namespace WhoIsSpy.Lib.Vote;

/// <summary>Maps all vote-related API routes.</summary>
public static class VoteEndpoints
{
    /// <summary>Registers vote routes.</summary>
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/rooms/{code}/votes", CastVote);
    }

    private static async Task<IResult> CastVote(
        string code,
        HttpRequest req,
        [FromServices] IVoteService votes,
        [FromServices] IStateService state)
    {
        if (!AppState.SchemaValid) return Results.StatusCode(503);

        CastVoteRequest? body = await req.ReadFromJsonAsync<CastVoteRequest>();
        if (string.IsNullOrWhiteSpace(body?.VoterId) || string.IsNullOrWhiteSpace(body?.TargetId))
            return Results.BadRequest(new { error = "VoterId and TargetId are required." });

        var gameState = await state.GetStateAsync(code.ToUpperInvariant());
        if (gameState is null) return Results.NotFound();
        if (gameState.Phase != "Voting")
            return Results.BadRequest(new { error = "Not in Voting phase." });

        bool ok = await votes.CastVoteAsync(
            code.ToUpperInvariant(), gameState.Room.CurrentRound, body.VoterId, body.TargetId);

        return ok ? Results.Ok() : Results.BadRequest(new { error = "Invalid vote." });
    }

    private sealed record CastVoteRequest(string VoterId, string TargetId);
}
