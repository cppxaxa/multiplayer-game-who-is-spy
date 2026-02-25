using Azure.Data.Tables;
using WhoIsSpy.Lib.Shared;
using WhoIsSpy.Lib.Shared.Entities;
using WhoIsSpy.Lib.Shared.Interfaces;
using WhoIsSpy.Lib.Shared.Models;

namespace WhoIsSpy.Lib.Room;

/// <summary>Handles room creation and retrieval.</summary>
public class RoomService(ITableService<RoomEntity> rooms) : IRoomService
{
    private readonly ITableService<RoomEntity> _rooms = rooms;

    /// <inheritdoc/>
    public async Task<RoomDto> CreateRoomAsync(int maxPlayers, int turnDurationSeconds, int votingDurationSeconds)
    {
        string code = GenerateRoomCode();
        RoomEntity entity = new()
        {
            PartitionKey = code,
            RowKey = code,
            Status = "Waiting",
            CurrentRound = 0,
            MaxPlayers = Math.Clamp(maxPlayers, Constants.MinPlayers, Constants.MaxPlayers),
            TurnDurationSeconds = turnDurationSeconds > 0 ? turnDurationSeconds : Constants.TurnSeconds,
            VotingDurationSeconds = votingDurationSeconds > 0 ? votingDurationSeconds : Constants.VotingSeconds,
            CreatedAt = DateTime.UtcNow.ToString("o")
        };
        await _rooms.UpsertAsync(entity);
        return Map(entity);
    }

    /// <inheritdoc/>
    public async Task<RoomDto?> GetRoomAsync(string roomCode)
    {
        RoomEntity? entity = await _rooms.GetAsync(roomCode, roomCode);
        return entity is null ? null : Map(entity);
    }

    private static RoomDto Map(RoomEntity e) => new()
    {
        RoomCode = e.PartitionKey,
        Status = e.Status,
        CurrentRound = e.CurrentRound,
        MaxPlayers = e.MaxPlayers,
        TurnDurationSeconds = e.TurnDurationSeconds,
        VotingDurationSeconds = e.VotingDurationSeconds,
        CreatedAt = e.CreatedAt
    };

    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[Random.Shared.Next(chars.Length)])
            .ToArray());
    }
}
