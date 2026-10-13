using System;

/// <summary>Stores the board position and initial setup.</summary>
public class ChessBoard
{
    private readonly char[,] squares = new char[8, 8];
    public char this[int row, int col]
    {
        get => squares[row, col];
        set => squares[row, col] = value;
    }
}
