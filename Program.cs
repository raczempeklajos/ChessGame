
// ChessGame - A two-player console chess application built with C# and .NET.
// Supports basic chess moves, check/checkmate/stalemate detection,
// move history, undo, captured-piece tracking, statistics, and JSON saves.
// Includes castling, en passant, and pawn promotion.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

class Program
{
    // The board uses uppercase letters for White, lowercase for Black,
    // and a dot ('.') to represent an empty square.
    static char[,] board = new char[8, 8];
    // True when White is the active player; false when Black is active.
    static bool whiteTurn = true;
    // Recorded moves, captured pieces, and snapshots used by the undo command.
    static List<string> moveHistory = new();
    static List<char> capturedPieces = new();
    static Stack<GameState> undoStack = new();

    // Castling rights and the square available for an en passant capture.
    static bool whiteKingSide = true, whiteQueenSide = true;
    static bool blackKingSide = true, blackQueenSide = true;
    static int enPassantRow = -1, enPassantCol = -1;

    /// <summary>
    /// Represents the serializable state of a chess game.
    /// Used for JSON persistence and undo snapshots.
    /// </summary>
    public class GameState
    {
        public string[] Board { get; set; } = Array.Empty<string>();
        public bool WhiteTurn { get; set; }
        public List<string> History { get; set; } = new();
        public List<char> Captured { get; set; } = new();
        public bool WhiteKingSide { get; set; }
        public bool WhiteQueenSide { get; set; }
        public bool BlackKingSide { get; set; }
        public bool BlackQueenSide { get; set; }
        public int EnPassantRow { get; set; } = -1;
        public int EnPassantCol { get; set; } = -1;
    }

