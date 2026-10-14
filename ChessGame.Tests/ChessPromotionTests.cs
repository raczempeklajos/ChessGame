
using Xunit;

public class ChessPromotionTests
{
    private readonly ChessGame game;

    public ChessPromotionTests()
    {
        game = new ChessGame();
        game.NewGame();
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

    [Theory]
    [InlineData("q", 'Q')]
    [InlineData("r", 'R')]
    [InlineData("b", 'B')]
    [InlineData("n", 'N')]
    public void WhitePawn_ShouldPromoteToSelectedPiece(
        string promotion, char expectedPiece)
    {
        SetPosition(
            ".......k",
            "P.......",
            "........",
            "........",
            "........",
            "........",
            "........",
            "....K..."
        );

        bool result = game.MovePiece($"a7 a8 {promotion}");

        Assert.True(result);
        Assert.Equal(expectedPiece, game.board[0, 0]);
        Assert.Equal('.', game.board[1, 0]);
    }

    [Theory]
    [InlineData("q", 'q')]
    [InlineData("r", 'r')]
    [InlineData("b", 'b')]
    [InlineData("n", 'n')]
    public void BlackPawn_ShouldPromoteToSelectedPiece(
        string promotion, char expectedPiece)
    {
        SetPosition(
            ".......k",
            "........",
            "........",
            "........",
            "........",
            "........",
            "p.......",
            "....K..."
        );

        // White makes a legal move first.
        Assert.True(game.MovePiece("e1 e2"));

        bool result = game.MovePiece($"a2 a1 {promotion}");

        Assert.True(result);
        Assert.Equal(expectedPiece, game.board[7, 0]);
        Assert.Equal('.', game.board[6, 0]);
    }

    [Fact]
    public void Pawn_ShouldPromoteToQueenByDefault()
    {
        SetPosition(
            ".......k",
            "P.......",
            "........",
            "........",
            "........",
            "........",
            "........",
            "....K..."
        );

        Assert.True(game.MovePiece("a7 a8"));
        Assert.Equal('Q', game.board[0, 0]);
    }

    [Fact]
    public void Pawn_ShouldRejectInvalidPromotionPiece()
    {
        SetPosition(
            ".......k",
            "P.......",
            "........",
            "........",
            "........",
            "........",
            "........",
            "....K..."
        );

        Assert.False(game.MovePiece("a7 a8 x"));
        Assert.Equal('P', game.board[1, 0]);
    }

    [Fact]
    public void Promotion_ShouldNotBeAllowedBeforeLastRank()
    {
        Assert.False(game.MovePiece("e2 e4 q"));
        Assert.Equal('P', game.board[6, 4]);
    }


[Fact]
public void Promotion_ShouldBeRecordedInMoveHistory()
{
    SetPosition(
        ".......k",
        "P.......",
        "........",
        "........",
        "........",
        "........",
        "........",
        "....K..."
    );

    bool result = game.MovePiece("a7 a8 n");

    Assert.True(result);

    GameState state = game.GetGameState();

    Assert.Contains("a7-a8=N", state.History);
}

[Fact]
public void Promotion_ShouldAcceptUppercasePiece()
{
    SetPosition(
        ".......k",
        "P.......",
        "........",
        "........",
        "........",
        "........",
        "........",
        "....K..."
    );

    bool result = game.MovePiece("a7 a8 R");

    Assert.True(result);
    Assert.Equal('R', game.board[0, 0]);
}

[Fact]
public void InvalidPromotion_ShouldNotChangeBoard()
{
    SetPosition(
        ".......k",
        "P.......",
        "........",
        "........",
        "........",
        "........",
        "........",
        "....K..."
    );

    bool result = game.MovePiece("a7 a8 k");

    Assert.False(result);
    Assert.Equal('P', game.board[1, 0]);
    Assert.Equal('.', game.board[0, 0]);
}


}
