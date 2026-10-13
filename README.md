
# ChessGame ♟️

A console-based chess game developed in C# using .NET 10.

This project demonstrates object-oriented programming, chess rule validation, automated unit testing, and continuous integration with GitHub Actions.

## Features

- Two-player chess in the console
- Standard chess piece movement
- Turn-based gameplay
- Legal move validation
- Check, checkmate, and stalemate detection
- Kingside and queenside castling
- En passant captures
- Pawn promotion
- Move history
- Undo functionality
- Captured piece tracking
- Game statistics
- Save and load game progress using JSON

## Technologies

- C#
- .NET 10
- xUnit
- Git and GitHub
- GitHub Actions
- JSON serialization

## Project Architecture

The application follows an object-oriented structure.

| Class | Responsibility |
|---|---|
| `Program.cs` | Application entry point |
| `ChessGame.cs` | Game flow and move execution |
| `ChessBoard.cs` | Chessboard representation |
| `MoveValidator.cs` | Chess rules and move validation |
| `GameState.cs` | Game state management |

## Getting Started

### Requirements

- .NET 10 SDK
- Terminal or Visual Studio Code

### Installation

Clone the repository:

```bash
git clone https://github.com/raczempeklajos/ChessGame.git
```

Navigate to the project directory:

```bash
cd ChessGame
```

Run the application:

```bash
dotnet run
```

## How to Play

The game uses standard chess coordinates.

For example:

```text
e2 e4
e7 e5
g1 f3
```

Players take turns entering their moves.

The application validates moves according to chess rules and prevents illegal moves.

## Automated Testing

The project includes 58 automated tests using xUnit.

The tests cover:

- Chessboard operations
- Coordinate validation
- Piece movement
- Legal and illegal moves
- Turn management
- Castling
- En passant
- Pawn promotion
- Check detection
- Checkmate
- Stalemate

Run all tests:

```bash
dotnet test ChessGame.Tests/ChessGame.Tests.csproj
```

## Continuous Integration

The project uses GitHub Actions to automatically:

1. Restore .NET dependencies
2. Build the application
3. Execute the xUnit test suite

The workflow runs on pushes to the configured branches and pull requests targeting `main`.

## Future Improvements

- Improve test coverage and edge-case handling
- Implement a computer opponent using the Minimax algorithm
- Add difficulty levels
- Introduce FEN and PGN support
- Develop a graphical user interface

## Author

Ráczempek Lajos

GitHub: https://github.com/raczempeklajos

## License

No license has been specified for this repository.
