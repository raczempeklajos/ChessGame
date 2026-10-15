using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ChessGameApi.Models;

public sealed class GameClock
{
    private readonly List<ClockSnapshot> snapshots = new();
    private long lastTimestamp;

    public int TimeControlMinutes { get; private set; } = 10;
    public double WhiteMilliseconds { get; private set; } = 600_000;
    public double BlackMilliseconds { get; private set; } = 600_000;
    public bool Started { get; private set; }
    public bool Running { get; private set; }
    public string? TimeoutWinner { get; private set; }
    public bool WhiteToMove { get; private set; } = true;

    public bool Start(int minutes, bool whiteToMove)
    {
        if (Started || minutes is not (5 or 10 or 15))
            return false;

        TimeControlMinutes = minutes;
        WhiteMilliseconds = minutes * 60_000.0;
        BlackMilliseconds = minutes * 60_000.0;
        WhiteToMove = whiteToMove;
        Started = true;
        Running = true;
        TimeoutWinner = null;
        snapshots.Clear();

        lastTimestamp = Stopwatch.GetTimestamp();

        return true;
    }

    public void Tick()
    {
        if (!Running)
            return;

        long now = Stopwatch.GetTimestamp();

        double elapsed =
            (now - lastTimestamp) * 1000.0 /
            Stopwatch.Frequency;

        lastTimestamp = now;

        ConsumeElapsed(elapsed);
    }

    private void ConsumeElapsed(double elapsed)
    {
        if (!Running || elapsed <= 0)
            return;

        if (WhiteToMove)
        {
            WhiteMilliseconds = Math.Max(
                0,
                WhiteMilliseconds - elapsed
            );

            if (WhiteMilliseconds == 0)
                Expire("Black");
        }
        else
        {
            BlackMilliseconds = Math.Max(
                0,
                BlackMilliseconds - elapsed
            );

            if (BlackMilliseconds == 0)
                Expire("White");
        }
    }

    public void SaveBeforeMove(int moveCount)
    {
        snapshots.Add(
            new ClockSnapshot(
                moveCount,
                WhiteMilliseconds,
                BlackMilliseconds
            )
        );
    }

    public void CompleteMove(
        bool whiteToMove,
        bool gameOver
    )
    {
        WhiteToMove = whiteToMove;
        Running = Started &&
                  !gameOver &&
                  TimeoutWinner is null;

        lastTimestamp = Stopwatch.GetTimestamp();
    }

    public void Stop()
    {
        Tick();
        Running = false;
    }

    public void RestoreAfterUndo(
        int moveCount,
        bool whiteToMove,
        bool gameOver
    )
    {
        ClockSnapshot? snapshot = snapshots.FindLast(
            s => s.MoveCount == moveCount
        );

        if (snapshot is not null)
        {
            WhiteMilliseconds = snapshot.WhiteMilliseconds;
            BlackMilliseconds = snapshot.BlackMilliseconds;
        }

        snapshots.RemoveAll(
            s => s.MoveCount >= moveCount
        );

        TimeoutWinner = null;
        WhiteToMove = whiteToMove;
        Running = Started && !gameOver;

        lastTimestamp = Stopwatch.GetTimestamp();
    }

    public GameClockPersistenceState ExportPersistenceState()
    {
        Tick();

        return new GameClockPersistenceState
        {
            TimeControlMinutes = TimeControlMinutes,
            WhiteMilliseconds = WhiteMilliseconds,
            BlackMilliseconds = BlackMilliseconds,
            Started = Started,
            Running = Running,
            TimeoutWinner = TimeoutWinner,
            WhiteToMove = WhiteToMove,
            SavedAtUtc = DateTime.UtcNow,

            Snapshots = snapshots.Select(
                snapshot => new GameClockSnapshotState
                {
                    MoveCount = snapshot.MoveCount,
                    WhiteMilliseconds = snapshot.WhiteMilliseconds,
                    BlackMilliseconds = snapshot.BlackMilliseconds
                }
            ).ToList()
        };
    }

    public void ImportPersistenceState(
        GameClockPersistenceState savedState
    )
    {
        ArgumentNullException.ThrowIfNull(savedState);

        if (savedState.TimeControlMinutes is not (5 or 10 or 15))
            throw new ArgumentException("Invalid time control.");

        if (!double.IsFinite(savedState.WhiteMilliseconds) ||
            !double.IsFinite(savedState.BlackMilliseconds) ||
            savedState.WhiteMilliseconds < 0 ||
            savedState.BlackMilliseconds < 0)
        {
            throw new ArgumentException("Invalid clock values.");
        }

        TimeControlMinutes = savedState.TimeControlMinutes;
        WhiteMilliseconds = savedState.WhiteMilliseconds;
        BlackMilliseconds = savedState.BlackMilliseconds;
        Started = savedState.Started;
        Running = savedState.Running;
        TimeoutWinner = savedState.TimeoutWinner;
        WhiteToMove = savedState.WhiteToMove;

        snapshots.Clear();

        foreach (GameClockSnapshotState snapshot in
                 savedState.Snapshots ?? new())
        {
            snapshots.Add(
                new ClockSnapshot(
                    snapshot.MoveCount,
                    snapshot.WhiteMilliseconds,
                    snapshot.BlackMilliseconds
                )
            );
        }

        // Account for time elapsed while the backend was offline.
        if (Running && savedState.SavedAtUtc != default)
        {
            double elapsed = Math.Max(
                0,
                (DateTime.UtcNow - savedState.SavedAtUtc)
                    .TotalMilliseconds
            );

            ConsumeElapsed(elapsed);
        }

        lastTimestamp = Stopwatch.GetTimestamp();
    }

    private void Expire(string winner)
    {
        TimeoutWinner = winner;
        Running = false;
    }

    private sealed record ClockSnapshot(
        int MoveCount,
        double WhiteMilliseconds,
        double BlackMilliseconds
    );
}