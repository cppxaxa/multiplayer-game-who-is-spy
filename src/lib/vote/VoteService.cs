using WhoIsSpy.Lib.Shared;
using WhoIsSpy.Lib.Shared.Entities;
using WhoIsSpy.Lib.Shared.Interfaces;

namespace WhoIsSpy.Lib.Vote;

/// <summary>Handles vote submission for the active round.</summary>
public class VoteService(
    ITableService<VoteEntity> votes,
    ITableService<PlayerEntity> players) : IVoteService
{
    private readonly ITableService<VoteEntity> _votes = votes;
    private readonly ITableService<PlayerEntity> _players = players;

    /// <inheritdoc/>
    public async Task<bool> CastVoteAsync(
        string roomCode, int roundNumber, string voterId, string targetId)
    {
        // Validate both players exist and are active
        PlayerEntity? voter = await _players.GetAsync(roomCode, voterId);
        PlayerEntity? target = await _players.GetAsync(roomCode, targetId);

        if (voter is null || voter.IsEliminated) return false;
        if (target is null || target.IsEliminated) return false;
        if (voterId == targetId) return false;

        VoteEntity entity = new()
        {
            PartitionKey = Constants.VotePartitionKey(roomCode, roundNumber),
            RowKey = voterId,
            TargetPlayerId = targetId,
            CastAt = DateTime.UtcNow.ToString("o")
        };
        await _votes.UpsertAsync(entity);
        return true;
    }
}
