
using Microsoft.AspNetCore.Mvc;
using ChessGameApi.Models;
using System.Collections.Concurrent;

namespace ChessGameApi.Controllers;

/// <summary>
/// Provides REST API endpoints for creating, retrieving,
/// and managing chess games.
/// </summary>
[ApiController]
[Route("api/games")]
public class GamesController : ControllerBase
{
    // Stores active chess games using unique identifiers.
    private static readonly ConcurrentDictionary<Guid, ChessGame> games = new();

    /// <summary>
    /// Retrieves the current state of an existing chess game.
    /// </summary>
    /// <param name="id">The unique identifier of the game.</param>
    /// <returns>The game state or a 404 response if the game does not exist.</returns>
    [HttpGet("{id:guid}")]
    public IActionResult GetGame(Guid id)
    {
        if (!games.TryGetValue(id, out ChessGame? game))
        {
            return NotFound(new
            {
                message = "Game not found"
            });
        }

        GameState state;

        // Synchronize access to the game while creating a state snapshot.
        lock (game)
        {
            state = game.GetGameState();
        }

        return Ok(new
        {
            id,
            board = state.Board,
            currentPlayer = state.WhiteTurn ? "White" : "Black",
            history = state.History
        });
    }

    /// <summary>
    /// Returns a chessboard in its standard starting position.
    /// </summary>
    /// <returns>The initial board, current player, and empty move history.</returns>
    [HttpGet("board")]
    public IActionResult GetBoard()
    {
        var game = new ChessGame();

        game.NewGame();

        GameState state = game.GetGameState();

        return Ok(new
        {
            board = state.Board,
            currentPlayer = state.WhiteTurn ? "White" : "Black",
            history = state.History
        });
    }

    /// <summary>
    /// Creates a new chess game and assigns it a unique identifier.
    /// </summary>
    /// <returns>The new game identifier and its initial state.</returns>
    [HttpPost]
    public IActionResult CreateGame()
    {
        var game = new ChessGame();
        game.NewGame();

        // Generate a unique identifier and register the new game.
        Guid gameId = Guid.NewGuid();

        games.TryAdd(gameId, game);

        GameState state;

        lock (game)
        {
            state = game.GetGameState();
        }

        return Ok(new
        {
            id = gameId,
            message = "New chess game created",
            board = state.Board,
            currentPlayer = state.WhiteTurn ? "White" : "Black",
            history = state.History
        });
    }

    /// <summary>
    /// Attempts to execute a chess move in an existing game.
    /// </summary>
    /// <param name="id">The unique identifier of the game.</param>
    /// <param name="request">
    /// The source square, destination square, and optional promotion piece.
    /// </param>
    /// <returns>
    /// The updated game state, or an error response if the game
    /// does not exist or the move is invalid.
    /// </returns>
    [HttpPost("{id:guid}/moves")]
    public IActionResult MakeMove(Guid id, [FromBody] MoveRequest request)
    {
        if (!games.TryGetValue(id, out ChessGame? game))
        {
            return NotFound(new
            {
                message = "Game not found"
            });
        }

        // Convert the API request into the move format expected by the engine.
        string move = $"{request.From} {request.To}";

        // Include the promotion piece when pawn promotion is requested.
        if (!string.IsNullOrWhiteSpace(request.Promotion))
        {
            move += $" {request.Promotion}";
        }

        GameState state;

        // Prevent simultaneous moves from modifying the same game.
        lock (game)
        {
            if (!game.MovePiece(move))
            {
                return BadRequest(new
                {
                    message = "Invalid move"
                });
            }

            // Capture the updated state after the move is completed.
            state = game.GetGameState();
        }

        return Ok(new
        {
            id,
            message = "Move completed",
            board = state.Board,
            currentPlayer = state.WhiteTurn ? "White" : "Black",
            history = state.History
        });
    }


    /// <summary>
    /// Returns all legal destination squares for a selected piece.
    /// </summary>
    /// <param name="id">The unique identifier of the game.</param>
    /// <param name="from">The source square in algebraic notation.</param>
    /// <returns>
    /// The legal destination squares, or an error if the game
    /// does not exist or the source coordinate is invalid.
    /// </returns>
    [HttpGet("{id:guid}/legal-moves")]
    public IActionResult GetLegalMoves(Guid id, [FromQuery] string? from)
    {
        if (!games.TryGetValue(id, out ChessGame? game))
        {
            return NotFound(new
            {
                message = "Game not found"
            });
        }

        if (string.IsNullOrWhiteSpace(from) ||
            from.Length != 2 ||
            from[0] is < 'a' or > 'h' ||
            from[1] is < '1' or > '8')
        {
            return BadRequest(new
            {
                message = "Invalid source square"
            });
        }

        List<string> legalMoves;

        // Synchronize access while the engine evaluates possible moves.
        lock (game)
        {
            legalMoves = game.GetLegalMoves(from);
        }

        return Ok(new
        {
            from,
            legalMoves
        });
    }


}
