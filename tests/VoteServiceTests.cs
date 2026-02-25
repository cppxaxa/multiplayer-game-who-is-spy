using Moq;
using WhoIsSpy.Lib.Shared;
using WhoIsSpy.Lib.Shared.Entities;
using WhoIsSpy.Lib.Shared.Interfaces;
using WhoIsSpy.Lib.Vote;
using Xunit;

namespace WhoIsSpy.Tests;

/// <summary>Unit tests for <see cref="VoteService"/>.</summary>
public class VoteServiceTests
{
    private readonly Mock<ITableService<VoteEntity>>   _votes   = new();
    private readonly Mock<ITableService<PlayerEntity>> _players = new();

    private VoteService CreateSut() => new(_votes.Object, _players.Object);

    private static PlayerEntity ActivePlayer(string roomCode, string id, string name) =>
        new() { PartitionKey = roomCode, RowKey = id, Nickname = name, IsEliminated = false };

    [Fact]
    public async Task CastVoteAsync_ReturnsFalse_WhenVoterNotFound()
    {
        _players.Setup(p => p.GetAsync("R1", "voter1")).ReturnsAsync((PlayerEntity?)null);

        VoteService sut = CreateSut();
        bool result = await sut.CastVoteAsync("R1", 1, "voter1", "target1");

        Assert.False(result);
    }

    [Fact]
    public async Task CastVoteAsync_ReturnsFalse_WhenTargetNotFound()
    {
        _players.Setup(p => p.GetAsync("R1", "voter1"))
                .ReturnsAsync(ActivePlayer("R1", "voter1", "Alice"));
        _players.Setup(p => p.GetAsync("R1", "target1")).ReturnsAsync((PlayerEntity?)null);

        VoteService sut = CreateSut();
        bool result = await sut.CastVoteAsync("R1", 1, "voter1", "target1");

        Assert.False(result);
    }

    [Fact]
    public async Task CastVoteAsync_ReturnsFalse_WhenVotingSelf()
    {
        _players.Setup(p => p.GetAsync("R1", "p1"))
                .ReturnsAsync(ActivePlayer("R1", "p1", "Alice"));

        VoteService sut = CreateSut();
        bool result = await sut.CastVoteAsync("R1", 1, "p1", "p1");

        Assert.False(result);
    }

    [Fact]
    public async Task CastVoteAsync_ReturnsFalse_WhenTargetEliminated()
    {
        _players.Setup(p => p.GetAsync("R1", "voter1"))
                .ReturnsAsync(ActivePlayer("R1", "voter1", "Alice"));
        _players.Setup(p => p.GetAsync("R1", "target1"))
                .ReturnsAsync(new PlayerEntity
                {
                    PartitionKey = "R1", RowKey = "target1",
                    Nickname = "Bob", IsEliminated = true
                });

        VoteService sut = CreateSut();
        bool result = await sut.CastVoteAsync("R1", 1, "voter1", "target1");

        Assert.False(result);
    }

    [Fact]
    public async Task CastVoteAsync_SavesVote_WhenValid()
    {
        _players.Setup(p => p.GetAsync("R1", "voter1"))
                .ReturnsAsync(ActivePlayer("R1", "voter1", "Alice"));
        _players.Setup(p => p.GetAsync("R1", "target1"))
                .ReturnsAsync(ActivePlayer("R1", "target1", "Bob"));

        VoteEntity? saved = null;
        _votes.Setup(v => v.UpsertAsync(It.IsAny<VoteEntity>()))
              .Callback<VoteEntity>(e => saved = e)
              .Returns(Task.CompletedTask);

        VoteService sut = CreateSut();
        bool result = await sut.CastVoteAsync("R1", 2, "voter1", "target1");

        Assert.True(result);
        Assert.NotNull(saved);
        Assert.Equal(Constants.VotePartitionKey("R1", 2), saved.PartitionKey);
        Assert.Equal("voter1",  saved.RowKey);
        Assert.Equal("target1", saved.TargetPlayerId);
    }
}
