using Azure;
using Azure.Data.Tables;

namespace WhoIsSpy.Lib.Shared.Entities;

/// <summary>Azure Table Storage entity for a round's game state.</summary>
public class GameStateEntity : ITableEntity
{
    /// <inheritdoc/>
    public string PartitionKey { get; set; } = string.Empty;  // RoomCode

    /// <inheritdoc/>
    public string RowKey { get; set; } = string.Empty;        // RoundNumber as string

    /// <inheritdoc/>
    public DateTimeOffset? Timestamp { get; set; }

    /// <inheritdoc/>
    public ETag ETag { get; set; }

    /// <summary>The word shared by civilian players this round.</summary>
    public string CivilianWord { get; set; } = string.Empty;

    /// <summary>The different word given to the spy this round.</summary>
    public string SpyWord { get; set; } = string.Empty;

    /// <summary>PlayerId whose turn it currently is during Discussion phase.</summary>
    public string CurrentTurnPlayerId { get; set; } = string.Empty;

    /// <summary>ISO 8601 UTC time when the current turn expires.</summary>
    public string TurnEndsAt { get; set; } = string.Empty;

    /// <summary>Current round phase: Discussion, Voting, or Ended.</summary>
    public string Phase { get; set; } = "Discussion";

    /// <summary>True when the admin has paused the timer.</summary>
    public bool IsPaused { get; set; }

    /// <summary>ISO 8601 UTC time at which the timer was paused (null when not paused).</summary>
    public string? PausedAt { get; set; }

    /// <summary>Seconds remaining at the moment the timer was paused.</summary>
    public double PausedSecondsRemaining { get; set; }

    /// <summary>
    /// Who won at the end of this round: empty = in-progress, "spy" = spy won, "civilians" = civilians won.
    /// </summary>
    public string Winner { get; set; } = string.Empty;

    /// <summary>PlayerId of the spy player for this round (set when round ends for frontend reveal).</summary>
    public string SpyPlayerId { get; set; } = string.Empty;
}
