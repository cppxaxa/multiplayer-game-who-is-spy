namespace WhoIsSpy.Lib.Shared.Enums;

/// <summary>Room lifecycle status.</summary>
public enum RoomStatus
{
    /// <summary>Players are joining; game not yet started.</summary>
    Waiting,

    /// <summary>A round is currently in progress.</summary>
    InProgress,

    /// <summary>Game has ended (spy was found or host ended it).</summary>
    Ended
}
