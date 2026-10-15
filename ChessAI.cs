
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class ChessAI

{

    public long NodesEvaluated { get; private set; }

    public long BranchesPruned { get; private set; }

    public TimeSpan SearchTime { get; private set; }

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



private int GetMovePriority(
    int fromRow,
    int fromCol,
    int toRow,
    int toCol)
{
    char piece = game.board[fromRow, fromCol];
    char target = game.board[toRow, toCol];

    int attackerValue = GetPieceValue(piece);
    int victimValue = GetPieceValue(target);

    // Handle en passant captures.
    if (char.ToLowerInvariant(piece) == 'p' &&
        fromCol != toCol &&
        target == '.')
    {
        victimValue = GetPieceValue(
            game.board[fromRow, toCol]);
    }

    int priority = 0;

    // MVV-LVA: Most Valuable Victim,
    // Least Valuable Attacker.
    if (victimValue > 0)
    {
        priority = 1000
            + victimValue * 100
            - attackerValue;
    }

    // Prioritize pawn promotions.
    if (char.ToLowerInvariant(piece) == 'p' &&
        (toRow == 0 || toRow == 7))
    {
        priority += 2000;
    }

    return priority;
}




private static readonly int[,] PawnTable =
{
    {  0,  0,  0,  0,  0,  0,  0,  0 },
    {  5,  5,  5,  5,  5,  5,  5,  5 },
    {  1,  1,  2,  3,  3,  2,  1,  1 },
    {  1,  1,  2,  4,  4,  2,  1,  1 },
    {  0,  0,  1,  3,  3,  1,  0,  0 },
    {  1, -1, -1,  0,  0, -1, -1,  1 },
    {  1,  2,  2, -2, -2,  2,  2,  1 },
    {  0,  0,  0,  0,  0,  0,  0,  0 }
};

private static int GetPawnPositionBonus(
    int row, int col, bool isWhite)
{
    int tableRow = isWhite ? row : 7 - row;

    return PawnTable[tableRow, col];
}




private static readonly int[,] BishopTable =
{
    { -2, -1, -1, -1, -1, -1, -1, -2 },
    { -1,  0,  0,  0,  0,  0,  0, -1 },
    { -1,  0,  1,  1,  1,  1,  0, -1 },
    { -1,  1,  1,  2,  2,  1,  1, -1 },
    { -1,  0,  2,  2,  2,  2,  0, -1 },
    { -1,  1,  1,  1,  1,  1,  1, -1 },
    { -1,  1,  0,  0,  0,  0,  1, -1 },
    { -2, -1, -1, -1, -1, -1, -1, -2 }
};

private static int GetBishopPositionBonus(int row, int col)
{
    return BishopTable[row, col];
}




private static readonly int[,] RookTable =
{
    {  0,  0,  0,  1,  1,  0,  0,  0 },
    {  1,  2,  2,  2,  2,  2,  2,  1 },
    { -1,  0,  0,  0,  0,  0,  0, -1 },
    { -1,  0,  0,  0,  0,  0,  0, -1 },
    { -1,  0,  0,  0,  0,  0,  0, -1 },
    { -1,  0,  0,  0,  0,  0,  0, -1 },
    { -1,  0,  0,  0,  0,  0,  0, -1 },
    {  0,  0,  0,  1,  1,  0,  0,  0 }
};

private static int GetRookPositionBonus(
    int row, int col, bool isWhite)
{
    int tableRow = isWhite ? row : 7 - row;

    return RookTable[tableRow, col];
}




private static readonly int[,] QueenTable =
{
    { -2, -1, -1,  0,  0, -1, -1, -2 },
    { -1,  0,  0,  0,  0,  0,  0, -1 },
    { -1,  0,  1,  1,  1,  1,  0, -1 },
    {  0,  0,  1,  1,  1,  1,  0, -1 },
    { -1,  0,  1,  1,  1,  1,  0, -1 },
    { -1,  1,  1,  1,  1,  1,  0, -1 },
    { -1,  0,  1,  0,  0,  0,  0, -1 },
    { -2, -1, -1,  0,  0, -1, -1, -2 }
};

private static int GetQueenPositionBonus(int row, int col)
{
    return QueenTable[row, col];
}



private static readonly int[,] KnightTable =
{
    { -5, -4, -3, -3, -3, -3, -4, -5 },
    { -4, -2,  0,  0,  0,  0, -2, -4 },
    { -3,  0,  1,  2,  2,  1,  0, -3 },
    { -3,  1,  2,  3,  3,  2,  1, -3 },
    { -3,  0,  2,  3,  3,  2,  0, -3 },
    { -3,  1,  1,  2,  2,  1,  1, -3 },
    { -4, -2,  0,  1,  1,  0, -2, -4 },
    { -5, -4, -3, -3, -3, -3, -4, -5 }
};


private static readonly int[,] KingTable =
{
    {  2,  3,  1, -2, -2,  1,  3,  2 },
    {  1,  1, -1, -3, -3, -1,  1,  1 },
    { -1, -2, -3, -4, -4, -3, -2, -1 },
    { -2, -3, -4, -5, -5, -4, -3, -2 },
    { -2, -3, -4, -5, -5, -4, -3, -2 },
    { -1, -2, -3, -4, -4, -3, -2, -1 },
    {  1,  1, -1, -3, -3, -1,  1,  1 },
    {  2,  3,  1, -2, -2,  1,  3,  2 }
};


private static int GetKnightPositionBonus(int row, int col)
{
    return KnightTable[row, col];
}


private static int GetKingPositionBonus(
    int row, int col, bool isEndgame)
{
    if (isEndgame)
    {
        // Reward central king positions in the endgame.
        int distanceFromCenter =
            Math.Abs(row - 3) + Math.Abs(col - 3);

        return 6 - distanceFromCenter;
    }

    return KingTable[row, col];
}


private bool IsEndgame()
{
    int nonPawnMaterial = 0;

    for (int row = 0; row < 8; row++)
    {
        for (int col = 0; col < 8; col++)
        {
            char piece = char.ToLowerInvariant(
                game.board[row, col]);

            if (piece == 'q' || piece == 'r' ||
                piece == 'b' || piece == 'n')
            {
                nonPawnMaterial += GetPieceValue(piece);
            }
        }
    }

    return nonPawnMaterial <= 12;
}


public int EvaluateBoard(bool isWhite)
{
    int score = 0;

    bool isEndgame = IsEndgame();

    for (int row = 0; row < 8; row++)
    {
        for (int col = 0; col < 8; col++)
        {
            char piece = game.board[row, col];

            if (piece == '.')
                continue;

            int value = GetPieceValue(piece) * 10;

            if (char.ToLowerInvariant(piece) == 'n')
            {
                value += GetKnightPositionBonus(row, col);
            }

            if (char.ToLowerInvariant(piece) == 'p')
            {
                value += GetPawnPositionBonus(
                    row,
                    col,
                    char.IsUpper(piece));
            }

            if (char.ToLowerInvariant(piece) == 'b')
            {
                value += GetBishopPositionBonus(row, col);
            }

            // Rook position evaluation
            if (char.ToLowerInvariant(piece) == 'r')
            {
                value += GetRookPositionBonus(
                    row,
                    col,
                    char.IsUpper(piece));
            }

            // Queen position evaluation
            if (char.ToLowerInvariant(piece) == 'q')
            {
                value += GetQueenPositionBonus(row, col);
            }

            // King position evaluation
            if (char.ToLowerInvariant(piece) == 'k')
            {
                value += GetKingPositionBonus(row, col, isEndgame);
            }

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

    NodesEvaluated = 0;
    BranchesPruned = 0;
    SearchTime = TimeSpan.Zero;

    Stopwatch stopwatch = Stopwatch.StartNew();

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

                int score = Minimax(
                    depth - 1,
                    !isWhite,
                    isWhite,
                    int.MinValue,
                    int.MaxValue);

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
            stopwatch.Stop();
            SearchTime = stopwatch.Elapsed;
            game.RestoreSnapshot(originalState);
        }

}


private int Minimax(
    int depth,
    bool currentWhite,
    bool aiWhite,
    int alpha,
    int beta)
{
    NodesEvaluated++;
    var moves = GetLegalMoves(currentWhite);

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

    int bestScore = maximizing
        ? int.MinValue
        : int.MaxValue;

    foreach (var move in moves)
    {
        GameState snapshot = game.CreateSnapshot();

        try
        {
            game.ApplyMove(
                move.fromRow,
                move.fromCol,
                move.toRow,
                move.toCol,
                'q',
                false);

            int score = Minimax(
                depth - 1,
                !currentWhite,
                aiWhite,
                alpha,
                beta);

            if (maximizing)
            {
                bestScore = Math.Max(bestScore, score);
                alpha = Math.Max(alpha, bestScore);
            }
            else
            {
                bestScore = Math.Min(bestScore, score);
                beta = Math.Min(beta, bestScore);
            }

            
                if (beta <= alpha)
                {
                    BranchesPruned++;
                    break;
                }

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
    List<(int fromRow, int fromCol, int toRow, int toCol)> moves = new();

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
                            fromRow,
                            fromCol,
                            toRow,
                            toCol));
                    }
                }
            }
        }
    }

    moves.Sort((a, b) =>
        GetMovePriority(
            b.fromRow, b.fromCol,
            b.toRow, b.toCol
        ).CompareTo(
            GetMovePriority(
                a.fromRow, a.fromCol,
                a.toRow, a.toCol
            )
        )
    );

    return moves;
}




    private static string ToCoordinate(int row, int col)
    {
        char file = (char)('a' + col);
        int rank = 8 - row;

        return $"{file}{rank}";
    }



public void RunBenchmark(
    bool isWhite,
    int minDepth = 2,
    int maxDepth = 4)
{
    if (minDepth < 1 || maxDepth < minDepth)
        throw new ArgumentOutOfRangeException(nameof(minDepth));

    Console.WriteLine();
    Console.WriteLine("Chess AI Benchmark");
    Console.WriteLine("----------------------------------------");

    for (int depth = minDepth; depth <= maxDepth; depth++)
    {
        string? bestMove = GetMinimaxMove(isWhite, depth);

        Console.WriteLine($"Depth: {depth}");
        Console.WriteLine($"Best move: {bestMove ?? "None"}");
        Console.WriteLine($"Nodes evaluated: {NodesEvaluated}");
        Console.WriteLine($"Branches pruned: {BranchesPruned}");
        Console.WriteLine(
            $"Search time: {SearchTime.TotalMilliseconds:F2} ms");
        Console.WriteLine("----------------------------------------");
    }
}


}
