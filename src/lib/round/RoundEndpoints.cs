using Microsoft.AspNetCore.Mvc;
using WhoIsSpy.Lib.Shared;
using WhoIsSpy.Lib.Shared.Interfaces;

namespace WhoIsSpy.Lib.Round;

/// <summary>Maps all round lifecycle API routes.</summary>
public static class RoundEndpoints
{
    /// <summary>Registers round routes.</summary>
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/rooms/{code}/rounds/start", StartRound);
        app.MapPost("/api/rooms/{code}/rounds/advance", AdvanceRound);
        app.MapPost("/api/rooms/{code}/rounds/skip-to-voting", SkipToVoting);
        app.MapPost("/api/rooms/{code}/rounds/yield", YieldTurn);
        app.MapPost("/api/rooms/{code}/timer/pause", PauseTimer);
        app.MapPost("/api/rooms/{code}/timer/resume", ResumeTimer);
        app.MapPost("/api/rooms/{code}/timer/duration", SetDuration);
        app.MapPost("/api/rooms/{code}/reset", ResetGame);
    }

    private static async Task<IResult> StartRound(
        string code,
        HttpRequest req,
        [FromServices] IRoundService rounds,
        IConfiguration config)
    {
        if (!AppState.SchemaValid) return Results.StatusCode(503);
        if (!IsAdmin(req, config)) return Results.Unauthorized();

        bool ok = await rounds.StartRoundAsync(code.ToUpperInvariant());
        return ok ? Results.Ok() : Results.BadRequest(new { error = "Cannot start round." });
    }

    private static async Task<IResult> AdvanceRound(
        string code,
        HttpRequest req,
        [FromServices] IRoundService rounds,
        IConfiguration config)
    {
        if (!AppState.SchemaValid) return Results.StatusCode(503);
        if (!IsAdmin(req, config)) return Results.Unauthorized();

        bool ok = await rounds.AdvanceAsync(code.ToUpperInvariant());
        return ok ? Results.Ok() : Results.BadRequest(new { error = "Cannot advance round." });
    }

    private static async Task<IResult> SkipToVoting(
        string code,
        HttpRequest req,
        [FromServices] IRoundService rounds,
        IConfiguration config)
    {
        if (!AppState.SchemaValid) return Results.StatusCode(503);
        if (!IsAdmin(req, config)) return Results.Unauthorized();

        bool ok = await rounds.SkipToVotingAsync(code.ToUpperInvariant());
        return ok ? Results.Ok() : Results.BadRequest(new { error = "Cannot skip to voting — phase must be Discussion." });
    }

    private static async Task<IResult> YieldTurn(
        string code,
        HttpRequest req,
        [FromServices] IRoundService rounds)
    {
        if (!AppState.SchemaValid) return Results.StatusCode(503);
        YieldTurnRequest? body = await req.ReadFromJsonAsync<YieldTurnRequest>();
        if (string.IsNullOrWhiteSpace(body?.PlayerId))
            return Results.BadRequest(new { error = "PlayerId required." });
        bool ok = await rounds.YieldTurnAsync(code.ToUpperInvariant(), body.PlayerId);
        return ok ? Results.Ok() : Results.BadRequest(new { error = "Cannot yield turn." });
    }

    private static async Task<IResult> PauseTimer(
        string code,
        HttpRequest req,
        [FromServices] IRoundService rounds,
        IConfiguration config)
    {
        if (!AppState.SchemaValid) return Results.StatusCode(503);
        if (!IsAdmin(req, config)) return Results.Unauthorized();

        bool ok = await rounds.PauseTimerAsync(code.ToUpperInvariant());
        return ok ? Results.Ok() : Results.BadRequest(new { error = "Cannot pause timer." });
    }

    private static async Task<IResult> ResumeTimer(
        string code,
        HttpRequest req,
        [FromServices] IRoundService rounds,
        IConfiguration config)
    {
        if (!AppState.SchemaValid) return Results.StatusCode(503);
        if (!IsAdmin(req, config)) return Results.Unauthorized();

        bool ok = await rounds.ResumeTimerAsync(code.ToUpperInvariant());
        return ok ? Results.Ok() : Results.BadRequest(new { error = "Cannot resume timer." });
    }

    private static async Task<IResult> SetDuration(
        string code,
        HttpRequest req,
        [FromServices] IRoundService rounds,
        IConfiguration config)
    {
        if (!AppState.SchemaValid) return Results.StatusCode(503);
        if (!IsAdmin(req, config)) return Results.Unauthorized();

        SetDurationRequest? body = await req.ReadFromJsonAsync<SetDurationRequest>();
        if (body is null) return Results.BadRequest(new { error = "Body required." });

        bool ok = await rounds.SetTurnDurationAsync(
            code.ToUpperInvariant(),
            body.TurnDurationSeconds,
            body.VotingDurationSeconds,
            body.ApplyNow);

        return ok ? Results.Ok() : Results.BadRequest(new { error = "Cannot update duration." });
    }

    private static async Task<IResult> ResetGame(
        string code,
        HttpRequest req,
        [FromServices] IRoundService rounds,
        IConfiguration config)
    {
        if (!AppState.SchemaValid) return Results.StatusCode(503);
        if (!IsAdmin(req, config)) return Results.Unauthorized();
        bool ok = await rounds.ResetGameAsync(code.ToUpperInvariant());
        return ok ? Results.Ok() : Results.BadRequest(new { error = "Cannot reset game." });
    }

    private static bool IsAdmin(HttpRequest req, IConfiguration config)
    {
        string? provided = req.Headers[Constants.EnvAdminPass].FirstOrDefault();
        string? expected = config[Constants.EnvAdminPass];
        return !string.IsNullOrEmpty(expected) && provided == expected;
    }

    private sealed record SetDurationRequest(int TurnDurationSeconds, int VotingDurationSeconds, bool ApplyNow);
    private sealed record YieldTurnRequest(string PlayerId);
}
