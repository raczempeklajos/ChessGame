
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ApiProgram = ChessGameApi.Program;
using Xunit;

/// <summary>
/// Contains integration tests for the chess game REST API.
/// Verifies game creation, retrieval, move execution, and error handling.
/// </summary>
public class GamesApiTests : IClassFixture<WebApplicationFactory<ApiProgram>>
{
    private readonly HttpClient client;

    /// <summary>
    /// Initializes the test client using an in-memory ASP.NET Core test server.
    /// </summary>
    /// <param name="factory">
    /// The web application factory used to create the HTTP client.
    /// </param>
    public GamesApiTests(WebApplicationFactory<ApiProgram> factory)
    {
        client = factory.CreateClient();
    }

    /// <summary>
    /// Verifies that creating a new game returns a successful response,
    /// a unique game identifier, and White as the starting player.
    /// </summary>
    [Fact]
    public async Task CreateGame_ReturnsSuccess()
    {
        var response = await client.PostAsync("/api/games", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.True(json.RootElement.TryGetProperty("id", out _));
        Assert.Equal(
            "White",
            json.RootElement.GetProperty("currentPlayer").GetString());
    }

    /// <summary>
    /// Verifies that requesting a nonexistent game returns HTTP 404.
    /// </summary>
    [Fact]
    public async Task GetGame_WithInvalidId_ReturnsNotFound()
    {
        var response = await client.GetAsync(
            $"/api/games/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies that a legal move is accepted and the active player
    /// changes from White to Black.
    /// </summary>
    [Fact]
    public async Task MakeMove_WithValidMove_ReturnsSuccess()
    {
        // Create a new game before submitting a move.
        var createResponse = await client.PostAsync("/api/games", null);

        using var json = JsonDocument.Parse(
            await createResponse.Content.ReadAsStringAsync());

        string id = json.RootElement.GetProperty("id").GetString()!;

        // Submit the opening pawn move from e2 to e4.
        var response = await client.PostAsJsonAsync(
            $"/api/games/{id}/moves",
            new
            {
                from = "e2",
                to = "e4"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var result = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        // Confirm that the turn switches after a successful move.
        Assert.Equal(
            "Black",
            result.RootElement.GetProperty("currentPlayer").GetString());
    }

    /// <summary>
    /// Verifies that an invalid chessboard coordinate
    /// is rejected with HTTP 400.
    /// </summary>
    [Fact]
    public async Task MakeMove_WithInvalidPosition_ReturnsBadRequest()
    {
        var createResponse = await client.PostAsync("/api/games", null);

        using var json = JsonDocument.Parse(
            await createResponse.Content.ReadAsStringAsync());

        string id = json.RootElement.GetProperty("id").GetString()!;

        // The coordinate z9 is outside the valid chessboard.
        var response = await client.PostAsJsonAsync(
            $"/api/games/{id}/moves",
            new
            {
                from = "z9",
                to = "e4"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies that a newly created game can be retrieved
    /// using its unique identifier.
    /// </summary>
    [Fact]
    public async Task GetGame_AfterCreation_ReturnsGame()
    {
        var createResponse = await client.PostAsync("/api/games", null);

        using var json = JsonDocument.Parse(
            await createResponse.Content.ReadAsStringAsync());

        string id = json.RootElement.GetProperty("id").GetString()!;

        var response = await client.GetAsync($"/api/games/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }


[Theory]
[InlineData("Q")]
[InlineData("R")]
[InlineData("B")]
[InlineData("N")]
[InlineData("q")]
[InlineData("r")]
[InlineData("b")]
[InlineData("n")]
public async Task MakeMove_WithValidPromotionFormat_IsAcceptedByValidation(
    string promotion)
{
    var createResponse = await client.PostAsync("/api/games", null);

    using var createJson = JsonDocument.Parse(
        await createResponse.Content.ReadAsStringAsync());

    string id = createJson.RootElement
        .GetProperty("id").GetString()!;

    // The promotion format is valid, but this move is illegal
    // because the pawn is not on the promotion rank.
    var response = await client.PostAsJsonAsync(
        $"/api/games/{id}/moves",
        new
        {
            from = "e2",
            to = "e4",
            promotion
        });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    using var result = JsonDocument.Parse(
        await response.Content.ReadAsStringAsync());

    // The request reached the chess engine rather than
    // being rejected by ASP.NET Core model validation.
    Assert.Equal(
        "Invalid move",
        result.RootElement.GetProperty("message").GetString());
}

[Theory]
[InlineData("x")]
[InlineData("k")]
[InlineData("queen")]
public async Task MakeMove_WithInvalidPromotionFormat_ReturnsBadRequest(
    string promotion)
{
    var createResponse = await client.PostAsync("/api/games", null);

    using var createJson = JsonDocument.Parse(
        await createResponse.Content.ReadAsStringAsync());

    string id = createJson.RootElement
        .GetProperty("id").GetString()!;

    var response = await client.PostAsJsonAsync(
        $"/api/games/{id}/moves",
        new
        {
            from = "e2",
            to = "e4",
            promotion
        });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    using var result = JsonDocument.Parse(
        await response.Content.ReadAsStringAsync());

    // Model validation rejects the invalid promotion format.
    Assert.True(
        result.RootElement.TryGetProperty("errors", out _));
}


[Fact]
public async Task Promotion_ThroughApi_ShouldUpdateBoardAndHistory()
{
    // Create a new game.
    var createResponse = await client.PostAsync("/api/games", null);

    using var createJson = JsonDocument.Parse(
        await createResponse.Content.ReadAsStringAsync());

    string id = createJson.RootElement
        .GetProperty("id").GetString()!;

    // Move the white pawn from a2 to a8 through legal moves.
    string[] moves =
    {
        "a2 a4",
        "b7 b5",
        "a4 b5",
        "a7 a6",
        "b5 b6",
        "g7 g6",
        "b6 b7",
        "g6 g5"
    };

    foreach (string move in moves)
    {
        string[] squares = move.Split(' ');

        var response = await client.PostAsJsonAsync(
            $"/api/games/{id}/moves",
            new
            {
                from = squares[0],
                to = squares[1]
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Promote the pawn to a knight.
    var promotionResponse = await client.PostAsJsonAsync(
        $"/api/games/{id}/moves",
        new
        {
            from = "b7",
            to = "a8",
            promotion = "N"
        });

    Assert.Equal(HttpStatusCode.OK, promotionResponse.StatusCode);

    using var result = JsonDocument.Parse(
        await promotionResponse.Content.ReadAsStringAsync());

    string[] board = result.RootElement
        .GetProperty("board")
        .EnumerateArray()
        .Select(x => x.GetString()!)
        .ToArray();

    Assert.Equal('N', board[0][0]);

    string[] history = result.RootElement
        .GetProperty("history")
        .EnumerateArray()
        .Select(x => x.GetString()!)
        .ToArray();

    Assert.Contains("b7-a8=N", history);
}


[Fact]
public async Task UndoMove_AfterValidMove_RestoresPreviousState()
{
    // Create a new game.
    var createResponse = await client.PostAsync("/api/games", null);

    using var createJson = JsonDocument.Parse(
        await createResponse.Content.ReadAsStringAsync());

    string id = createJson.RootElement
        .GetProperty("id").GetString()!;

    // Make a legal opening move.
    var moveResponse = await client.PostAsJsonAsync(
        $"/api/games/{id}/moves",
        new
        {
            from = "e2",
            to = "e4"
        });

    Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);

    // Undo the move.
    var undoResponse = await client.PostAsync(
        $"/api/games/{id}/undo", null);

    Assert.Equal(HttpStatusCode.OK, undoResponse.StatusCode);

    using var result = JsonDocument.Parse(
        await undoResponse.Content.ReadAsStringAsync());

    string[] board = result.RootElement
        .GetProperty("board")
        .EnumerateArray()
        .Select(x => x.GetString()!)
        .ToArray();

    // The white pawn returns to e2.
    Assert.Equal('P', board[6][4]);
    Assert.Equal('.', board[4][4]);

    // White should be the active player again.
    Assert.Equal(
        "White",
        result.RootElement.GetProperty("currentPlayer").GetString());

    // The move history should be empty.
    Assert.Empty(
        result.RootElement.GetProperty("history").EnumerateArray());
}

[Fact]
public async Task UndoMove_WithoutPreviousMoves_ReturnsBadRequest()
{
    var createResponse = await client.PostAsync("/api/games", null);

    using var createJson = JsonDocument.Parse(
        await createResponse.Content.ReadAsStringAsync());

    string id = createJson.RootElement
        .GetProperty("id").GetString()!;

    var response = await client.PostAsync(
        $"/api/games/{id}/undo", null);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}

[Fact]
public async Task AiMove_ShouldMakeLegalBlackMove()
{
    // Arrange: create a new game.
    var createResponse = await client.PostAsync("/api/games", null);
    createResponse.EnsureSuccessStatusCode();

    using var createdJson = JsonDocument.Parse(
        await createResponse.Content.ReadAsStringAsync());

    string gameId = createdJson.RootElement
        .GetProperty("id")
        .GetString()!;

    // White makes the first move.
    var whiteMove = await client.PostAsJsonAsync(
        $"/api/games/{gameId}/moves",
        new { from = "e2", to = "e4" });

    whiteMove.EnsureSuccessStatusCode();

    // Act: computer makes a move with black pieces.
    var aiResponse = await client.PostAsync(
        $"/api/games/{gameId}/ai-move", null);

    // Assert
    Assert.Equal(HttpStatusCode.OK, aiResponse.StatusCode);

    using var aiJson = JsonDocument.Parse(
        await aiResponse.Content.ReadAsStringAsync());

    var result = aiJson.RootElement;

    Assert.Equal(
        "White",
        result.GetProperty("currentPlayer").GetString());

    Assert.Equal(
        2,
        result.GetProperty("history").GetArrayLength());

    Assert.False(string.IsNullOrWhiteSpace(
        result.GetProperty("aiMove").GetString()));
}

}
