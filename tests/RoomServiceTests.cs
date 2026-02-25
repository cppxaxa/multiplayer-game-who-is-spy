using Moq;
using WhoIsSpy.Lib.Room;
using WhoIsSpy.Lib.Shared;
using WhoIsSpy.Lib.Shared.Entities;
using WhoIsSpy.Lib.Shared.Interfaces;
using Xunit;

namespace WhoIsSpy.Tests;

/// <summary>Unit tests for <see cref="RoomService"/>.</summary>
public class RoomServiceTests
{
    private readonly Mock<ITableService<RoomEntity>> _roomsMock = new();

    private RoomService CreateSut() => new(_roomsMock.Object);

    [Fact]
    public async Task CreateRoomAsync_ReturnsRoomWithSixCharCode()
    {
        _roomsMock.Setup(r => r.UpsertAsync(It.IsAny<RoomEntity>()))
                  .Returns(Task.CompletedTask);

        RoomService sut = CreateSut();
        var room = await sut.CreateRoomAsync(20, 60, 120);

        Assert.NotNull(room);
        Assert.Equal(6, room.RoomCode.Length);
        Assert.True(room.RoomCode == room.RoomCode.ToUpperInvariant());
    }

    [Fact]
    public async Task CreateRoomAsync_ClampsMaxPlayersToAllowedRange()
    {
        _roomsMock.Setup(r => r.UpsertAsync(It.IsAny<RoomEntity>()))
                  .Returns(Task.CompletedTask);

        RoomService sut = CreateSut();

        var tooMany = await sut.CreateRoomAsync(999, 60, 120);
        var tooFew  = await sut.CreateRoomAsync(0, 60, 120);

        Assert.Equal(Constants.MaxPlayers, tooMany.MaxPlayers);
        Assert.Equal(Constants.MinPlayers, tooFew.MaxPlayers);
    }

    [Fact]
    public async Task CreateRoomAsync_SetsStatusToWaiting()
    {
        _roomsMock.Setup(r => r.UpsertAsync(It.IsAny<RoomEntity>()))
                  .Returns(Task.CompletedTask);

        RoomService sut = CreateSut();
        var room = await sut.CreateRoomAsync(15, 60, 120);

        Assert.Equal("Waiting", room.Status);
        Assert.Equal(0, room.CurrentRound);
    }

    [Fact]
    public async Task GetRoomAsync_ReturnsNull_WhenNotFound()
    {
        _roomsMock.Setup(r => r.GetAsync("XXXXXX", "XXXXXX"))
                  .ReturnsAsync((RoomEntity?)null);

        RoomService sut = CreateSut();
        var room = await sut.GetRoomAsync("XXXXXX");

        Assert.Null(room);
    }

    [Fact]
    public async Task GetRoomAsync_ReturnsMappedRoom_WhenFound()
    {
        RoomEntity entity = new()
        {
            PartitionKey = "ABC123",
            RowKey = "ABC123",
            Status = "InProgress",
            CurrentRound = 2,
            MaxPlayers = 20,
            CreatedAt = "2026-01-01T00:00:00Z"
        };
        _roomsMock.Setup(r => r.GetAsync("ABC123", "ABC123"))
                  .ReturnsAsync(entity);

        RoomService sut = CreateSut();
        var room = await sut.GetRoomAsync("ABC123");

        Assert.NotNull(room);
        Assert.Equal("ABC123",     room.RoomCode);
        Assert.Equal("InProgress", room.Status);
        Assert.Equal(2,            room.CurrentRound);
    }
}
