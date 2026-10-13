using Xunit;

public class ChessBoardTests
{
    [Fact]
    public void NewBoard_ShouldHaveEightRowsAndColumns()
    {
        ChessBoard board = new ChessBoard();

        Assert.Equal('\0', board[0, 0]);
        Assert.Equal('\0', board[7, 7]);
    }

    [Fact]
    public void Board_ShouldStorePieceCorrectly()
    {
        ChessBoard board = new ChessBoard();

        board[0, 0] = 'r';

        Assert.Equal('r', board[0, 0]);
    }

    [Fact]
    public void Board_ShouldAllowDifferentPieces()
    {
        ChessBoard board = new ChessBoard();

        board[0, 4] = 'k';
        board[7, 4] = 'K';

        Assert.Equal('k', board[0, 4]);
        Assert.Equal('K', board[7, 4]);
    }

    [Fact]
    public void Board_ShouldRejectInvalidPosition()
    {
        ChessBoard board = new ChessBoard();

        Assert.Throws<IndexOutOfRangeException>(() => board[8, 0]);
    }
}