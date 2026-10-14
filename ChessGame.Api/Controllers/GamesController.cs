
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
        string status;

        lock (game)
        {
            state = game.GetGameState();
            status = game.GetGameStatus();
        }

        return Ok(new
        {
            id,
            board = state.Board,
            currentPlayer = state.WhiteTurn ? "White" : "Black",
            history = state.History,
            captured = state.Captured,
            status,
            gameOver = status is "Checkmate" or "Stalemate",
            winner = GetWinner(status, state.WhiteTurn)
        });
    }

    /// <summary>
    /// Returns a chessboard in its standard starting position.
    /// </summary>
    [HttpGet("board")]
    public IActionResult GetBoard()
    {
        var game = new ChessGame();
        game.NewGame();

        GameState state = game.GetGameState();
        string status = game.GetGameStatus();

        return Ok(new
        {
            board = state.Board,
            currentPlayer = state.WhiteTurn ? "White" : "Black",
            history = state.History,
            captured = state.Captured,
            status,
            gameOver = status is "Checkmate" or "Stalemate",
            winner = GetWinner(status, state.WhiteTurn)
        });
    }

    /// <summary>
    /// Creates a new chess game and assigns it a unique identifier.
    /// </summary>
    [HttpPost]
    public IActionResult CreateGame()
    {
        var game = new ChessGame();
        game.NewGame();

        Guid gameId = Guid.NewGuid();
        games.TryAdd(gameId, game);

        GameState state;
        string status;

        lock (game)
        {
            state = game.GetGameState();
            status = game.GetGameStatus();
        }

        return Ok(new
        {
            id = gameId,
            message = "New chess game created",
            board = state.Board,
            currentPlayer = state.WhiteTurn ? "White" : "Black",
            history = state.History,
            captured = state.Captured,
            status,
            gameOver = status is "Checkmate" or "Stalemate",
            winner = GetWinner(status, state.WhiteTurn)
        });
    }

    /// <summary>
    /// Attempts to execute a chess move in an existing game.
    /// </summary>
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

        if (string.IsNullOrWhiteSpace(request.From) ||
            string.IsNullOrWhiteSpace(request.To))
        {
            return BadRequest(new
            {
                message = "Source and destination squares are required"
            });
        }

        string move = $"{request.From} {request.To}";

        if (!string.IsNullOrWhiteSpace(request.Promotion))
        {
            move += $" {request.Promotion}";
        }

        GameState state;
        string status;

        lock (game)
        {
            // Prevent moves after checkmate or stalemate.
            string currentStatus = game.GetGameStatus();

            if (currentStatus is "Checkmate" or "Stalemate")
            {
                return BadRequest(new
                {
                    message = "The game is already over",
                    status = currentStatus
                });
            }

            if (!game.MovePiece(move))
            {
                return BadRequest(new
                {
                    message = "Invalid move"
                });
            }

            state = game.GetGameState();
            status = game.GetGameStatus();
        }

        return Ok(new
        {
            id,
            message = "Move completed",
            board = state.Board,
            currentPlayer = state.WhiteTurn ? "White" : "Black",
            history = state.History,
            captured = state.Captured,
            status,
            gameOver = status is "Checkmate" or "Stalemate",
            winner = GetWinner(status, state.WhiteTurn)
        });
    }

    /// <summary>
    /// Returns all legal destination squares for a selected piece.
    /// </summary>
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
        string status;

        lock (game)
        {
            status = game.GetGameStatus();

            legalMoves = status is "Checkmate" or "Stalemate"
                ? new List<string>()
                : game.GetLegalMoves(from);
        }

        return Ok(new
        {
            from,
            legalMoves
        });
    }

    /// <summary>
    /// Returns the winner if the game ended in checkmate.
    /// </summary>
    private static string? GetWinner(string status, bool whiteTurn)
    {
        if (status != "Checkmate")
        {
            return null;
        }

        // The player whose turn it is has been checkmated.
        return whiteTurn ? "Black" : "White";
    }

    /// <summary>
    /// Undoes the last move of an existing chess game.
    /// </summary>
    [HttpPost("{id:guid}/undo")]
    public IActionResult UndoMove(Guid id)
    {
        if (!games.TryGetValue(id, out ChessGame? game))
        {
            return NotFound(new { message = "Game not found" });
        }

        GameState state;
        string status;

        lock (game)
        {
            if (!game.TryUndoMove())
            {
                return BadRequest(new
                {
                    message = "No moves to undo"
                });
            }

            state = game.GetGameState();
            status = game.GetGameStatus();
        }

        return Ok(new
        {
            id,
            message = "Last move undone",
            board = state.Board,
            currentPlayer = state.WhiteTurn ? "White" : "Black",
            history = state.History,
            captured = state.Captured,
            status,
            gameOver = status is "Checkmate" or "Stalemate",
            winner = GetWinner(status, state.WhiteTurn)
        });
    }

    /// <summary>
    /// Makes a computer move using Minimax.
    /// </summary>
    [HttpPost("{id:guid}/ai-move")]
    public IActionResult MakeAiMove(Guid id)
    {
        if (!games.TryGetValue(id, out ChessGame? game))
        {
            return NotFound(new { message = "Game not found" });
        }

        lock (game)
        {
            // The computer plays with black pieces.
            if (game.GetGameState().WhiteTurn)
            {
                return BadRequest(new
                {
                    message = "It is White's turn"
                });
            }

            string currentStatus = game.GetGameStatus();

            if (currentStatus is "Checkmate" or "Stalemate")
            {
                return BadRequest(new
                {
                    message = "The game is already over"
                });
            }

            ChessAI ai = new ChessAI(game);

            // Minimax search with depth 2.
            string? aiMove = ai.GetMinimaxMove(false, 2);

            if (aiMove == null)
            {
                return BadRequest(new
                {
                    message = "No legal moves available"
                });
            }

            bool success = game.MovePiece(aiMove);

            if (!success)
            {
                return BadRequest(new
                {
                    message = "AI move could not be executed"
                });
            }

            GameState state = game.GetGameState();
            string status = game.GetGameStatus();

            return Ok(new
            {
                id,
                message = "AI move completed",
                aiMove,
                board = state.Board,
                currentPlayer = state.WhiteTurn ? "White" : "Black",
                history = state.History,
                captured = state.Captured,
                status,
                gameOver = status is "Checkmate" or "Stalemate",
                winner = GetWinner(status, state.WhiteTurn)
            });
        }
    }
}
