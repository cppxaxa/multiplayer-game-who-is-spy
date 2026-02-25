using Azure;
using Azure.Data.Tables;

namespace WhoIsSpy.Lib.Shared.Entities;

/// <summary>Azure Table Storage entity representing a game room.</summary>
public class RoomEntity : ITableEntity
{
    /// <inheritdoc/>
    public string PartitionKey { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string RowKey { get; set; } = string.Empty;

    /// <inheritdoc/>
    public DateTimeOffset? Timestamp { get; set; }

    /// <inheritdoc/>
    public ETag ETag { get; set; }

    /// <summary>Room status: Waiting, InProgress, or Ended.</summary>
    public string Status { get; set; } = "Waiting";

    /// <summary>Current active round number (0 = not started).</summary>
    public int CurrentRound { get; set; }

    /// <summary>ISO 8601 UTC creation timestamp.</summary>
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>Maximum number of players allowed in this room.</summary>
    public int MaxPlayers { get; set; }

    /// <summary>Seconds each player gets to speak during Discussion. Default 60.</summary>
    public int TurnDurationSeconds { get; set; } = 60;

    /// <summary>Seconds allocated for the Voting phase. Default 120.</summary>
    public int VotingDurationSeconds { get; set; } = 120;
}
