
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
    /// Gets or sets the source square of the move.
    /// </summary>
    [Required]
    [RegularExpression("^[a-h][1-8]$")]
    public string From { get; set; } = "";

    /// <summary>
    /// Gets or sets the destination square of the move.
    /// </summary>
    [Required]
    [RegularExpression("^[a-h][1-8]$")]
    public string To { get; set; } = "";

    /// <summary>
    /// Gets or sets the optional promotion piece.
    /// Accepts queen, rook, bishop, or knight.
    /// </summary>
    [RegularExpression("^[qrbnQRBN]$")]
    public string? Promotion { get; set; }
}
