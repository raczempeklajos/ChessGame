
using Xunit;

public class ChessAITests
{
    [Fact]
    public void GetBestMove_ShouldReturnLegalMove()
    {
        ChessGame game = new();
        game.NewGame();

        ChessAI ai = new(game);
        MoveValidator validator = new(game);

        string? move = ai.GetBestMove(false);

        Assert.NotNull(move);

        string[] positions = move!.Split(' ');

        Assert.Equal(2, positions.Length);

        int fromCol = positions[0][0] - 'a';
        int fromRow = 8 - (positions[0][1] - '0');

        int toCol = positions[1][0] - 'a';
        int toRow = 8 - (positions[1][1] - '0');

        Assert.True(validator.IsLegalMove(
            fromRow, fromCol, toRow, toCol, false));
    }

    [Fact]
    public void GetBestMove_ShouldPreferQueenCapture()
    {
        ChessGame game = new();
        game.NewGame();

        // Clear the board.
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                game.board[row, col] = '.';
            }
        }

        // Place both kings and the pieces for the test.
        game.board[0, 4] = 'k';
        game.board[7, 4] = 'K';
        game.board[4, 4] = 'r';
        game.board[4, 1] = 'Q';
        game.board[1, 4] = 'P';

        ChessAI ai = new(game);

        string? move = ai.GetBestMove(false);

        Assert.Equal("e4 b4", move);
    }

    [Fact]
    public void GetBestMove_ShouldReturnNull_WhenNoLegalMoves()
    {
        ChessGame game = new();
        game.NewGame();

        // Clear the board.
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                game.board[row, col] = '.';
            }
        }

        // Black is checkmated.
        game.board[0, 0] = 'k';
        game.board[1, 1] = 'Q';
        game.board[2, 2] = 'K';

        ChessAI ai = new(game);

        string? move = ai.GetBestMove(false);

        Assert.Null(move);
    }

[Fact]
public void EvaluateBoard_ShouldReturnZero_AtStartingPosition()
{
    ChessGame game = new();
    game.NewGame();

    ChessAI ai = new(game);

    Assert.Equal(0, ai.EvaluateBoard(true));
    Assert.Equal(0, ai.EvaluateBoard(false));
}

[Fact]
public void EvaluateBoard_ShouldDetectMaterialAdvantage()
{
    ChessGame game = new();
    game.NewGame();

    // Remove the black queen.
    game.board[0, 3] = '.';

    ChessAI ai = new(game);

    Assert.Equal(90, ai.EvaluateBoard(true));
    Assert.Equal(-90, ai.EvaluateBoard(false));
}

[Fact]
public void EvaluateBoard_ShouldNotModifyBoard()
{
    ChessGame game = new();
    game.NewGame();

    GameState before = game.CreateSnapshot();

    ChessAI ai = new(game);
    ai.EvaluateBoard(false);

    GameState after = game.CreateSnapshot();

    Assert.Equal(before.Board, after.Board);
    Assert.Equal(before.WhiteTurn, after.WhiteTurn);
    Assert.Equal(before.EnPassantRow, after.EnPassantRow);
    Assert.Equal(before.EnPassantCol, after.EnPassantCol);
}


[Fact]
public void GetMinimaxMove_ShouldReturnLegalMove()
{
    ChessGame game = new();
    game.NewGame();

    ChessAI ai = new(game);
    MoveValidator validator = new(game);

    string? move = ai.GetMinimaxMove(false, 2);

    Assert.NotNull(move);

    string[] positions = move!.Split(' ');

    int fromCol = positions[0][0] - 'a';
    int fromRow = 8 - (positions[0][1] - '0');
    int toCol = positions[1][0] - 'a';
    int toRow = 8 - (positions[1][1] - '0');

    Assert.True(validator.IsLegalMove(
        fromRow, fromCol, toRow, toCol, false));
}

