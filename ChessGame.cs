using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

/// <summary>Owns the chess game state and console commands.</summary>
public class ChessGame
{
    internal readonly ChessBoard board = new();
    private readonly MoveValidator validator;
    private ChessAI? ai;
    private bool playAgainstComputer = false;
    private bool whiteTurn = true;
    private List<string> moveHistory = new();
    private List<char> capturedPieces = new();
    private Stack<GameState> undoStack = new();
    internal bool whiteKingSide = true, whiteQueenSide = true;
    internal bool blackKingSide = true, blackQueenSide = true;
    internal int enPassantRow = -1, enPassantCol = -1;

    public ChessGame() => validator = new MoveValidator(this);

    /// <summary>
    /// Resets the board and clears all history for a new game.
    /// </summary>
    public void NewGame()
    {
        string[] startingPosition =
        {
            "rnbqkbnr",
            "pppppppp",
            "........",
            "........",
            "........",
            "........",
            "PPPPPPPP",
            "RNBQKBNR"
        };

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                board[row, col] = startingPosition[row][col];
            }
        }

        whiteTurn = true;
        moveHistory.Clear();
        capturedPieces.Clear();
        undoStack.Clear();
        whiteKingSide = whiteQueenSide = true;
        blackKingSide = blackQueenSide = true;
        enPassantRow = enPassantCol = -1;
    }

    /// <summary>
    /// Prints the current board with algebraic coordinates.
    /// </summary>
    public void DisplayBoard()
    {
        Console.WriteLine("\n=== CHESS GAME ===");
        Console.WriteLine("    a b c d e f g h");

        for (int row = 0; row < 8; row++)
        {
            Console.Write($" {8 - row}  ");

            for (int col = 0; col < 8; col++)
            {
                Console.Write(board[row, col] + " ");
            }

            Console.WriteLine($" {8 - row}");
        }

        Console.WriteLine("    a b c d e f g h\n");
        Console.WriteLine("White: UPPERCASE | Black: lowercase\n");
    }

    /// <summary>
    /// Parses a move, validates it, saves an undo snapshot, and applies it.
    /// Promotion syntax: "e7 e8 q" (q, r, b, or n); the default is queen.
    /// </summary>
    public bool MovePiece(string input)
    {
        string[] positions = input.ToLowerInvariant().Split(
            ' ', StringSplitOptions.RemoveEmptyEntries);

        if ((positions.Length != 2 && positions.Length != 3) ||
            positions[0].Length != 2 || positions[1].Length != 2)
            return false;

        int fromCol = positions[0][0] - 'a';
        int fromRow = 8 - (positions[0][1] - '0');
        int toCol = positions[1][0] - 'a';
        int toRow = 8 - (positions[1][1] - '0');

        if (!validator.IsInsideBoard(fromRow, fromCol) ||
            !validator.IsInsideBoard(toRow, toCol))
            return false;

        char promotion = 'q';
        if (positions.Length == 3)
        {
            if (positions[2].Length != 1 ||
                !"qrbn".Contains(positions[2][0]))
                return false;
            promotion = positions[2][0];
        }

        char piece = board[fromRow, fromCol];
        bool promotes = char.ToLowerInvariant(piece) == 'p' &&
            toRow == (whiteTurn ? 0 : 7);
        if (positions.Length == 3 && !promotes)
            return false;

        if (!validator.IsLegalMove(fromRow, fromCol, toRow, toCol, whiteTurn))
            return false;

        undoStack.Push(CreateSnapshot());
        ApplyMove(fromRow, fromCol, toRow, toCol, promotion, true);
        moveHistory.Add($"{positions[0]}-{positions[1]}" +
            (promotes ? $"={char.ToUpperInvariant(promotion)}" : ""));
        whiteTurn = !whiteTurn;
        return true;
    }

    /// <summary>
    /// Applies a move, including the rook movement during castling, en passant
    /// captures, and promotion. Also updates rights needed by future moves.
    /// </summary>
    internal void ApplyMove(int fromRow, int fromCol, int toRow, int toCol,
        char promotion, bool recordCapture)
    {
        char piece = board[fromRow, fromCol];
        char target = board[toRow, toCol];
        bool isWhite = char.IsUpper(piece);
        char type = char.ToLowerInvariant(piece);

        // A rook captured on its original square removes that castling right.
        if (target == 'R' && toRow == 7 && toCol == 0) whiteQueenSide = false;
        if (target == 'R' && toRow == 7 && toCol == 7) whiteKingSide = false;
        if (target == 'r' && toRow == 0 && toCol == 0) blackQueenSide = false;
        if (target == 'r' && toRow == 0 && toCol == 7) blackKingSide = false;

        // En passant removes the pawn beside the starting square.
        if (type == 'p' && fromCol != toCol && target == '.')
        {
            target = board[fromRow, toCol];
            board[fromRow, toCol] = '.';
        }
        if (recordCapture && target != '.') capturedPieces.Add(target);

        board[fromRow, fromCol] = '.';
        board[toRow, toCol] = piece;

        // Castling moves the rook on the same rank as the king.
        if (type == 'k' && Math.Abs(toCol - fromCol) == 2)
        {
            int rookFrom = toCol == 6 ? 7 : 0;
            int rookTo = toCol == 6 ? 5 : 3;
            board[fromRow, rookTo] = board[fromRow, rookFrom];
            board[fromRow, rookFrom] = '.';
        }

        if (type == 'p' && toRow == (isWhite ? 0 : 7))
            board[toRow, toCol] = isWhite
                ? char.ToUpperInvariant(promotion) : promotion;

        if (piece == 'K') whiteKingSide = whiteQueenSide = false;
        if (piece == 'k') blackKingSide = blackQueenSide = false;
        if (piece == 'R' && fromRow == 7 && fromCol == 0) whiteQueenSide = false;
        if (piece == 'R' && fromRow == 7 && fromCol == 7) whiteKingSide = false;
        if (piece == 'r' && fromRow == 0 && fromCol == 0) blackQueenSide = false;
        if (piece == 'r' && fromRow == 0 && fromCol == 7) blackKingSide = false;

        // En passant is available only immediately after a two-square pawn move.
        enPassantRow = enPassantCol = -1;
        if (type == 'p' && Math.Abs(toRow - fromRow) == 2)
        {
            enPassantRow = (fromRow + toRow) / 2;
            enPassantCol = fromCol;
        }
    }

    /// <summary>
    /// Copies the board and game metadata into a separate state object.
    /// </summary>
    internal GameState CreateSnapshot()
    {
        string[] rows = new string[8];

        for (int row = 0; row < 8; row++)
        {
            char[] pieces = new char[8];

            for (int col = 0; col < 8; col++)
                pieces[col] = board[row, col];

            rows[row] = new string(pieces);
        }

        // Copy mutable collections so later moves cannot alter this snapshot.
        return new GameState
        {
            Board = rows,
            WhiteTurn = whiteTurn,
            History = new List<string>(moveHistory),
            Captured = new List<char>(capturedPieces),
            WhiteKingSide = whiteKingSide,
            WhiteQueenSide = whiteQueenSide,
            BlackKingSide = blackKingSide,
            BlackQueenSide = blackQueenSide,
            EnPassantRow = enPassantRow,
            EnPassantCol = enPassantCol
        };
    }

    /// <summary>
    /// Restores a previously captured board position and its metadata.
    /// </summary>
    internal void RestoreSnapshot(GameState state)
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
                board[row, col] = state.Board[row][col];
        }

        whiteTurn = state.WhiteTurn;
        moveHistory = new List<string>(state.History);
        capturedPieces = new List<char>(state.Captured);
        whiteKingSide = state.WhiteKingSide;
        whiteQueenSide = state.WhiteQueenSide;
        blackKingSide = state.BlackKingSide;
        blackQueenSide = state.BlackQueenSide;
        enPassantRow = state.EnPassantRow;
        enPassantCol = state.EnPassantCol;
    }

    /// <summary>
    /// Reverts the most recent move using the last stored snapshot.
    /// </summary>
    public void UndoMove()
    {
        if (undoStack.Count == 0)
        {
            Console.WriteLine("No moves to undo.");
        }
        else
        {
            RestoreSnapshot(undoStack.Pop());
            Console.WriteLine("Last move undone.");
        }

        Pause();
    }

    /// <summary>
    /// Displays recorded moves in numbered White/Black pairs.
    /// </summary>
    public void ShowHistory()
    {
        Console.WriteLine("\n=== MOVE HISTORY ===");

        if (moveHistory.Count == 0)
        {
            Console.WriteLine("No moves yet.");
        }
        else
        {
            for (int i = 0; i < moveHistory.Count; i += 2)
            {
                string blackMove = i + 1 < moveHistory.Count
                    ? moveHistory[i + 1]
                    : "";

                Console.WriteLine(
                    $"{i / 2 + 1}. {moveHistory[i]}  {blackMove}");
            }
        }

        Pause();
    }

    /// <summary>
    /// Lists captured pieces, grouped by the player who captured them.
    /// </summary>
    public void ShowCapturedPieces()
    {
        Console.WriteLine("\n=== CAPTURED PIECES ===");

        Console.Write("White captured: ");

        foreach (char piece in capturedPieces)
        {
            if (char.IsLower(piece))
                Console.Write(piece + " ");
        }

        Console.WriteLine();

        Console.Write("Black captured: ");

        foreach (char piece in capturedPieces)
        {
            if (char.IsUpper(piece))
                Console.Write(piece + " ");
        }

        Console.WriteLine();
        Pause();
    }

    /// <summary>
    /// Displays move counts, capture counts, and the active player.
    /// </summary>
    public void ShowStatistics()
    {
        int whiteCaptures = capturedPieces.Count(
            piece => char.IsLower(piece));

        int blackCaptures = capturedPieces.Count(
            piece => char.IsUpper(piece));

        Console.WriteLine("\n=== GAME STATISTICS ===");
        Console.WriteLine($"Total half-moves: {moveHistory.Count}");
        Console.WriteLine($"White captures: {whiteCaptures}");
        Console.WriteLine($"Black captures: {blackCaptures}");
        Console.WriteLine($"Total captures: {capturedPieces.Count}");
        Console.WriteLine(
            $"Current player: {(whiteTurn ? "White" : "Black")}");

        Pause();
    }

    /// <summary>
    /// Serializes the current game state to chess-save.json.
    /// </summary>
    public void SaveGame()
    {
        try
        {
            GameState state = CreateSnapshot();

            string json = JsonSerializer.Serialize(
                state,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText("chess-save.json", json);

            Console.WriteLine("Game saved successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Save failed: {ex.Message}");
        }

        Pause();
    }

    /// <summary>
    /// Loads a saved game, validates basic board data, and clears undo history.
    /// </summary>
    public void LoadGame()
    {
        try
        {
            string json = File.ReadAllText("chess-save.json");

            GameState? state =
                JsonSerializer.Deserialize<GameState>(json);

            if (state == null ||
                state.Board == null ||
                state.Board.Length != 8 ||
                state.Board.Any(row =>
                    row == null ||
                    row.Length != 8 ||
                    row.Any(piece =>
                        !".prnbqkPRNBQK".Contains(piece))) ||
                state.History == null ||
                state.Captured == null)
            {
                throw new InvalidDataException("Invalid save file.");
            }

            // Old saves lack special-move metadata; disable unavailable rights.
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                if (!document.RootElement.TryGetProperty("WhiteKingSide", out _))
                {
                    state.WhiteKingSide = state.WhiteQueenSide = false;
                    state.BlackKingSide = state.BlackQueenSide = false;
                    state.EnPassantRow = state.EnPassantCol = -1;
                }
            }
            RestoreSnapshot(state);
            undoStack.Clear();

            Console.WriteLine("Game loaded successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Load failed: {ex.Message}");
        }

        Pause();
    }

    /// <summary>
    /// Waits for Enter so console messages remain visible.
    /// </summary>
    public void Pause()
    {
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    /// <summary>
    /// Runs the game loop, handles commands, and checks for game-ending positions.
    /// </summary>
    
public void Run()
{
    NewGame();

    Console.WriteLine("=== GAME MODE ===");
    Console.WriteLine("1. Player vs Player");
    Console.WriteLine("2. Player vs Computer");
    Console.Write("Select game mode (1 or 2): ");

    string mode = (Console.ReadLine() ?? "").Trim();

    playAgainstComputer = mode == "2";

    if (playAgainstComputer)
    {
        ai = new ChessAI(this);
        Console.WriteLine("You play as White. Computer plays as Black.");
    }

    while (true)
    {
        Console.Clear();
        DisplayBoard();

        bool check = validator.IsKingInCheck(whiteTurn);

        if (!validator.HasLegalMoves(whiteTurn))
        {
            Console.WriteLine(check
                ? $"CHECKMATE! {(whiteTurn ? "Black" : "White")} wins!"
                : "STALEMATE! The game is a draw.");

            Console.WriteLine("Type 'new' to play again or 'exit'.");

            string endCommand = (Console.ReadLine() ?? "")
                .Trim().ToLowerInvariant();

            if (endCommand == "new")
            {
                NewGame();
                continue;
            }

            break;
        }

        if (check)
            Console.WriteLine("CHECK! Your king is under attack!");

        if (playAgainstComputer && !whiteTurn)
        {
            string? computerMove = ai!.GetMinimaxMove(false, depth: 2);

            if (computerMove != null)
            {
                MovePiece(computerMove);
                Console.WriteLine($"Computer played: {computerMove}");
                Pause();
            }

            continue;
        }

        Console.WriteLine(
            $"Current player: {(whiteTurn ? "White" : "Black")}");

        Console.WriteLine(
            "Commands: history | undo | captured | stats | save | load | new | exit");

        Console.Write("Enter move (e.g. e2 e4 or e7 e8 q): ");

        string input = (Console.ReadLine() ?? "").Trim();
        string command = input.ToLowerInvariant();

        switch (command)
        {
            case "exit":
                return;

            case "new":
                NewGame();
                continue;

            case "history":
                ShowHistory();
                continue;

            case "undo":
                UndoMove();
                continue;

            case "captured":
                ShowCapturedPieces();
                continue;

            case "stats":
                ShowStatistics();
                continue;

            case "save":
                SaveGame();
                continue;

            case "load":
                LoadGame();
                continue;
        }

        if (!MovePiece(input))
        {
            Console.WriteLine("Invalid move!");
            Pause();
        }
    }
}

}
