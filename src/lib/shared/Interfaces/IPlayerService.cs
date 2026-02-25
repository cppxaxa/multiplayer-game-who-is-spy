using WhoIsSpy.Lib.Shared.Models;

namespace WhoIsSpy.Lib.Shared.Interfaces;

/// <summary>Player join, leave, and listing contract.</summary>
public interface IPlayerService
{
    /// <summary>
    /// Registers a player with a nickname and returns their assigned PlayerId.
    /// Returns null if the room is full, not found, or not in Waiting status.
    /// </summary>
    Task<string?> JoinRoomAsync(string roomCode, string nickname);

    /// <summary>Removes a player from the room.</summary>
    Task LeaveRoomAsync(string roomCode, string playerId);

    /// <summary>Returns all players currently in the room.</summary>
    Task<List<PlayerDto>> GetPlayersAsync(string roomCode);

    /// <summary>
    /// Admin-kicks a player: removes them from the room entirely, identical to leaving.
    /// Returns false if the player row no longer exists.
    /// </summary>
    Task<bool> KickPlayerAsync(string roomCode, string playerId);
}