[Fact]
public void GetMinimaxMove_ShouldNotModifyGameState()
{
    ChessGame game = new();
    game.NewGame();

    GameState before = game.CreateSnapshot();

    ChessAI ai = new(game);
    ai.GetMinimaxMove(false, 2);

    GameState after = game.CreateSnapshot();

    Assert.Equal(before.Board, after.Board);
    Assert.Equal(before.WhiteTurn, after.WhiteTurn);
    Assert.Equal(before.WhiteKingSide, after.WhiteKingSide);
    Assert.Equal(before.WhiteQueenSide, after.WhiteQueenSide);
    Assert.Equal(before.BlackKingSide, after.BlackKingSide);
    Assert.Equal(before.BlackQueenSide, after.BlackQueenSide);
    Assert.Equal(before.EnPassantRow, after.EnPassantRow);
    Assert.Equal(before.EnPassantCol, after.EnPassantCol);
    Assert.Equal(before.History, after.History);
    Assert.Equal(before.Captured, after.Captured);
}

[Fact]
public void GetMinimaxMove_ShouldRejectInvalidDepth()
{
    ChessGame game = new();
    game.NewGame();

    ChessAI ai = new(game);

    Assert.Throws<ArgumentOutOfRangeException>(
        () => ai.GetMinimaxMove(false, 0));
}


[Fact]
public void GetMinimaxMove_ShouldWorkAtDepthThree()
{
    ChessGame game = new();
    game.NewGame();

    ChessAI ai = new(game);
    MoveValidator validator = new(game);

    string? move = ai.GetMinimaxMove(false, 3);

    Assert.NotNull(move);

    string[] positions = move!.Split(' ');

    int fromCol = positions[0][0] - 'a';
    int fromRow = 8 - (positions[0][1] - '0');
    int toCol = positions[1][0] - 'a';
    int toRow = 8 - (positions[1][1] - '0');

    Assert.True(validator.IsLegalMove(
        fromRow, fromCol, toRow, toCol, false));
}

[Fact]
public void GetMinimaxMove_ShouldPreserveBoardAtDepthThree()
{
    ChessGame game = new();
    game.NewGame();

    GameState before = game.CreateSnapshot();

    ChessAI ai = new(game);
    ai.GetMinimaxMove(false, 3);

    GameState after = game.CreateSnapshot();

    Assert.Equal(before.Board, after.Board);
    Assert.Equal(before.WhiteTurn, after.WhiteTurn);
    Assert.Equal(before.History, after.History);
    Assert.Equal(before.Captured, after.Captured);
    Assert.Equal(before.EnPassantRow, after.EnPassantRow);
    Assert.Equal(before.EnPassantCol, after.EnPassantCol);
}


[Fact]
public void GetMinimaxMove_ShouldTrackSearchStatistics()
{
    ChessGame game = new();
    game.NewGame();

    ChessAI ai = new(game);

    string? move = ai.GetMinimaxMove(false, 2);

    Assert.NotNull(move);
    Assert.True(ai.NodesEvaluated > 0);
    Assert.True(ai.SearchTime >= TimeSpan.Zero);
    Assert.True(ai.BranchesPruned >= 0);
}

[Fact]
public void GetMinimaxMove_ShouldResetStatistics()
{
    ChessGame game = new();
    game.NewGame();

    ChessAI ai = new(game);

    ai.GetMinimaxMove(false, 2);
    long firstSearchNodes = ai.NodesEvaluated;

    ai.GetMinimaxMove(false, 2);

    Assert.Equal(firstSearchNodes, ai.NodesEvaluated);
}


[Fact]
public void RunBenchmark_ShouldPreserveGameState()
{
    ChessGame game = new();
    game.NewGame();

    GameState before = game.CreateSnapshot();

    ChessAI ai = new(game);
    ai.RunBenchmark(true, 1, 2);

    GameState after = game.CreateSnapshot();

    Assert.Equal(before.Board, after.Board);
    Assert.Equal(before.WhiteTurn, after.WhiteTurn);
    Assert.Equal(before.History, after.History);
    Assert.Equal(before.Captured, after.Captured);
    Assert.Equal(before.EnPassantRow, after.EnPassantRow);
    Assert.Equal(before.EnPassantCol, after.EnPassantCol);
}


