using Azure;
using Azure.Data.Tables;

namespace WhoIsSpy.Lib.Shared.Entities;

/// <summary>Azure Table Storage entity recording a single vote cast in a round.</summary>
public class VoteEntity : ITableEntity
{
    /// <inheritdoc/>
    public string PartitionKey { get; set; } = string.Empty;  // {RoomCode}_R{RoundNumber}

    /// <inheritdoc/>
    public string RowKey { get; set; } = string.Empty;        // VoterPlayerId

    /// <inheritdoc/>
    public DateTimeOffset? Timestamp { get; set; }

    /// <inheritdoc/>
    public ETag ETag { get; set; }

    /// <summary>PlayerId of the player being voted against.</summary>
    public string TargetPlayerId { get; set; } = string.Empty;

    /// <summary>ISO 8601 UTC timestamp of when the vote was cast.</summary>
    public string CastAt { get; set; } = string.Empty;
}
