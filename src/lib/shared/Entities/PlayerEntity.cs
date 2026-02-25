using Azure;
using Azure.Data.Tables;

namespace WhoIsSpy.Lib.Shared.Entities;

/// <summary>Azure Table Storage entity representing a player in a room.</summary>
public class PlayerEntity : ITableEntity
{
    /// <inheritdoc/>
    public string PartitionKey { get; set; } = string.Empty;  // RoomCode

    /// <inheritdoc/>
    public string RowKey { get; set; } = string.Empty;        // PlayerId (GUID)

    /// <inheritdoc/>
    public DateTimeOffset? Timestamp { get; set; }

    /// <inheritdoc/>
    public ETag ETag { get; set; }

    /// <summary>Player's chosen display name.</summary>
    public string Nickname { get; set; } = string.Empty;

    /// <summary>True when this player has been assigned the spy role for the current round.</summary>
    public bool IsSpy { get; set; }

    /// <summary>True once this player has been voted out.</summary>
    public bool IsEliminated { get; set; }

    /// <summary>ISO 8601 UTC timestamp of when the player joined.</summary>
    public string JoinedAt { get; set; } = string.Empty;
}
