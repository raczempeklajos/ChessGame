using Microsoft.AspNetCore.Mvc;
using ChessGameApi.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ChessGameApi.Controllers;

[ApiController]
[Route("api/games")]
public class GamesController : ControllerBase
{
    private static readonly ConcurrentDictionary<Guid, GameSession>
        games = new();

    private sealed class GameSession
    {
        public ChessGame Game { get; } = new();
        public GameClock Clock { get; } = new();
    }

    public sealed class StartGameRequest
    {
        public int Minutes { get; set; } = 10;
    }

    [HttpPost]
    public IActionResult CreateGame()
    {
        var session = new GameSession();
        session.Game.NewGame();

        Guid id = Guid.NewGuid();
        games[id] = session;

        return Ok(BuildResponse(
            id,
            session,
            "New chess game created"
        ));
    }

    [HttpGet("board")]
    public IActionResult GetBoard()
    {
        var session = new GameSession();
        session.Game.NewGame();

        return Ok(BuildResponse(Guid.Empty, session));
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetGame(Guid id)
    {
        if (!games.TryGetValue(id, out GameSession? session))
        {
            return NotFound(new
            {
                message = "Game not found"
            });
        }

        lock (session)
        {
            session.Clock.Tick();

            return Ok(BuildResponse(id, session));
        }
    }

    [HttpPost("{id:guid}/start")]
    public IActionResult StartGame(
        Guid id,
        [FromBody] StartGameRequest request
    )
    {
        if (!games.TryGetValue(id, out GameSession? session))
        {
            return NotFound(new
            {
                message = "Game not found"
            });
        }

        if (request.Minutes is not (5 or 10 or 15))
        {
            return BadRequest(new
            {
                message = "Time control must be 5, 10, or 15 minutes"
            });
        }

        lock (session)
        {
            bool started = session.Clock.Start(
                request.Minutes,
                session.Game.GetGameState().WhiteTurn
            );

            if (!started)
            {
                return BadRequest(new
                {
                    message = "Game has already started"
                });
            }

            return Ok(BuildResponse(
                id,
                session,
                "Game started"
            ));
        }
    }

    [HttpPost("{id:guid}/moves")]
    public IActionResult MakeMove(
        Guid id,
        [FromBody] MoveRequest request
    )
    {
        if (!games.TryGetValue(id, out GameSession? session))
        {
            return NotFound(new
            {
                message = "Game not found"
            });
        }

        if (
            request is null ||
            string.IsNullOrWhiteSpace(request.From) ||
            string.IsNullOrWhiteSpace(request.To)
        )
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

        lock (session)
        {
            session.Clock.Tick();

            if (Blocked(session, out string reason))
            {
                return BadRequest(new
                {
                    message = reason
                });
            }

            GameState before = session.Game.GetGameState();

            if (!session.Game.MovePiece(move))
            {
                return BadRequest(new
                {
                    message = "Invalid move"
                });
            }

            session.Clock.SaveBeforeMove(
                before.History.Count
            );

            GameState after = session.Game.GetGameState();
            string status = session.Game.GetGameStatus();

            session.Clock.CompleteMove(
                after.WhiteTurn,
                IsBoardGameOver(status)
            );

            return Ok(BuildResponse(
                id,
                session,
                "Move completed"
            ));
        }
    }

    [HttpGet("{id:guid}/legal-moves")]
    public IActionResult GetLegalMoves(
        Guid id,
        [FromQuery] string? from
    )
    {
        if (!games.TryGetValue(id, out GameSession? session))
        {
            return NotFound(new
            {
                message = "Game not found"
            });
        }

        if (
            string.IsNullOrWhiteSpace(from) ||
            from.Length != 2 ||
            from[0] is < 'a' or > 'h' ||
            from[1] is < '1' or > '8'
        )
        {
            return BadRequest(new
            {
                message = "Invalid source square"
            });
        }

        lock (session)
        {
            session.Clock.Tick();

            List<string> legalMoves =
                Blocked(session, out _)
                    ? new List<string>()
                    : session.Game.GetLegalMoves(from);

            return Ok(new
            {
                from,
                legalMoves
            });
        }
    }

    [HttpPost("{id:guid}/undo")]
    public IActionResult UndoMove(Guid id)
    {
        if (!games.TryGetValue(id, out GameSession? session))
        {
            return NotFound(new
            {
                message = "Game not found"
            });
        }

        lock (session)
        {
            session.Clock.Tick();

            if (!session.Game.TryUndoMove())
            {
                return BadRequest(new
                {
                    message = "No moves to undo"
                });
            }

            GameState state = session.Game.GetGameState();

            session.Clock.RestoreAfterUndo(
                state.History.Count,
                state.WhiteTurn,
                IsBoardGameOver(
                    session.Game.GetGameStatus()
                )
            );

            return Ok(BuildResponse(
                id,
                session,
                "Last move undone"
            ));
        }
    }

    [HttpPost("{id:guid}/ai-move")]
    public IActionResult MakeAiMove(
        Guid id,
        [FromQuery] string difficulty = "medium"
    )
    {
        if (!games.TryGetValue(id, out GameSession? session))
        {
            return NotFound(new
            {
                message = "Game not found"
            });
        }

        difficulty = (difficulty ?? "medium")
            .ToLowerInvariant();

        if (difficulty is not ("easy" or "medium" or "hard"))
        {
            return BadRequest(new
            {
                message =
                    "Invalid difficulty. Use easy, medium, or hard."
            });
        }

        lock (session)
        {
            session.Clock.Tick();

            if (Blocked(session, out string reason))
            {
                return BadRequest(new
                {
                    message = reason
                });
            }

            GameState before = session.Game.GetGameState();

            if (before.WhiteTurn)
            {
                return BadRequest(new
                {
                    message = "It is White's turn"
                });
            }

            var ai = new ChessAI(session.Game);

            string? aiMove = difficulty switch
            {
                "easy" => ai.GetRandomMove(false),
                "medium" => ai.GetMinimaxMove(false, 2),
                "hard" => ai.GetMinimaxMove(false, 3),
                _ => null
            };

            // AI computation consumes Black's time.
            session.Clock.Tick();

            if (session.Clock.TimeoutWinner is not null)
            {
                return BadRequest(new
                {
                    message = "Time is up",
                    winner = session.Clock.TimeoutWinner
                });
            }

            if (aiMove is null)
            {
                return BadRequest(new
                {
                    message = "No legal moves available"
                });
            }

            if (!session.Game.MovePiece(aiMove))
            {
                return BadRequest(new
                {
                    message = "AI move could not be executed"
                });
            }

            session.Clock.SaveBeforeMove(
                before.History.Count
            );

            GameState after = session.Game.GetGameState();

            session.Clock.CompleteMove(
                after.WhiteTurn,
                IsBoardGameOver(
                    session.Game.GetGameStatus()
                )
            );

            return Ok(BuildResponse(
                id,
                session,
                "AI move completed",
                difficulty,
                aiMove
            ));
        }
    }

    private static bool Blocked(
        GameSession session,
        out string reason
    )
    {
        if (!session.Clock.Started)
        {
            reason = "Game has not started";
            return true;
        }

        if (session.Clock.TimeoutWinner is not null)
        {
            reason = "Time is up";
            return true;
        }

        if (IsBoardGameOver(
            session.Game.GetGameStatus()
        ))
        {
            reason = "The game is already over";
            return true;
        }

        reason = "";
        return false;
    }

    private static bool IsBoardGameOver(string status)
    {
        return status is "Checkmate" or "Stalemate";
    }

    private static string? GetWinner(
        string status,
        bool whiteTurn
    )
    {
        if (status != "Checkmate")
        {
            return null;
        }

        return whiteTurn ? "Black" : "White";
    }

    private static object BuildResponse(
        Guid id,
        GameSession session,
        string? message = null,
        string? difficulty = null,
        string? aiMove = null
    )
    {
        GameState state = session.Game.GetGameState();
        string boardStatus = session.Game.GetGameStatus();

        bool timedOut =
            session.Clock.TimeoutWinner is not null;

        string status = timedOut
            ? "Timeout"
            : boardStatus;

        bool gameOver =
            timedOut || IsBoardGameOver(boardStatus);

        string? winner =
            session.Clock.TimeoutWinner ??
            GetWinner(boardStatus, state.WhiteTurn);

        if (gameOver)
        {
            session.Clock.Stop();
        }

        return new
        {
            id,
            message,
            difficulty,
            aiMove,
            board = state.Board,
            currentPlayer = state.WhiteTurn
                ? "White"
                : "Black",
            history = state.History,
            captured = state.Captured,
            status,
            gameOver,
            winner,
            gameStarted = session.Clock.Started,
            timeControlMinutes =
                session.Clock.TimeControlMinutes,
            whiteTimeMs = Math.Ceiling(
                session.Clock.WhiteMilliseconds
            ),
            blackTimeMs = Math.Ceiling(
                session.Clock.BlackMilliseconds
            )
        };
    }
}