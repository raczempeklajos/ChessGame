using System;

/// <summary>Validates legal moves, checks, and endgame positions.</summary>
public class MoveValidator
{
    private readonly ChessGame game;
    public MoveValidator(ChessGame game) => this.game = game;

    /// <summary>
    /// Checks whether a row and column are inside the 8-by-8 game.board.
    /// </summary>
    public bool IsInsideBoard(int row, int col)
    {
        return row >= 0 && row < 8 &&
               col >= 0 && col < 8;
    }

    /// <summary>
    /// Checks movement rules and simulates the complete move to prevent
    /// exposing the moving player's king to check.
    /// </summary>
    public bool IsLegalMove(int fromRow, int fromCol,
        int toRow, int toCol, bool isWhite)
    {
        char piece = game.board[fromRow, fromCol];
        char target = game.board[toRow, toCol];

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

        GameState snapshot = game.CreateSnapshot();
        game.ApplyMove(fromRow, fromCol, toRow, toCol, 'q', false);
        bool inCheck = IsKingInCheck(isWhite);
        game.RestoreSnapshot(snapshot);
        return !inCheck;
    }

    /// <summary>
    /// Validates the movement pattern of a piece without checking king safety.
    /// Handles castling and en passant; promotion is applied after validation.
    /// </summary>
    public bool IsValidMove(
        int fromRow, int fromCol,
        int toRow, int toCol)
    {
        char piece = game.board[fromRow, fromCol];
        char target = game.board[toRow, toCol];

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
                        game.board[fromRow + direction, fromCol] == '.')
                        return true;
                }

                if (absCol == 1 && rowDiff == direction)
                {
                    if (target != '.') return true;
                    return toRow == game.enPassantRow &&
                           toCol == game.enPassantCol &&
                           game.board[fromRow, toCol] ==
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
                    ? (kingSide ? game.whiteKingSide : game.whiteQueenSide)
                    : (kingSide ? game.blackKingSide : game.blackQueenSide);
                if (!allowed) return false;

                int rookCol = kingSide ? 7 : 0;
                if (game.board[fromRow, rookCol] !=
                    (char.IsUpper(piece) ? 'R' : 'r'))
                    return false;
                int step = kingSide ? 1 : -1;
                for (int col = fromCol + step; col != rookCol; col += step)
                    if (game.board[fromRow, col] != '.') return false;
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Checks that no pieces block the path of a sliding piece.
    /// The destination square itself is excluded from this check.
    /// </summary>
    public bool IsPathClear(
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
            if (game.board[row, col] != '.')
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
    public bool IsSquareAttacked(
        int targetRow, int targetCol, bool byWhite)
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                char piece = game.board[row, col];

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
    public bool IsKingInCheck(bool isWhite)
    {
        char king = isWhite ? 'K' : 'k';

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if (game.board[row, col] == king)
                    return IsSquareAttacked(row, col, !isWhite);
            }
        }

        return true;
    }

    /// <summary>
    /// Searches for at least one legal move for the specified player.
    /// Used to distinguish checkmate from stalemate.
    /// </summary>
    public bool HasLegalMoves(bool isWhite)
    {
        for (int fromRow = 0; fromRow < 8; fromRow++)
        {
            for (int fromCol = 0; fromCol < 8; fromCol++)
            {
                char piece = game.board[fromRow, fromCol];

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
}
