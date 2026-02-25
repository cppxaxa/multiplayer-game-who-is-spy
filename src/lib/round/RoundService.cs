using WhoIsSpy.Lib.Shared;
using WhoIsSpy.Lib.Shared.Entities;
using WhoIsSpy.Lib.Shared.Interfaces;

namespace WhoIsSpy.Lib.Round;

/// <summary>Manages the full round lifecycle: start, discussion turns, voting, and elimination.</summary>
public class RoundService(
    ITableService<RoomEntity> rooms,
    ITableService<PlayerEntity> players,
    ITableService<GameStateEntity> gameStates,
    ITableService<VoteEntity> votes) : IRoundService
{
    private readonly ITableService<RoomEntity> _rooms = rooms;
    private readonly ITableService<PlayerEntity> _players = players;
    private readonly ITableService<GameStateEntity> _gameStates = gameStates;
    private readonly ITableService<VoteEntity> _votes = votes;

    /// <inheritdoc/>
    public async Task<bool> StartRoundAsync(string roomCode)
    {
        RoomEntity? room = await _rooms.GetAsync(roomCode, roomCode);
        if (room is null) return false;

        // Disallow starting a new round when the game is fully over (spy/civ win condition met)
        if (room.Status == "Ended") return false;

        // If a round is in progress, only allow starting a new one once that round has ended
        if (room.CurrentRound > 0)
        {
            GameStateEntity? current = await _gameStates.GetAsync(roomCode, room.CurrentRound.ToString());
            if (current is not null && current.Phase != "Ended") return false;
        }

        List<PlayerEntity> activePlayers = await GetActivePlayersAsync(roomCode);
        if (activePlayers.Count < Constants.MinPlayers) return false;

        // Pick word pair and spy
        (string civilian, string spy) = Constants.WordPairs[
            Random.Shared.Next(Constants.WordPairs.Count)];
        PlayerEntity spyPlayer = activePlayers[Random.Shared.Next(activePlayers.Count)];

        // Reset spy flags
        foreach (PlayerEntity p in activePlayers)
        {
            p.IsSpy = p.RowKey == spyPlayer.RowKey;
            await _players.UpsertAsync(p);
        }

        int round = room.CurrentRound + 1;
        room.CurrentRound = round;
        room.Status = "InProgress";
        await _rooms.UpsertAsync(room);

        int turnSecs = room.TurnDurationSeconds > 0 ? room.TurnDurationSeconds : Constants.TurnSeconds;

        GameStateEntity state = new()
        {
            PartitionKey = roomCode,
            RowKey = round.ToString(),
            CivilianWord = civilian,
            SpyWord = spy,
            CurrentTurnPlayerId = activePlayers[0].RowKey,
            TurnEndsAt = DateTime.UtcNow.AddSeconds(turnSecs).ToString("o"),
            Phase = "Discussion",
            IsPaused = false,
            Winner = string.Empty,
            SpyPlayerId = string.Empty
        };
        await _gameStates.UpsertAsync(state);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> SkipToVotingAsync(string roomCode)
    {
        RoomEntity? room = await _rooms.GetAsync(roomCode, roomCode);
        if (room is null || room.CurrentRound == 0) return false;

        GameStateEntity? state = await _gameStates.GetAsync(roomCode, room.CurrentRound.ToString());
        if (state is null || state.Phase != "Discussion") return false;

        int votingSecs = room.VotingDurationSeconds > 0 ? room.VotingDurationSeconds : Constants.VotingSeconds;

        state.Phase = "Voting";
        state.CurrentTurnPlayerId = string.Empty;
        state.TurnEndsAt = DateTime.UtcNow.AddSeconds(votingSecs).ToString("o");
        // Clear any lingering pause so the voting timer starts running immediately
        state.IsPaused = false;
        state.PausedAt = null;
        state.PausedSecondsRemaining = 0;

        await _gameStates.UpsertAsync(state);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> AdvanceAsync(string roomCode)
    {
        RoomEntity? room = await _rooms.GetAsync(roomCode, roomCode);
        if (room is null || room.CurrentRound == 0) return false;

        GameStateEntity? state = await _gameStates.GetAsync(
            roomCode, room.CurrentRound.ToString());
        if (state is null) return false;

        // Clear any pause when advancing
        state.IsPaused = false;
        state.PausedAt = null;
        state.PausedSecondsRemaining = 0;

        return state.Phase switch
        {
            "Discussion" => await AdvanceDiscussionAsync(roomCode, state, room),
            "Voting" => await AdvanceVotingAsync(roomCode, state, room),
            _ => false
        };
    }

    /// <inheritdoc/>
    public async Task<bool> PauseTimerAsync(string roomCode)
    {
        RoomEntity? room = await _rooms.GetAsync(roomCode, roomCode);
        if (room is null || room.CurrentRound == 0) return false;

        GameStateEntity? state = await _gameStates.GetAsync(roomCode, room.CurrentRound.ToString());
        if (state is null || state.IsPaused || state.Phase == "Ended") return false;

        double remaining = (DateTime.Parse(state.TurnEndsAt, null,
            System.Globalization.DateTimeStyles.RoundtripKind) - DateTime.UtcNow).TotalSeconds;

        state.IsPaused = true;
        state.PausedAt = DateTime.UtcNow.ToString("o");
        state.PausedSecondsRemaining = Math.Max(0, remaining);
        await _gameStates.UpsertAsync(state);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> ResumeTimerAsync(string roomCode)
    {
        RoomEntity? room = await _rooms.GetAsync(roomCode, roomCode);
        if (room is null || room.CurrentRound == 0) return false;

        GameStateEntity? state = await _gameStates.GetAsync(roomCode, room.CurrentRound.ToString());
        if (state is null || !state.IsPaused) return false;

        state.TurnEndsAt = DateTime.UtcNow
            .AddSeconds(state.PausedSecondsRemaining).ToString("o");
        state.IsPaused = false;
        state.PausedAt = null;
        state.PausedSecondsRemaining = 0;
        await _gameStates.UpsertAsync(state);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> SetTurnDurationAsync(
        string roomCode, int turnSeconds, int votingSeconds, bool applyNow)
    {
        RoomEntity? room = await _rooms.GetAsync(roomCode, roomCode);
        if (room is null) return false;

        room.TurnDurationSeconds = turnSeconds > 0 ? turnSeconds : Constants.TurnSeconds;
        room.VotingDurationSeconds = votingSeconds > 0 ? votingSeconds : Constants.VotingSeconds;
        await _rooms.UpsertAsync(room);

        if (applyNow && room.CurrentRound > 0)
        {
            GameStateEntity? state = await _gameStates.GetAsync(roomCode, room.CurrentRound.ToString());
            if (state is not null && state.Phase is "Discussion" or "Voting")
            {
                int secs = state.Phase == "Voting"
                    ? room.VotingDurationSeconds
                    : room.TurnDurationSeconds;
                state.TurnEndsAt = DateTime.UtcNow.AddSeconds(secs).ToString("o");
                state.IsPaused = false;
                state.PausedAt = null;
                state.PausedSecondsRemaining = 0;
                await _gameStates.UpsertAsync(state);
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public async Task TryAutoAdvanceVotingAsync(string roomCode)
    {
        RoomEntity? room = await _rooms.GetAsync(roomCode, roomCode);
        if (room is null || room.CurrentRound == 0) return;

        GameStateEntity? state = await _gameStates.GetAsync(roomCode, room.CurrentRound.ToString());
        if (state is null || state.Phase != "Voting" || state.IsPaused) return;

        List<PlayerEntity> active = await GetActivePlayersAsync(roomCode);
        string votePk = Constants.VotePartitionKey(roomCode, room.CurrentRound);
        List<VoteEntity> roundVotes = await _votes.QueryAsync($"PartitionKey eq '{votePk}'");

        bool timerExpired = DateTime.Parse(state.TurnEndsAt, null,
            System.Globalization.DateTimeStyles.RoundtripKind) <= DateTime.UtcNow;
        bool allVoted = active.Count > 0 && roundVotes.Count >= active.Count;

        if (timerExpired || allVoted)
            await AdvanceAsync(roomCode);
    }

    /// <inheritdoc/>
    public async Task<bool> YieldTurnAsync(string roomCode, string playerId)
    {
        RoomEntity? room = await _rooms.GetAsync(roomCode, roomCode);
        if (room is null || room.Status != "InProgress" || room.CurrentRound == 0) return false;

        GameStateEntity? state = await _gameStates.GetAsync(roomCode, room.CurrentRound.ToString());
        if (state is null || state.Phase != "Discussion") return false;
        if (state.CurrentTurnPlayerId != playerId) return false;

        return await AdvanceAsync(roomCode);
    }

    /// <inheritdoc/>
    public async Task<bool> ResetGameAsync(string roomCode)
    {
        RoomEntity? room = await _rooms.GetAsync(roomCode, roomCode);
        if (room is null) return false;

        int previousRounds = room.CurrentRound;

        // 1. Reset room to Waiting
        room.Status = "Waiting";
        room.CurrentRound = 0;
        await _rooms.UpsertAsync(room);

        // 2. Reset all players (un-eliminate, un-spy)
        List<PlayerEntity> players = await _players.QueryAsync($"PartitionKey eq '{roomCode}'");
        foreach (PlayerEntity p in players)
        {
            p.IsSpy = false;
            p.IsEliminated = false;
            await _players.UpsertAsync(p);
        }

        // 3. Delete all GameState rows for this room
        List<GameStateEntity> gameStates = await _gameStates.QueryAsync($"PartitionKey eq '{roomCode}'");
        foreach (GameStateEntity gs in gameStates)
            await _gameStates.DeleteAsync(roomCode, gs.RowKey);

        // 4. Delete all Vote rows across all past rounds
        for (int r = 1; r <= previousRounds; r++)
        {
            string votePk = Constants.VotePartitionKey(roomCode, r);
            List<VoteEntity> roundVotes = await _votes.QueryAsync($"PartitionKey eq '{votePk}'");
            foreach (VoteEntity v in roundVotes)
                await _votes.DeleteAsync(votePk, v.RowKey);
        }

        return true;
    }

    private async Task<bool> AdvanceDiscussionAsync(
        string roomCode, GameStateEntity state, RoomEntity room)
    {
        List<PlayerEntity> active = await GetActivePlayersAsync(roomCode);
        int currentIdx = active.FindIndex(p => p.RowKey == state.CurrentTurnPlayerId);
        int nextIdx = currentIdx + 1;

        int turnSecs = room.TurnDurationSeconds > 0 ? room.TurnDurationSeconds : Constants.TurnSeconds;
        int votingSecs = room.VotingDurationSeconds > 0 ? room.VotingDurationSeconds : Constants.VotingSeconds;

        if (nextIdx >= active.Count)
        {
            // All players had their turn — move to voting
            state.Phase = "Voting";
            state.TurnEndsAt = DateTime.UtcNow.AddSeconds(votingSecs).ToString("o");
            state.CurrentTurnPlayerId = string.Empty;
        }
        else
        {
            state.CurrentTurnPlayerId = active[nextIdx].RowKey;
            state.TurnEndsAt = DateTime.UtcNow.AddSeconds(turnSecs).ToString("o");
        }

        await _gameStates.UpsertAsync(state);
        return true;
    }

    private async Task<bool> AdvanceVotingAsync(
        string roomCode, GameStateEntity state, RoomEntity room)
    {
        string votePk = Constants.VotePartitionKey(roomCode, room.CurrentRound);
        List<VoteEntity> roundVotes = await _votes.QueryAsync(
            $"PartitionKey eq '{votePk}'");

        string? eliminated = TallyVotes(roundVotes);
        if (eliminated is not null)
        {
            PlayerEntity? target = await _players.GetAsync(roomCode, eliminated);
            if (target is not null)
            {
                target.IsEliminated = true;
                await _players.UpsertAsync(target);
            }
        }

        // Compute remaining active players after this elimination
        List<PlayerEntity> remaining = await GetActivePlayersAsync(roomCode);
        bool spyAlive = remaining.Any(p => p.IsSpy);
        bool spyWins  = spyAlive && remaining.Count(p => !p.IsSpy) <= 1;
        bool civWins  = !spyAlive;

        if (spyWins || civWins)
        {
            room.Status = "Ended";
            state.Winner = spyWins ? "spy" : "civilians";

            // Identify the spy for the reveal (query all players, not just active ones)
            List<PlayerEntity> allPlayers = await _players.QueryAsync(
                $"PartitionKey eq '{roomCode}'");
            PlayerEntity? spyPlayer = allPlayers.FirstOrDefault(p => p.IsSpy);
            state.SpyPlayerId = spyPlayer?.RowKey ?? string.Empty;
        }

        state.Phase = "Ended";
        await _gameStates.UpsertAsync(state);
        await _rooms.UpsertAsync(room);
        return true;
    }

    private static string? TallyVotes(List<VoteEntity> roundVotes)
    {
        if (roundVotes.Count == 0) return null;

        Dictionary<string, int> tally = roundVotes
            .GroupBy(v => v.TargetPlayerId)
            .ToDictionary(g => g.Key, g => g.Count());

        int max = tally.Values.Max();
        List<string> topVoted = tally.Where(kv => kv.Value == max).Select(kv => kv.Key).ToList();

        // Tie — no elimination
        return topVoted.Count == 1 ? topVoted[0] : null;
    }

    private async Task<List<PlayerEntity>> GetActivePlayersAsync(string roomCode) =>
        (await _players.QueryAsync($"PartitionKey eq '{roomCode}'"))
            .Where(p => !p.IsEliminated)
            .OrderBy(p => p.JoinedAt)
            .ToList();
}
