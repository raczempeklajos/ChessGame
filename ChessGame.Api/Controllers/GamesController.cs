using Microsoft.AspNetCore.Mvc;
using ChessGameApi.Data;
using ChessGameApi.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;

namespace ChessGameApi.Controllers;

[ApiController]
[Route("api/games")]
public class GamesController : ControllerBase
{
    private static readonly ConcurrentDictionary<Guid, GameSession> games = new();

    private static readonly SqliteGameRepository repository = new();

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class GameSession
    {
        public ChessGame Game { get; } = new();
        public GameClock Clock { get; } = new();
    }

    public sealed class StartGameRequest
    {
        public int Minutes { get; set; } = 10;
    }

    private static GameSession? FindSession(Guid id)
    {
        if (games.TryGetValue(id, out GameSession? existing))
            return existing;

        string? json = repository.Load(id);

        if (json is null)
            return null;

        SavedGameSession? saved;

        try
        {
            saved = JsonSerializer.Deserialize<SavedGameSession>(
                json,
                jsonOptions
            );
        }
        catch (JsonException)
        {
            return null;
        }

        if (saved?.Game is null || saved.Clock is null)
            return null;

        var restored = new GameSession();

        try
        {
            restored.Game.ImportPersistenceState(saved.Game);
            restored.Clock.ImportPersistenceState(saved.Clock);
        }
        catch (ArgumentException)
        {
            return null;
        }

        return games.GetOrAdd(id, restored);
    }

    private static void SaveSession(Guid id, GameSession session)
    {
        var saved = new SavedGameSession
        {
            Game = session.Game.ExportPersistenceState(),
            Clock = session.Clock.ExportPersistenceState()
        };

        string json = JsonSerializer.Serialize(saved, jsonOptions);

        repository.Save(id, json);
    }

    [HttpPost]
    public IActionResult CreateGame()
    {
        var session = new GameSession();
        session.Game.NewGame();

        Guid id = Guid.NewGuid();
        games[id] = session;

        lock (session)
        {
            return Ok(BuildResponse(
                id,
                session,
                "New chess game created"
            ));
        }
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
        GameSession? session = FindSession(id);

        if (session is null)
            return GameNotFound();

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
        GameSession? session = FindSession(id);

        if (session is null)
            return GameNotFound();

        if (request is null || request.Minutes is not (5 or 10 or 15))
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
        GameSession? session = FindSession(id);

        if (session is null)
            return GameNotFound();

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
            move += $" {request.Promotion}";

        lock (session)
        {
            session.Clock.Tick();

            if (Blocked(session, out string reason))
            {
                SaveSession(id, session);

                return BadRequest(new
                {
                    message = reason
                });
            }

            GameState before = session.Game.GetGameState();

            if (!session.Game.MovePiece(move))
            {
                SaveSession(id, session);

                return BadRequest(new
                {
                    message = "Invalid move"
                });
            }

            session.Clock.SaveBeforeMove(before.History.Count);

            GameState after = session.Game.GetGameState();

            session.Clock.CompleteMove(
                after.WhiteTurn,
                IsBoardGameOver(session.Game.GetGameStatus())
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
        GameSession? session = FindSession(id);

        if (session is null)
            return GameNotFound();

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

            SaveSession(id, session);

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
        GameSession? session = FindSession(id);

        if (session is null)
            return GameNotFound();

        lock (session)
        {
            session.Clock.Tick();

            if (!session.Game.TryUndoMove())
            {
                SaveSession(id, session);

                return BadRequest(new
                {
                    message = "No moves to undo"
                });
            }

            GameState state = session.Game.GetGameState();

            session.Clock.RestoreAfterUndo(
                state.History.Count,
                state.WhiteTurn,
                IsBoardGameOver(session.Game.GetGameStatus())
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
        GameSession? session = FindSession(id);

        if (session is null)
            return GameNotFound();

        difficulty = (difficulty ?? "medium").ToLowerInvariant();

        if (difficulty is not ("easy" or "medium" or "hard"))
        {
            return BadRequest(new
            {
                message = "Invalid difficulty. Use easy, medium, or hard."
            });
        }

        lock (session)
        {
            session.Clock.Tick();

            if (Blocked(session, out string reason))
            {
                SaveSession(id, session);

                return BadRequest(new
                {
                    message = reason
                });
            }

            GameState before = session.Game.GetGameState();

            if (before.WhiteTurn)
            {
                SaveSession(id, session);

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
                SaveSession(id, session);

                return BadRequest(new
                {
                    message = "Time is up",
                    winner = session.Clock.TimeoutWinner
                });
            }

            if (aiMove is null)
            {
                SaveSession(id, session);

                return BadRequest(new
                {
                    message = "No legal moves available"
                });
            }

            if (!session.Game.MovePiece(aiMove))
            {
                SaveSession(id, session);

                return BadRequest(new
                {
                    message = "AI move could not be executed"
                });
            }

            session.Clock.SaveBeforeMove(before.History.Count);

            GameState after = session.Game.GetGameState();

            session.Clock.CompleteMove(
                after.WhiteTurn,
                IsBoardGameOver(session.Game.GetGameStatus())
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

    private static IActionResult GameNotFound()
    {
        return new NotFoundObjectResult(new
        {
            message = "Game not found"
        });
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

        if (IsBoardGameOver(session.Game.GetGameStatus()))
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
            return null;

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
        session.Clock.Tick();

        GameState state = session.Game.GetGameState();
        string boardStatus = session.Game.GetGameStatus();

        bool timedOut = session.Clock.TimeoutWinner is not null;

        string status = timedOut
            ? "Timeout"
            : boardStatus;

        bool gameOver = timedOut || IsBoardGameOver(boardStatus);

        string? winner =
            session.Clock.TimeoutWinner ??
            GetWinner(boardStatus, state.WhiteTurn);

        if (gameOver)
            session.Clock.Stop();

        if (id != Guid.Empty)
            SaveSession(id, session);

        return new
        {
            id,
            message,
            difficulty,
            aiMove,
            board = state.Board,
            currentPlayer = state.WhiteTurn ? "White" : "Black",
            history = state.History,
            captured = state.Captured,
            status,
            gameOver,
            winner,
            gameStarted = session.Clock.Started,
            timeControlMinutes = session.Clock.TimeControlMinutes,
            whiteTimeMs = Math.Ceiling(
                session.Clock.WhiteMilliseconds
            ),
            blackTimeMs = Math.Ceiling(
                session.Clock.BlackMilliseconds
            )
        };
    }
}