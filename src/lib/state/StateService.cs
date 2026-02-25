using WhoIsSpy.Lib.Shared;
using WhoIsSpy.Lib.Shared.Entities;
using WhoIsSpy.Lib.Shared.Interfaces;
using WhoIsSpy.Lib.Shared.Models;

namespace WhoIsSpy.Lib.State;

/// <summary>Builds game state snapshots for frontend polling.</summary>
public class StateService(
    ITableService<RoomEntity> rooms,
    ITableService<PlayerEntity> players,
    ITableService<GameStateEntity> gameStates,
    ITableService<VoteEntity> votes,
    IRoundService rounds) : IStateService
{
    private readonly ITableService<RoomEntity> _rooms = rooms;
    private readonly ITableService<PlayerEntity> _players = players;
    private readonly ITableService<GameStateEntity> _gameStates = gameStates;
    private readonly ITableService<VoteEntity> _votes = votes;
    private readonly IRoundService _rounds = rounds;

    /// <inheritdoc/>
    public async Task<GameStateDto?> GetStateAsync(string roomCode)
    {
        await _rounds.TryAutoAdvanceVotingAsync(roomCode);

        RoomEntity? room = await _rooms.GetAsync(roomCode, roomCode);
        if (room is null) return null;

        List<PlayerEntity> playerEntities = await _players.QueryAsync(
            $"PartitionKey eq '{roomCode}'");
        GameStateEntity? gs = room.CurrentRound > 0
            ? await _gameStates.GetAsync(roomCode, room.CurrentRound.ToString())
            : null;

        return new GameStateDto
        {
            Room = MapRoom(room),
            Players = playerEntities.Select(MapPlayer).ToList(),
            CurrentTurnPlayerId = gs?.CurrentTurnPlayerId,
            TurnEndsAt = gs?.TurnEndsAt,
            Phase = gs?.Phase,
            IsPaused = gs?.IsPaused ?? false,
            PausedSecondsRemaining = gs?.PausedSecondsRemaining ?? 0,
            Winner = gs?.Winner,
            SpyPlayerId = gs?.SpyPlayerId,
            SpyNickname = string.IsNullOrEmpty(gs?.SpyPlayerId) ? null
                : playerEntities.FirstOrDefault(p => p.RowKey == gs.SpyPlayerId)?.Nickname
        };
    }

    /// <inheritdoc/>
    public async Task<PlayerStateDto?> GetPlayerStateAsync(string roomCode, string playerId)
    {
        await _rounds.TryAutoAdvanceVotingAsync(roomCode);

        RoomEntity? room = await _rooms.GetAsync(roomCode, roomCode);
        if (room is null) return null;

        PlayerEntity? me = await _players.GetAsync(roomCode, playerId);
        if (me is null) return null;

        List<PlayerEntity> allPlayers = await _players.QueryAsync(
            $"PartitionKey eq '{roomCode}'");
        GameStateEntity? gs = room.CurrentRound > 0
            ? await _gameStates.GetAsync(roomCode, room.CurrentRound.ToString())
            : null;

        // Give the player their own word; never expose IsSpy of others
        string? myWord = gs is null ? null
            : me.IsSpy ? gs.SpyWord : gs.CivilianWord;

        return new PlayerStateDto
        {
            Room = MapRoom(room),
            Players = allPlayers.Select(MapPlayer).ToList(),
            Me = MapPlayer(me),
            MyWord = myWord,
            CurrentTurnPlayerId = gs?.CurrentTurnPlayerId,
            TurnEndsAt = gs?.TurnEndsAt,
            Phase = gs?.Phase,
            IsPaused = gs?.IsPaused ?? false,
            PausedSecondsRemaining = gs?.PausedSecondsRemaining ?? 0,
            Winner = gs?.Winner,
            SpyPlayerId = gs?.SpyPlayerId,
            SpyNickname = string.IsNullOrEmpty(gs?.SpyPlayerId) ? null
                : allPlayers.FirstOrDefault(p => p.RowKey == gs.SpyPlayerId)?.Nickname
        };
    }

    /// <inheritdoc/>
    public async Task<GameStateDto?> GetAdminStateAsync(string roomCode)
    {
        await _rounds.TryAutoAdvanceVotingAsync(roomCode);

        RoomEntity? room = await _rooms.GetAsync(roomCode, roomCode);
        if (room is null) return null;

        List<PlayerEntity> playerEntities = await _players.QueryAsync(
            $"PartitionKey eq '{roomCode}'");
        GameStateEntity? gs = room.CurrentRound > 0
            ? await _gameStates.GetAsync(roomCode, room.CurrentRound.ToString())
            : null;

        // Load votes for the admin during Voting phase so they can see who voted for whom
        List<VoteDto> voteDtos = [];
        if (gs?.Phase == "Voting" && room.CurrentRound > 0)
        {
            string votePk = Constants.VotePartitionKey(roomCode, room.CurrentRound);
            List<VoteEntity> voteEntities = await _votes.QueryAsync($"PartitionKey eq '{votePk}'");
            voteDtos = voteEntities.Select(v => new VoteDto
            {
                VoterPlayerId  = v.RowKey,
                VoterNickname  = playerEntities.FirstOrDefault(p => p.RowKey == v.RowKey)?.Nickname ?? v.RowKey,
                TargetPlayerId = v.TargetPlayerId,
                TargetNickname = playerEntities.FirstOrDefault(p => p.RowKey == v.TargetPlayerId)?.Nickname ?? v.TargetPlayerId
            }).ToList();
        }

        return new GameStateDto
        {
            Room = MapRoom(room),
            Players = playerEntities.Select(MapPlayerAdmin).ToList(),
            CurrentTurnPlayerId = gs?.CurrentTurnPlayerId,
            TurnEndsAt = gs?.TurnEndsAt,
            Phase = gs?.Phase,
            CivilianWord = gs?.CivilianWord,
            SpyWord = gs?.SpyWord,
            IsPaused = gs?.IsPaused ?? false,
            PausedSecondsRemaining = gs?.PausedSecondsRemaining ?? 0,
            Winner = gs?.Winner,
            SpyPlayerId = gs?.SpyPlayerId,
            SpyNickname = string.IsNullOrEmpty(gs?.SpyPlayerId) ? null
                : playerEntities.FirstOrDefault(p => p.RowKey == gs.SpyPlayerId)?.Nickname,
            Votes = voteDtos
        };
    }

    private static RoomDto MapRoom(RoomEntity e) => new()
    {
        RoomCode = e.PartitionKey,
        Status = e.Status,
        CurrentRound = e.CurrentRound,
        MaxPlayers = e.MaxPlayers,
        TurnDurationSeconds = e.TurnDurationSeconds,
        VotingDurationSeconds = e.VotingDurationSeconds,
        CreatedAt = e.CreatedAt
    };

    private static PlayerDto MapPlayer(PlayerEntity e) => new()
    {
        PlayerId = e.RowKey,
        Nickname = e.Nickname,
        IsEliminated = e.IsEliminated,
        JoinedAt = e.JoinedAt
    };

    private static PlayerDto MapPlayerAdmin(PlayerEntity e) => new()
    {
        PlayerId = e.RowKey,
        Nickname = e.Nickname,
        IsEliminated = e.IsEliminated,
        IsSpy = e.IsSpy,
        JoinedAt = e.JoinedAt
    };
}
