
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
}
