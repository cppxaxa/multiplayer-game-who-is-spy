namespace WhoIsSpy.Lib.Shared.Models;

/// <summary>Full game state snapshot polled by the frontend (admin/observer view).</summary>
public class GameStateDto
{
    /// <summary>Room metadata.</summary>
    public RoomDto Room { get; set; } = new();

    /// <summary>All players in the room (IsSpy only populated in admin responses).</summary>
    public List<PlayerDto> Players { get; set; } = [];

    /// <summary>PlayerId whose turn it currently is (null if not in Discussion).</summary>
    public string? CurrentTurnPlayerId { get; set; }

    /// <summary>ISO 8601 UTC time when the current turn expires.</summary>
    public string? TurnEndsAt { get; set; }

    /// <summary>Current round phase: Discussion, Voting, or Ended.</summary>
    public string? Phase { get; set; }

    /// <summary>The word given to civilians — only populated in admin responses.</summary>
    public string? CivilianWord { get; set; }

    /// <summary>The word given to the spy — only populated in admin responses.</summary>
    public string? SpyWord { get; set; }

    /// <summary>True when the admin has paused the timer.</summary>
    public bool IsPaused { get; set; }

    /// <summary>Seconds remaining at the moment of pause (only meaningful when IsPaused=true).</summary>
    public double PausedSecondsRemaining { get; set; }

    /// <summary>Who won: empty = in-progress, "spy" = spy won, "civilians" = civilians won.</summary>
    public string? Winner { get; set; }

    /// <summary>PlayerId of the spy (set when round ends, for winner reveal).</summary>
    public string? SpyPlayerId { get; set; }

    /// <summary>Nickname of the spy (denormalised for easy display in winner reveal).</summary>
    public string? SpyNickname { get; set; }

    /// <summary>
    /// Votes cast so far this round — only populated in admin responses during the Voting phase.
    /// </summary>
    public List<VoteDto> Votes { get; set; } = [];
}
