using WhoIsSpy.Lib.Shared.Models;

namespace WhoIsSpy.Lib.Shared.Interfaces;

/// <summary>Game state snapshot contract polled by the frontend.</summary>
public interface IStateService
{
    /// <summary>Returns the full game state for a room (no IsSpy exposed).</summary>
    Task<GameStateDto?> GetStateAsync(string roomCode);

    /// <summary>
    /// Returns the player-specific game state including that player's assigned word.
    /// </summary>
    Task<PlayerStateDto?> GetPlayerStateAsync(string roomCode, string playerId);

    /// <summary>
    /// Returns the full game state for admin use — includes IsSpy flag per player
    /// and both civilian and spy words for the current round.
    /// </summary>
    Task<GameStateDto?> GetAdminStateAsync(string roomCode);
}
