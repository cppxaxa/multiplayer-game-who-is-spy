namespace WhoIsSpy.Lib.Shared.Models;

/// <summary>Room data returned to the frontend (no secrets).</summary>
public class RoomDto
{
    /// <summary>6-character uppercase room code.</summary>
    public string RoomCode { get; set; } = string.Empty;

    /// <summary>Room status: Waiting, InProgress, or Ended.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Current active round number (0 = not started).</summary>
    public int CurrentRound { get; set; }

    /// <summary>Maximum players allowed.</summary>
    public int MaxPlayers { get; set; }

    /// <summary>Seconds each player gets to speak during Discussion.</summary>
    public int TurnDurationSeconds { get; set; }

    /// <summary>Seconds allocated for the Voting phase.</summary>
    public int VotingDurationSeconds { get; set; }

    /// <summary>ISO 8601 UTC creation timestamp.</summary>
    public string CreatedAt { get; set; } = string.Empty;
}
