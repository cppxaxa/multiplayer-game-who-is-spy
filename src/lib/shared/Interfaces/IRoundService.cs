namespace WhoIsSpy.Lib.Shared.Interfaces;

/// <summary>Round lifecycle management contract.</summary>
public interface IRoundService
{
    /// <summary>
    /// Starts a new round: increments CurrentRound, picks a word pair,
    /// assigns the spy role, and sets Phase to Discussion.
    /// </summary>
    Task<bool> StartRoundAsync(string roomCode);

    /// <summary>
    /// Advances the current round state:
    /// - Discussion: moves to the next player's turn; if all done, switches to Voting.
    /// - Voting: tallies votes, eliminates the top-voted player, ends the round.
    /// </summary>
    Task<bool> AdvanceAsync(string roomCode);

    /// <summary>
    /// Immediately transitions a Discussion-phase round to the Voting phase,
    /// skipping any remaining discussion turns. Sets the voting timer from the
    /// room's configured VotingDurationSeconds. Does nothing if the phase is
    /// not Discussion.
    /// </summary>
    Task<bool> SkipToVotingAsync(string roomCode);

    /// <summary>Pauses the timer, recording seconds remaining.</summary>
    Task<bool> PauseTimerAsync(string roomCode);

    /// <summary>Resumes a paused timer, recalculating TurnEndsAt from remaining seconds.</summary>
    Task<bool> ResumeTimerAsync(string roomCode);

    /// <summary>
    /// Updates the per-turn discussion duration on the room.
    /// Takes effect on the next turn advance.
    /// If applyNow is true, also resets the current TurnEndsAt immediately.
    /// </summary>
    Task<bool> SetTurnDurationAsync(string roomCode, int turnSeconds, int votingSeconds, bool applyNow);

    /// <summary>
    /// Checks whether the voting phase should auto-advance (timer expired or all active players
    /// have voted) and, if so, calls AdvanceAsync. Safe to call on every poll — does nothing
    /// unless the conditions are met and the phase is still Voting.
    /// </summary>
    Task TryAutoAdvanceVotingAsync(string roomCode);

    /// <summary>
    /// Advances the turn on behalf of the current speaker.
    /// Only succeeds if the room is InProgress, phase is Discussion,
    /// and playerId matches CurrentTurnPlayerId.
    /// </summary>
    Task<bool> YieldTurnAsync(string roomCode, string playerId);

    /// <summary>
    /// Resets the room to Waiting: clears CurrentRound, un-eliminates and un-spies all players,
    /// and deletes all GameState and Vote rows for this room.
    /// </summary>
    Task<bool> ResetGameAsync(string roomCode);
}
