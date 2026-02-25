namespace WhoIsSpy.Lib.Shared.Enums;

/// <summary>Phase within a single round.</summary>
public enum GamePhase
{
    /// <summary>Players take turns describing their word.</summary>
    Discussion,

    /// <summary>Players vote to eliminate a suspect.</summary>
    Voting,

    /// <summary>Round has concluded; elimination result is shown.</summary>
    Ended
}
