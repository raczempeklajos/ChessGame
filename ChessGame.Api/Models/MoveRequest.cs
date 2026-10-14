
using System.ComponentModel.DataAnnotations;

namespace ChessGameApi.Models;

/// <summary>
/// Represents a chess move submitted through the REST API.
/// Contains the source square, destination square,
/// and an optional pawn promotion piece.
/// </summary>
public class MoveRequest
{
    /// <summary>
    /// Gets or sets the source square of the move (e.g., "e2").
    /// Must be a valid chessboard coordinate.
    /// </summary>
    [Required]
    [RegularExpression("^[a-h][1-8]$")]
    public string From { get; set; } = "";

    /// <summary>
    /// Gets or sets the destination square of the move (e.g., "e4").
    /// Must be a valid chessboard coordinate.
    /// </summary>
    [Required]
    [RegularExpression("^[a-h][1-8]$")]
    public string To { get; set; } = "";

    /// <summary>
    /// Gets or sets the optional promotion piece.
    /// Accepted values are "q" (queen), "r" (rook),
    /// "b" (bishop), and "n" (knight).
    /// </summary>
    [RegularExpression("^[qrbn]$")]
    public string? Promotion { get; set; }
}
