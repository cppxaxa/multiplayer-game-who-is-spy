namespace WhoIsSpy.Lib.Shared.Interfaces;

/// <summary>Vote submission contract.</summary>
public interface IVoteService
{
    /// <summary>
    /// Records or updates a vote from voterId targeting targetId.
    /// Returns false if the voter or target is not a valid active player.
    /// </summary>
    Task<bool> CastVoteAsync(
        string roomCode, int roundNumber, string voterId, string targetId);
}
