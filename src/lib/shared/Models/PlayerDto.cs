namespace WhoIsSpy.Lib.Shared.Models;

/// <summary>Player data returned to the frontend. IsSpy is never included here.</summary>
public class PlayerDto
{
    /// <summary>Unique player identifier (GUID).</summary>
    public string PlayerId { get; set; } = string.Empty;

    /// <summary>Player's display name.</summary>
    public string Nickname { get; set; } = string.Empty;

    /// <summary>True once this player has been eliminated by vote.</summary>
    public bool IsEliminated { get; set; }

    /// <summary>
    /// Spy flag — only populated in admin state responses; always null in
    /// player-facing endpoints so the spy's identity is never leaked.
    /// </summary>
    public bool? IsSpy { get; set; }

    /// <summary>ISO 8601 UTC timestamp of when the player joined.</summary>
    public string JoinedAt { get; set; } = string.Empty;
}
