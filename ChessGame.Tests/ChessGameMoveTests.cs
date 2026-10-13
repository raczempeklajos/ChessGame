
using Xunit;

public class ChessGameMoveTests
{
    private readonly ChessGame game;

    public ChessGameMoveTests()
    {
        game = new ChessGame();
        game.NewGame();
    }

    [Fact]
    public void NewGame_ShouldSetUpStartingPosition()
    {
        Assert.Equal('K', game.board[7, 4]);
        Assert.Equal('k', game.board[0, 4]);
        Assert.Equal('P', game.board[6, 4]);
        Assert.Equal('p', game.board[1, 4]);
        Assert.Equal('.', game.board[4, 4]);
    }

    [Fact]
    public void MovePiece_ShouldAllowWhitePawnToMoveForward()
    {
        bool result = game.MovePiece("e2 e4");

        Assert.True(result);
        Assert.Equal('P', game.board[4, 4]);
        Assert.Equal('.', game.board[6, 4]);
    }

    [Fact]
    public void MovePiece_ShouldAllowBlackPawnAfterWhiteMove()
    {
        game.MovePiece("e2 e4");

        bool result = game.MovePiece("e7 e5");

        Assert.True(result);
        Assert.Equal('p', game.board[3, 4]);
        Assert.Equal('.', game.board[1, 4]);
    }

    [Fact]
    public void MovePiece_ShouldRejectMovingOpponentsPiece()
    {
        bool result = game.MovePiece("e7 e5");

        Assert.False(result);
        Assert.Equal('p', game.board[1, 4]);
    }

    [Fact]
    public void MovePiece_ShouldRejectMovingToOccupiedSquare()
    {
        bool result = game.MovePiece("e1 e2");

        Assert.False(result);
        Assert.Equal('K', game.board[7, 4]);
        Assert.Equal('P', game.board[6, 4]);
    }

    [Fact]
    public void MovePiece_ShouldRejectInvalidCoordinates()
    {
        Assert.False(game.MovePiece("e9 e4"));
        Assert.False(game.MovePiece("z2 e4"));
        Assert.False(game.MovePiece("e2 z4"));
    }

    [Fact]
    public void MovePiece_ShouldRejectInvalidInput()
    {
        Assert.False(game.MovePiece("invalid"));
        Assert.False(game.MovePiece("e2"));
        Assert.False(game.MovePiece("e2 e4 e5"));
    }

    [Fact]
    public void MovePiece_ShouldRejectMovingSamePieceTwiceInOneTurn()
    {
        Assert.True(game.MovePiece("e2 e4"));

        bool result = game.MovePiece("d2 d4");

        Assert.False(result);
        Assert.Equal('P', game.board[6, 3]);
    }

    [Fact]
    public void MovePiece_ShouldAllowKnightJump()
    {
        bool result = game.MovePiece("g1 f3");

        Assert.True(result);
        Assert.Equal('N', game.board[5, 5]);
        Assert.Equal('.', game.board[7, 6]);
    }

    [Fact]
    public void MovePiece_ShouldRejectBishopJumpingOverPawn()
    {
        bool result = game.MovePiece("f1 b5");

        Assert.False(result);
        Assert.Equal('B', game.board[7, 5]);
    }

    [Fact]
    public void MovePiece_ShouldAllowPawnCapture()
    {
        Assert.True(game.MovePiece("e2 e4"));
        Assert.True(game.MovePiece("d7 d5"));

        bool result = game.MovePiece("e4 d5");

        Assert.True(result);
        Assert.Equal('P', game.board[3, 3]);
        Assert.Equal('.', game.board[4, 4]);
    }

    [Fact]
    public void MovePiece_ShouldRejectPawnMovingBackward()
    {
        Assert.True(game.MovePiece("e2 e4"));
        Assert.True(game.MovePiece("a7 a6"));

        bool result = game.MovePiece("e4 e3");

        Assert.False(result);
        Assert.Equal('P', game.board[4, 4]);
    }
}
