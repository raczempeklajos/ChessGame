
using Xunit;

public class ChessEndgameTests
{
    private readonly ChessGame game;
    private readonly MoveValidator validator;

    public ChessEndgameTests()
    {
        game = new ChessGame();
        game.NewGame();
        validator = new MoveValidator(game);
    }

    private void SetPosition(params string[] rows)
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                game.board[row, col] = rows[row][col];
            }
        }
    }

    [Fact]
    public void StartingPosition_ShouldNotBeCheck()
    {
        Assert.False(validator.IsKingInCheck(true));
        Assert.False(validator.IsKingInCheck(false));
    }

    [Fact]
    public void WhiteKing_ShouldBeInCheckFromRook()
    {
        SetPosition(
            "k...r...",
            "........",
            "........",
            "........",
            "........",
            "........",
            "........",
            "....K..."
        );

        Assert.True(validator.IsKingInCheck(true));
    }

    [Fact]
    public void WhiteKing_ShouldNotBeInCheckWhenBlocked()
    {
        SetPosition(
            "k...r...",
            "........",
            "........",
            "........",
            "....P...",
            "........",
            "........",
            "....K..."
        );

        Assert.False(validator.IsKingInCheck(true));
    }

    [Fact]
    public void King_ShouldNotMoveIntoCheck()
    {
        SetPosition(
            "k...r...",
            "........",
            "........",
            "........",
            "........",
            "........",
            "........",
            "...K...."
        );

        Assert.False(
            validator.IsLegalMove(7, 3, 7, 4, true)
        );
    }

    [Fact]
    public void Checkmate_ShouldHaveNoLegalMoves()
    {
        // Black king on h8, white queen on g7,
        // white king on f6.
        SetPosition(
            ".......k",
            "......Q.",
            ".....K..",
            "........",
            "........",
            "........",
            "........",
            "........"
        );

        Assert.True(validator.IsKingInCheck(false));
        Assert.False(validator.HasLegalMoves(false));
    }

    [Fact]
    public void Stalemate_ShouldHaveNoLegalMovesWithoutCheck()
    {
        // Black king on h8, white queen on g6,
        // white king on f7.
        SetPosition(
            ".......k",
            ".....K..",
            "......Q.",
            "........",
            "........",
            "........",
            "........",
            "........"
        );

        Assert.False(validator.IsKingInCheck(false));
        Assert.False(validator.HasLegalMoves(false));
    }

    [Fact]
    public void StartingPosition_ShouldHaveLegalMoves()
    {
        Assert.True(validator.HasLegalMoves(true));
        Assert.True(validator.HasLegalMoves(false));
    }
}
