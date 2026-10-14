using System;
using System.Collections.Generic;
using System.Diagnostics;

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
        Running = !gameOver && TimeoutWinner is null;
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