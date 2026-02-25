namespace WhoIsSpy.Lib.Shared.Models;

/// <summary>
/// A single vote cast during the Voting phase, returned only in admin state responses.
/// </summary>
public class VoteDto
{
    /// <summary>PlayerId of the player who cast this vote.</summary>
    public string VoterPlayerId { get; set; } = string.Empty;

    /// <summary>Nickname of the voter (denormalised for display).</summary>
    public string VoterNickname { get; set; } = string.Empty;

    /// <summary>PlayerId of the player who was voted against.</summary>
    public string TargetPlayerId { get; set; } = string.Empty;

    /// <summary>Nickname of the target (denormalised for display).</summary>
    public string TargetNickname { get; set; } = string.Empty;
}
