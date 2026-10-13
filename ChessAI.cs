
using System;
using System.Collections.Generic;

public class ChessAI
{
    private readonly ChessGame game;
    private readonly MoveValidator validator;
    private readonly Random random = new();

    public ChessAI(ChessGame game)
    {
        this.game = game;
        validator = new MoveValidator(game);
    }

    public string? GetRandomMove(bool isWhite)
    {
        List<string> legalMoves = new();

        for (int fromRow = 0; fromRow < 8; fromRow++)
        {
            for (int fromCol = 0; fromCol < 8; fromCol++)
            {
                char piece = game.board[fromRow, fromCol];

                if (piece == '.' || char.IsUpper(piece) != isWhite)
                    continue;

                for (int toRow = 0; toRow < 8; toRow++)
                {
                    for (int toCol = 0; toCol < 8; toCol++)
                    {
                        if (!validator.IsLegalMove(
                            fromRow, fromCol, toRow, toCol, isWhite))
                            continue;

                        string from = ToCoordinate(fromRow, fromCol);
                        string to = ToCoordinate(toRow, toCol);

                        legalMoves.Add($"{from} {to}");
                    }
                }
            }
        }

        if (legalMoves.Count == 0)
            return null;

        return legalMoves[random.Next(legalMoves.Count)];
    }

public string? GetBestMove(bool isWhite)
{
    List<string> bestMoves = new();
    int bestScore = int.MinValue;

    for (int fromRow = 0; fromRow < 8; fromRow++)
    {
        for (int fromCol = 0; fromCol < 8; fromCol++)
        {
            char piece = game.board[fromRow, fromCol];

            if (piece == '.' || char.IsUpper(piece) != isWhite)
                continue;

            for (int toRow = 0; toRow < 8; toRow++)
            {
                for (int toCol = 0; toCol < 8; toCol++)
                {
                    if (!validator.IsLegalMove(
                        fromRow, fromCol, toRow, toCol, isWhite))
                        continue;

                    char target = game.board[toRow, toCol];
                    int score = GetPieceValue(target);

                    // Account for en passant captures.
                    if (char.ToLowerInvariant(piece) == 'p' &&
                        fromCol != toCol && target == '.')
                    {
                        score = GetPieceValue(game.board[fromRow, toCol]);
                    }

                    // Prefer promoting a pawn to a queen.
                    if (char.ToLowerInvariant(piece) == 'p' &&
                        toRow == (isWhite ? 0 : 7))
                    {
                        score += 8;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMoves.Clear();
                    }

                    if (score == bestScore)
                    {
                        string from = ToCoordinate(fromRow, fromCol);
                        string to = ToCoordinate(toRow, toCol);

                        bestMoves.Add($"{from} {to}");
                    }
                }
            }
        }
    }

    if (bestMoves.Count == 0)
        return null;

    return bestMoves[random.Next(bestMoves.Count)];
}

private static int GetPieceValue(char piece)
{
    return char.ToLowerInvariant(piece) switch
    {
        'p' => 1,
        'n' => 3,
        'b' => 3,
        'r' => 5,
        'q' => 9,
        _ => 0
    };
}

public int EvaluateBoard(bool isWhite)
{
    int score = 0;

    for (int row = 0; row < 8; row++)
    {
        for (int col = 0; col < 8; col++)
        {
            char piece = game.board[row, col];

            if (piece == '.')
                continue;

            int value = GetPieceValue(piece);

            if (char.IsUpper(piece) == isWhite)
                score += value;
            else
                score -= value;
        }
    }

    return score;
}

public string? GetMinimaxMove(bool isWhite, int depth = 2)
{
    if (depth < 1)
        throw new ArgumentOutOfRangeException(nameof(depth));

    GameState originalState = game.CreateSnapshot();

    try
    {
        List<string> bestMoves = new();
        int bestScore = int.MinValue;

        foreach (var move in GetLegalMoves(isWhite))
        {
            GameState snapshot = game.CreateSnapshot();

            try
            {
                game.ApplyMove(
                    move.fromRow, move.fromCol,
                    move.toRow, move.toCol,
                    'q', false);

                int score = Minimax(depth - 1, !isWhite, isWhite);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMoves.Clear();
                }

                if (score == bestScore)
                {
                    string from = ToCoordinate(move.fromRow, move.fromCol);
                    string to = ToCoordinate(move.toRow, move.toCol);

                    bestMoves.Add($"{from} {to}");
                }
            }
            finally
            {
                game.RestoreSnapshot(snapshot);
            }
        }

        if (bestMoves.Count == 0)
            return null;

        return bestMoves[random.Next(bestMoves.Count)];
    }
    finally
    {
        game.RestoreSnapshot(originalState);
    }
}

private int Minimax(int depth, bool currentWhite, bool aiWhite)
{
    List<(int fromRow, int fromCol, int toRow, int toCol)> moves =
        GetLegalMoves(currentWhite);

    if (moves.Count == 0)
    {
        if (validator.IsKingInCheck(currentWhite))
        {
            return currentWhite == aiWhite
                ? -100000 - depth
                : 100000 + depth;
        }

        return 0;
    }

    if (depth == 0)
        return EvaluateBoard(aiWhite);

    bool maximizing = currentWhite == aiWhite;

    int bestScore = maximizing ? int.MinValue : int.MaxValue;

    foreach (var move in moves)
    {
        GameState snapshot = game.CreateSnapshot();

        try
        {
            game.ApplyMove(
                move.fromRow, move.fromCol,
                move.toRow, move.toCol,
                'q', false);

            int score = Minimax(depth - 1, !currentWhite, aiWhite);

            bestScore = maximizing
                ? Math.Max(bestScore, score)
                : Math.Min(bestScore, score);
        }
        finally
        {
            game.RestoreSnapshot(snapshot);
        }
    }

    return bestScore;
}

private List<(int fromRow, int fromCol, int toRow, int toCol)>
    GetLegalMoves(bool isWhite)
{
    List<(int, int, int, int)> moves = new();

    for (int fromRow = 0; fromRow < 8; fromRow++)
    {
        for (int fromCol = 0; fromCol < 8; fromCol++)
        {
            char piece = game.board[fromRow, fromCol];

            if (piece == '.' || char.IsUpper(piece) != isWhite)
                continue;

            for (int toRow = 0; toRow < 8; toRow++)
            {
                for (int toCol = 0; toCol < 8; toCol++)
                {
                    if (validator.IsLegalMove(
                        fromRow, fromCol, toRow, toCol, isWhite))
                    {
                        moves.Add((
                            fromRow, fromCol,
                            toRow, toCol));
                    }
                }
            }
        }
    }

    return moves;
}



    private static string ToCoordinate(int row, int col)
    {
        char file = (char)('a' + col);
        int rank = 8 - row;

        return $"{file}{rank}";
    }
}
