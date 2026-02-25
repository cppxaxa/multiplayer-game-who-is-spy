namespace WhoIsSpy.Lib.Shared.Models;

/// <summary>Player-specific state including the player's own assigned word.</summary>
public class PlayerStateDto
{
    /// <summary>Room metadata.</summary>
    public RoomDto Room { get; set; } = new();

    /// <summary>All players in the room.</summary>
    public List<PlayerDto> Players { get; set; } = [];

    /// <summary>The requesting player's own data.</summary>
    public PlayerDto? Me { get; set; }

    /// <summary>The word assigned to this player for the current round.</summary>
    public string? MyWord { get; set; }

    /// <summary>PlayerId whose turn it currently is.</summary>
    public string? CurrentTurnPlayerId { get; set; }

    /// <summary>ISO 8601 UTC time when the current turn expires.</summary>
    public string? TurnEndsAt { get; set; }

    /// <summary>Current round phase: Discussion, Voting, or Ended.</summary>
    public string? Phase { get; set; }

    /// <summary>True when the admin has paused the timer.</summary>
    public bool IsPaused { get; set; }

    /// <summary>Seconds remaining at the moment of pause (only meaningful when IsPaused=true).</summary>
    public double PausedSecondsRemaining { get; set; }

    /// <summary>PlayerId eliminated at the end of the last round (null if none).</summary>
    public string? EliminatedPlayerId { get; set; }

    /// <summary>Who won: empty = in-progress, "spy" = spy won, "civilians" = civilians won.</summary>
    public string? Winner { get; set; }

    /// <summary>PlayerId of the spy (set when round ends, for winner reveal).</summary>
    public string? SpyPlayerId { get; set; }

    /// <summary>Nickname of the spy (denormalised for easy display in winner reveal).</summary>
    public string? SpyNickname { get; set; }
}
