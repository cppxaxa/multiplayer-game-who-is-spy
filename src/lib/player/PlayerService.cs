using WhoIsSpy.Lib.Shared;
using WhoIsSpy.Lib.Shared.Entities;
using WhoIsSpy.Lib.Shared.Interfaces;
using WhoIsSpy.Lib.Shared.Models;

namespace WhoIsSpy.Lib.Player;

/// <summary>Handles player join, leave, and listing within a room.</summary>
public class PlayerService(
    ITableService<PlayerEntity> players,
    ITableService<RoomEntity> rooms) : IPlayerService
{
    private readonly ITableService<PlayerEntity> _players = players;
    private readonly ITableService<RoomEntity> _rooms = rooms;

    /// <inheritdoc/>
    public async Task<string?> JoinRoomAsync(string roomCode, string nickname)
    {
        RoomEntity? room = await _rooms.GetAsync(roomCode, roomCode);
        if (room is null || room.Status != "Waiting") return null;

        List<PlayerEntity> existing = await _players.QueryAsync(
            $"PartitionKey eq '{roomCode}'");
        if (existing.Count >= room.MaxPlayers) return null;

        string playerId = Guid.NewGuid().ToString("N");
        PlayerEntity entity = new()
        {
            PartitionKey = roomCode,
            RowKey = playerId,
            Nickname = nickname.Trim(),
            IsSpy = false,
            IsEliminated = false,
            JoinedAt = DateTime.UtcNow.ToString("o")
        };
        await _players.UpsertAsync(entity);
        return playerId;
    }

    /// <inheritdoc/>
    public async Task LeaveRoomAsync(string roomCode, string playerId) =>
        await _players.DeleteAsync(roomCode, playerId);

    /// <inheritdoc/>
    public async Task<List<PlayerDto>> GetPlayersAsync(string roomCode)
    {
        List<PlayerEntity> entities = await _players.QueryAsync(
            $"PartitionKey eq '{roomCode}'");
        return entities.Select(Map).ToList();
    }

    /// <inheritdoc/>
    public async Task<bool> KickPlayerAsync(string roomCode, string playerId)
    {
        PlayerEntity? entity = await _players.GetAsync(roomCode, playerId);
        if (entity is null) return false;
        await _players.DeleteAsync(roomCode, playerId);
        return true;
    }

    private static PlayerDto Map(PlayerEntity e) => new()
    {
        PlayerId = e.RowKey,
        Nickname = e.Nickname,
        IsEliminated = e.IsEliminated,
        JoinedAt = e.JoinedAt
    };
}
