
using Xunit;

public class ChessSpecialMovesTests
{
    private readonly ChessGame game;

    public ChessSpecialMovesTests()
    {
        game = new ChessGame();
        game.NewGame();
    }

    [Fact]
    public void Castling_ShouldMoveKingAndRook()
    {
        Assert.True(game.MovePiece("e2 e4"));
        Assert.True(game.MovePiece("e7 e5"));
        Assert.True(game.MovePiece("g1 f3"));
        Assert.True(game.MovePiece("b8 c6"));
        Assert.True(game.MovePiece("f1 e2"));
        Assert.True(game.MovePiece("g8 f6"));

        Assert.True(game.MovePiece("e1 g1"));

        Assert.Equal('K', game.board[7, 6]);
        Assert.Equal('R', game.board[7, 5]);
        Assert.Equal('.', game.board[7, 4]);
        Assert.Equal('.', game.board[7, 7]);
    }

    [Fact]
    public void Castling_ShouldFailWhenPathIsBlocked()
    {
        Assert.False(game.MovePiece("e1 g1"));

        Assert.Equal('K', game.board[7, 4]);
        Assert.Equal('R', game.board[7, 7]);
    }

    [Fact]
    public void EnPassant_ShouldCapturePawn()
    {
        Assert.True(game.MovePiece("e2 e4"));
        Assert.True(game.MovePiece("a7 a6"));
        Assert.True(game.MovePiece("e4 e5"));
        Assert.True(game.MovePiece("d7 d5"));

        Assert.True(game.MovePiece("e5 d6"));

        Assert.Equal('P', game.board[2, 3]);
        Assert.Equal('.', game.board[3, 3]);
        Assert.Equal('.', game.board[3, 4]);
    }

    [Fact]
    public void EnPassant_ShouldExpireAfterOneTurn()
    {
        Assert.True(game.MovePiece("e2 e4"));
        Assert.True(game.MovePiece("a7 a6"));
        Assert.True(game.MovePiece("e4 e5"));
        Assert.True(game.MovePiece("d7 d5"));
        Assert.True(game.MovePiece("g1 f3"));
        Assert.True(game.MovePiece("a6 a5"));

        Assert.False(game.MovePiece("e5 d6"));
    }

    [Fact]
    public void Promotion_ShouldPromotePawnToQueen()
    {
        Assert.True(game.MovePiece("a2 a4"));
        Assert.True(game.MovePiece("b7 b5"));
        Assert.True(game.MovePiece("a4 b5"));
        Assert.True(game.MovePiece("h7 h6"));
        Assert.True(game.MovePiece("b5 b6"));
        Assert.True(game.MovePiece("h6 h5"));
        Assert.True(game.MovePiece("b6 b7"));
        Assert.True(game.MovePiece("h5 h4"));

        // Black's rook is on a8, so b8 is occupied by a knight.
        Assert.True(game.MovePiece("b7 a8 q"));

        Assert.Equal('Q', game.board[0, 0]);
    }

    [Fact]
    public void Promotion_ShouldRejectInvalidPiece()
    {
        Assert.False(game.MovePiece("a2 a4 x"));

        Assert.Equal('P', game.board[6, 0]);
    }
}
