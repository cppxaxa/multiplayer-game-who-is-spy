using Moq;
using WhoIsSpy.Lib.Round;
using WhoIsSpy.Lib.Shared;
using WhoIsSpy.Lib.Shared.Entities;
using WhoIsSpy.Lib.Shared.Interfaces;
using Xunit;

namespace WhoIsSpy.Tests;

/// <summary>Unit tests for <see cref="RoundService"/>.</summary>
public class RoundServiceTests
{
    private readonly Mock<ITableService<RoomEntity>>      _rooms      = new();
    private readonly Mock<ITableService<PlayerEntity>>    _players    = new();
    private readonly Mock<ITableService<GameStateEntity>> _gameStates = new();
    private readonly Mock<ITableService<VoteEntity>>      _votes      = new();

    private RoundService CreateSut() =>
        new(_rooms.Object, _players.Object, _gameStates.Object, _votes.Object);

    [Fact]
    public async Task StartRoundAsync_ReturnsFalse_WhenRoomNotFound()
    {
        _rooms.Setup(r => r.GetAsync("NOROOM", "NOROOM"))
              .ReturnsAsync((RoomEntity?)null);

        RoundService sut = CreateSut();
        bool result = await sut.StartRoundAsync("NOROOM");

        Assert.False(result);
    }

    [Fact]
    public async Task StartRoundAsync_ReturnsFalse_WhenTooFewPlayers()
    {
        _rooms.Setup(r => r.GetAsync("R1", "R1")).ReturnsAsync(new RoomEntity
        {
            PartitionKey = "R1", RowKey = "R1", Status = "Waiting", CurrentRound = 0
        });
        _players.Setup(p => p.QueryAsync(It.IsAny<string>()))
                .ReturnsAsync([]); // no players

        RoundService sut = CreateSut();
        bool result = await sut.StartRoundAsync("R1");

        Assert.False(result);
    }

    [Fact]
    public async Task StartRoundAsync_AssignsExactlyOneSpyAmongActivePlayers()
    {
        string roomCode = "ROOM01";
        _rooms.Setup(r => r.GetAsync(roomCode, roomCode)).ReturnsAsync(new RoomEntity
        {
            PartitionKey = roomCode, RowKey = roomCode,
            Status = "Waiting", CurrentRound = 0, MaxPlayers = 20
        });

        List<PlayerEntity> playerList = Enumerable.Range(1, 5).Select(i => new PlayerEntity
        {
            PartitionKey = roomCode,
            RowKey = $"player{i}",
            Nickname = $"Player{i}",
            IsSpy = false,
            IsEliminated = false,
            JoinedAt = DateTime.UtcNow.AddMinutes(-i).ToString("o")
        }).ToList();

        _players.Setup(p => p.QueryAsync(It.IsAny<string>())).ReturnsAsync(playerList);

        List<PlayerEntity> upserted = [];
        _players.Setup(p => p.UpsertAsync(It.IsAny<PlayerEntity>()))
                .Callback<PlayerEntity>(e => upserted.Add(e))
                .Returns(Task.CompletedTask);
        _rooms.Setup(r => r.UpsertAsync(It.IsAny<RoomEntity>())).Returns(Task.CompletedTask);
        _gameStates.Setup(g => g.UpsertAsync(It.IsAny<GameStateEntity>())).Returns(Task.CompletedTask);

        RoundService sut = CreateSut();
        bool result = await sut.StartRoundAsync(roomCode);

        Assert.True(result);
        int spyCount = upserted.Count(p => p.IsSpy);
        Assert.Equal(1, spyCount);
    }

    [Fact]
    public async Task StartRoundAsync_SetsPhaseToDiscussion()
    {
        string roomCode = "ROOM02";
        _rooms.Setup(r => r.GetAsync(roomCode, roomCode)).ReturnsAsync(new RoomEntity
        {
            PartitionKey = roomCode, RowKey = roomCode,
            Status = "Waiting", CurrentRound = 0
        });

        List<PlayerEntity> twoPlayers =
        [
            new() { PartitionKey = roomCode, RowKey = "p1", Nickname = "Alice",
                    JoinedAt = DateTime.UtcNow.AddMinutes(-3).ToString("o") },
            new() { PartitionKey = roomCode, RowKey = "p2", Nickname = "Bob",
                    JoinedAt = DateTime.UtcNow.AddMinutes(-2).ToString("o") },
            new() { PartitionKey = roomCode, RowKey = "p3", Nickname = "Carol",
                    JoinedAt = DateTime.UtcNow.AddMinutes(-1).ToString("o") }
        ];
        _players.Setup(p => p.QueryAsync(It.IsAny<string>())).ReturnsAsync(twoPlayers);
        _players.Setup(p => p.UpsertAsync(It.IsAny<PlayerEntity>())).Returns(Task.CompletedTask);
        _rooms.Setup(r => r.UpsertAsync(It.IsAny<RoomEntity>())).Returns(Task.CompletedTask);

        GameStateEntity? saved = null;
        _gameStates.Setup(g => g.UpsertAsync(It.IsAny<GameStateEntity>()))
                   .Callback<GameStateEntity>(e => saved = e)
                   .Returns(Task.CompletedTask);

        RoundService sut = CreateSut();
        await sut.StartRoundAsync(roomCode);

        Assert.NotNull(saved);
        Assert.Equal("Discussion", saved.Phase);
        Assert.Equal("1", saved.RowKey);
    }
}