    /// <summary>
    /// Runs the game loop, handles commands, and checks for game-ending positions.
    /// </summary>
    static void Main()
    {
        NewGame();

        while (true)
        {
            Console.Clear();
            DisplayBoard();

            bool check = IsKingInCheck(whiteTurn);

            if (!HasLegalMoves(whiteTurn))
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

    /// <summary>
    /// Resets the board and clears all history for a new game.
    /// </summary>
    static void NewGame()
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
    static void DisplayBoard()
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
    static bool MovePiece(string input)
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

        if (!IsInsideBoard(fromRow, fromCol) ||
            !IsInsideBoard(toRow, toCol))
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

        if (!IsLegalMove(fromRow, fromCol, toRow, toCol, whiteTurn))
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
    static void ApplyMove(int fromRow, int fromCol, int toRow, int toCol,
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
    /// Checks whether a row and column are inside the 8-by-8 board.
    /// </summary>
    static bool IsInsideBoard(int row, int col)
    {
        return row >= 0 && row < 8 &&
               col >= 0 && col < 8;
    }

    /// <summary>
    /// Checks movement rules and simulates the complete move to prevent
    /// exposing the moving player's king to check.
    /// </summary>
    static bool IsLegalMove(int fromRow, int fromCol,
        int toRow, int toCol, bool isWhite)
    {
        char piece = board[fromRow, fromCol];
        char target = board[toRow, toCol];

        if (piece == '.' || char.IsUpper(piece) != isWhite)
            return false;
        if (target != '.' && char.IsUpper(target) == isWhite)
            return false;
        if (char.ToLowerInvariant(target) == 'k')
            return false;
        if (!IsValidMove(fromRow, fromCol, toRow, toCol))
            return false;

        // A king cannot castle out of, through, or into check.
        if (char.ToLowerInvariant(piece) == 'k' &&
            Math.Abs(toCol - fromCol) == 2)
        {
            int step = Math.Sign(toCol - fromCol);
            if (IsKingInCheck(isWhite) ||
                IsSquareAttacked(fromRow, fromCol + step, !isWhite))
                return false;
        }

        GameState snapshot = CreateSnapshot();
        ApplyMove(fromRow, fromCol, toRow, toCol, 'q', false);
        bool inCheck = IsKingInCheck(isWhite);
        RestoreSnapshot(snapshot);
        return !inCheck;
    }

    /// <summary>
    /// Validates the movement pattern of a piece without checking king safety.
    /// Handles castling and en passant; promotion is applied after validation.
    /// </summary>
    static bool IsValidMove(
        int fromRow, int fromCol,
        int toRow, int toCol)
    {
        char piece = board[fromRow, fromCol];
        char target = board[toRow, toCol];

        int rowDiff = toRow - fromRow;
        int colDiff = toCol - fromCol;

        int absRow = Math.Abs(rowDiff);
        int absCol = Math.Abs(colDiff);

        if (absRow == 0 && absCol == 0)
            return false;

        switch (char.ToLowerInvariant(piece))
        {
            case 'p':
                int direction = char.IsUpper(piece) ? -1 : 1;
                int startRow = char.IsUpper(piece) ? 6 : 1;

                if (colDiff == 0 && target == '.')
                {
                    if (rowDiff == direction)
                        return true;

                    if (fromRow == startRow &&
                        rowDiff == 2 * direction &&
                        board[fromRow + direction, fromCol] == '.')
                        return true;
                }

                if (absCol == 1 && rowDiff == direction)
                {
                    if (target != '.') return true;
                    return toRow == enPassantRow &&
                           toCol == enPassantCol &&
                           board[fromRow, toCol] ==
                               (char.IsUpper(piece) ? 'p' : 'P');
                }
                return false;

            case 'r':
                return (rowDiff == 0 || colDiff == 0) &&
                       IsPathClear(fromRow, fromCol, toRow, toCol);

            case 'n':
                return (absRow == 2 && absCol == 1) ||
                       (absRow == 1 && absCol == 2);

            case 'b':
                return absRow == absCol &&
                       IsPathClear(fromRow, fromCol, toRow, toCol);

            case 'q':
                return (rowDiff == 0 || colDiff == 0 ||
                        absRow == absCol) &&
                       IsPathClear(fromRow, fromCol, toRow, toCol);

            case 'k':
                if (absRow <= 1 && absCol <= 1)
                    return true;
                if (rowDiff != 0 || absCol != 2 || fromCol != 4 ||
                    fromRow != (char.IsUpper(piece) ? 7 : 0))
                    return false;

                bool kingSide = toCol == 6;
                bool queenSide = toCol == 2;
                if (!kingSide && !queenSide) return false;
                bool allowed = char.IsUpper(piece)
                    ? (kingSide ? whiteKingSide : whiteQueenSide)
                    : (kingSide ? blackKingSide : blackQueenSide);
                if (!allowed) return false;

                int rookCol = kingSide ? 7 : 0;
                if (board[fromRow, rookCol] !=
                    (char.IsUpper(piece) ? 'R' : 'r'))
                    return false;
                int step = kingSide ? 1 : -1;
                for (int col = fromCol + step; col != rookCol; col += step)
                    if (board[fromRow, col] != '.') return false;
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Checks that no pieces block the path of a sliding piece.
    /// The destination square itself is excluded from this check.
    /// </summary>
    static bool IsPathClear(
        int fromRow, int fromCol,
        int toRow, int toCol)
    {
        // Step one square at a time toward the destination.
        int rowStep = Math.Sign(toRow - fromRow);
        int colStep = Math.Sign(toCol - fromCol);

        int row = fromRow + rowStep;
        int col = fromCol + colStep;

        while (row != toRow || col != toCol)
        {
            if (board[row, col] != '.')
                return false;

            row += rowStep;
            col += colStep;
        }

        return true;
    }

    /// <summary>
    /// Determines whether a square is attacked by a piece of the given color.
    /// Pawn attack directions are handled separately from pawn movement.
    /// </summary>
    static bool IsSquareAttacked(
        int targetRow, int targetCol, bool byWhite)
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                char piece = board[row, col];

                if (piece == '.' || char.IsUpper(piece) != byWhite)
                    continue;

                int rowDiff = targetRow - row;
                int colDiff = targetCol - col;

                int absRow = Math.Abs(rowDiff);
                int absCol = Math.Abs(colDiff);

                bool attacks = char.ToLowerInvariant(piece) switch
                {
                    'p' => rowDiff == (byWhite ? -1 : 1) &&
                           absCol == 1,

                    'n' => (absRow == 2 && absCol == 1) ||
                           (absRow == 1 && absCol == 2),

                    'k' => absRow <= 1 && absCol <= 1 &&
                           (absRow != 0 || absCol != 0),

                    'r' => (rowDiff == 0 || colDiff == 0) &&
                           (absRow != 0 || absCol != 0) &&
                           IsPathClear(row, col, targetRow, targetCol),

                    'b' => absRow == absCol && absRow != 0 &&
                           IsPathClear(row, col, targetRow, targetCol),

                    'q' => (rowDiff == 0 || colDiff == 0 ||
                            absRow == absCol) &&
                           (absRow != 0 || absCol != 0) &&
                           IsPathClear(row, col, targetRow, targetCol),

                    _ => false
                };

                if (attacks)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds the specified king and tests whether the opponent attacks it.
    /// </summary>
    static bool IsKingInCheck(bool isWhite)
    {
        char king = isWhite ? 'K' : 'k';

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if (board[row, col] == king)
                    return IsSquareAttacked(row, col, !isWhite);
            }
        }

        return true;
    }

    /// <summary>
    /// Searches for at least one legal move for the specified player.
    /// Used to distinguish checkmate from stalemate.
    /// </summary>
    static bool HasLegalMoves(bool isWhite)
    {
        for (int fromRow = 0; fromRow < 8; fromRow++)
        {
            for (int fromCol = 0; fromCol < 8; fromCol++)
            {
                char piece = board[fromRow, fromCol];

                if (piece == '.' || char.IsUpper(piece) != isWhite)
                    continue;

                for (int toRow = 0; toRow < 8; toRow++)
                {
                    for (int toCol = 0; toCol < 8; toCol++)
                    {
                        if (IsLegalMove(
                            fromRow, fromCol, toRow, toCol, isWhite))
                            return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Copies the board and game metadata into a separate state object.
    /// </summary>
    static GameState CreateSnapshot()
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
    static void RestoreSnapshot(GameState state)
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
    static void UndoMove()
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
    static void ShowHistory()
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
    static void ShowCapturedPieces()
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
    static void ShowStatistics()
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
    static void SaveGame()
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
    static void LoadGame()
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
    static void Pause()
    {
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }
}
