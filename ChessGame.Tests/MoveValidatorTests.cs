
using Xunit;

public class MoveValidatorTests
{
    private readonly ChessGame game;
    private readonly MoveValidator validator;

    public MoveValidatorTests()
    {
        game = new ChessGame();
        game.NewGame();
        validator = new MoveValidator(game);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(7, 7, true)]
    [InlineData(3, 4, true)]
    [InlineData(-1, 0, false)]
    [InlineData(8, 0, false)]
    [InlineData(0, -1, false)]
    [InlineData(0, 8, false)]
    public void IsInsideBoard_ShouldValidateCoordinates(
        int row, int col, bool expected)
    {
        Assert.Equal(expected, validator.IsInsideBoard(row, col));
    }

    [Fact]
    public void WhitePawn_ShouldMoveOneSquareForward()
    {
        Assert.True(validator.IsValidMove(6, 4, 5, 4));
    }

    [Fact]
    public void WhitePawn_ShouldMoveTwoSquaresFromStartingPosition()
    {
        Assert.True(validator.IsValidMove(6, 4, 4, 4));
    }

    [Fact]
    public void WhitePawn_ShouldNotMoveBackward()
    {
        Assert.False(validator.IsValidMove(6, 4, 7, 4));
    }

    [Fact]
    public void BlackPawn_ShouldMoveForward()
    {
        Assert.True(validator.IsValidMove(1, 4, 2, 4));
    }

    [Fact]
    public void Knight_ShouldMoveInLShape()
    {
        Assert.True(validator.IsValidMove(7, 6, 5, 5));
    }

    [Fact]
    public void Knight_ShouldNotMoveStraight()
    {
        Assert.False(validator.IsValidMove(7, 6, 5, 6));
    }

    [Fact]
    public void Bishop_ShouldNotJumpOverPieces()
    {
        Assert.False(validator.IsValidMove(7, 2, 5, 4));
    }

    [Fact]
    public void Rook_ShouldNotJumpOverPieces()
    {
        Assert.False(validator.IsValidMove(7, 0, 5, 0));
    }

    [Fact]
    public void Queen_ShouldNotJumpOverPieces()
    {
        Assert.False(validator.IsValidMove(7, 3, 5, 3));
    }

    [Fact]
    public void King_ShouldMoveOneSquare()
    {
        Assert.True(validator.IsValidMove(7, 4, 6, 4));
    }

    [Fact]
    public void King_ShouldNotMoveThreeSquares()
    {
        Assert.False(validator.IsValidMove(7, 4, 4, 4));
    }
}