[Fact]
public void EvaluateBoard_ShouldPreferCentralKnight()
{
    ChessGame game = new ChessGame();
    game.NewGame();

    ChessAI ai = new ChessAI(game);

    // Clear the board.
    for (int row = 0; row < 8; row++)
    {
        for (int col = 0; col < 8; col++)
        {
            game.board[row, col] = '.';
        }
    }

    // Place the knight in the corner.
    game.board[7, 0] = 'N';
    int cornerScore = ai.EvaluateBoard(true);

    // Move the knight to a central square.
    game.board[7, 0] = '.';
    game.board[4, 3] = 'N';
    int centerScore = ai.EvaluateBoard(true);

    Assert.True(centerScore > cornerScore);
}


[Fact]
public void EvaluateBoard_ShouldRewardAdvancedPawn()
{
    ChessGame game = new ChessGame();
    game.NewGame();

    ChessAI ai = new ChessAI(game);

    // Clear the board.
    for (int row = 0; row < 8; row++)
    {
        for (int col = 0; col < 8; col++)
        {
            game.board[row, col] = '.';
        }
    }

    // White pawn on d2.
    game.board[6, 3] = 'P';
    int startingScore = ai.EvaluateBoard(true);

    // White pawn advances to d5.
    game.board[6, 3] = '.';
    game.board[3, 3] = 'P';
    int advancedScore = ai.EvaluateBoard(true);

    Assert.True(advancedScore > startingScore);
}


[Fact]
public void EvaluateBoard_ShouldPreferActiveBishop()
{
    ChessGame game = new ChessGame();
    game.NewGame();

    ChessAI ai = new ChessAI(game);

    // Clear the board.
    for (int row = 0; row < 8; row++)
    {
        for (int col = 0; col < 8; col++)
        {
            game.board[row, col] = '.';
        }
    }

    // Place the bishop on c1.
    game.board[7, 2] = 'B';
    int startingScore = ai.EvaluateBoard(true);

    // Move the bishop to e3.
    game.board[7, 2] = '.';
    game.board[5, 4] = 'B';
    int activeScore = ai.EvaluateBoard(true);

    Assert.True(activeScore > startingScore);
}


[Fact]
public void EvaluateBoard_ShouldRewardRookOnSeventhRank()
{
    ChessGame game = new ChessGame();
    game.NewGame();

    ChessAI ai = new ChessAI(game);

    // Clear the board.
    for (int row = 0; row < 8; row++)
    {
        for (int col = 0; col < 8; col++)
        {
            game.board[row, col] = '.';
        }
    }

    // White rook on a1.
    game.board[7, 0] = 'R';
    int startingScore = ai.EvaluateBoard(true);

    // White rook on a7.
    game.board[7, 0] = '.';
    game.board[1, 0] = 'R';
    int advancedScore = ai.EvaluateBoard(true);

    Assert.True(advancedScore > startingScore);
}


[Fact]
public void EvaluateBoard_ShouldPreferCentralQueen()
{
    ChessGame game = new ChessGame();
    game.NewGame();

    ChessAI ai = new ChessAI(game);

    // Clear the board.
    for (int row = 0; row < 8; row++)
    {
        for (int col = 0; col < 8; col++)
        {
            game.board[row, col] = '.';
        }
    }

    // Place the queen on a1.
    game.board[7, 0] = 'Q';
    int cornerScore = ai.EvaluateBoard(true);

    // Move the queen to d4.
    game.board[7, 0] = '.';
    game.board[4, 3] = 'Q';
    int centerScore = ai.EvaluateBoard(true);

    Assert.True(centerScore > cornerScore);
}


[Fact]
public void EvaluateBoard_ShouldPreferCentralKingInEndgame()
{
    ChessGame game = new ChessGame();
    game.NewGame();

    ChessAI ai = new ChessAI(game);

    // Clear the board.
    for (int row = 0; row < 8; row++)
    {
        for (int col = 0; col < 8; col++)
        {
            game.board[row, col] = '.';
        }
    }

    // Place the white king on a1.
    game.board[7, 0] = 'K';
    int cornerScore = ai.EvaluateBoard(true);

    // Move the king to d4.
    game.board[7, 0] = '.';
    game.board[4, 3] = 'K';
    int centerScore = ai.EvaluateBoard(true);

    Assert.True(centerScore > cornerScore);
}


}
