using WhoIsSpy.Lib.Shared.Models;

namespace WhoIsSpy.Lib.Shared.Interfaces;

/// <summary>Room creation and retrieval contract.</summary>
public interface IRoomService
{
    /// <summary>Creates a new room with the given settings.</summary>
    Task<RoomDto> CreateRoomAsync(int maxPlayers, int turnDurationSeconds, int votingDurationSeconds);

    /// <summary>Retrieves room metadata by room code. Returns null if not found.</summary>
    Task<RoomDto?> GetRoomAsync(string roomCode);
}
