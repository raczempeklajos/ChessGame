using System;
using System.Collections.Generic;

namespace ChessGameApi.Models;

public sealed class GameClockPersistenceState
{
    public int TimeControlMinutes { get; set; } = 10;
    public double WhiteMilliseconds { get; set; } = 600_000;
    public double BlackMilliseconds { get; set; } = 600_000;
    public bool Started { get; set; }
    public bool Running { get; set; }
    public string? TimeoutWinner { get; set; }
    public bool WhiteToMove { get; set; } = true;
    public DateTime SavedAtUtc { get; set; }

    public List<GameClockSnapshotState> Snapshots { get; set; } = new();
}

public sealed class GameClockSnapshotState
{
    public int MoveCount { get; set; }
    public double WhiteMilliseconds { get; set; }
    public double BlackMilliseconds { get; set; }
}
