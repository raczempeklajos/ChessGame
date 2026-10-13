# ChessGame ♟️

A chess application developed in **C# using .NET 10**, featuring a console-based interface, complete chess rule validation, and a computer opponent powered by the Minimax algorithm.

The project demonstrates object-oriented programming, algorithm design, game state management, automated testing, and continuous integration.

**Current status:** Functional console application with chess AI. Expansion into a full-stack web application is planned.

## Features

### Chess Gameplay

- Player vs Player mode
- Player vs Computer mode
- Standard chess piece movement
- Turn-based gameplay
- Legal move validation
- Check, checkmate, and stalemate detection
- Kingside and queenside castling
- En passant captures
- Pawn promotion

### Game Management

- Move history
- Undo functionality
- Captured piece tracking
- Game statistics
- Save and load game progress using JSON

### Chess AI

The application includes a computer opponent with:

- Minimax search algorithm
- Alpha-Beta pruning
- Material-based position evaluation
- Piece-square tables for positional evaluation
- Basic endgame evaluation
- Move ordering using MVV-LVA (Most Valuable Victim – Least Valuable Attacker)
- Search depth configuration
- AI benchmarking and performance statistics

The AI evaluates possible moves and selects a move based on the resulting board positions.

## Technologies

**Current technologies:**

- C#
- .NET 10
- Object-Oriented Programming (OOP)
- xUnit
- Git and GitHub
- GitHub Actions
- JSON serialization

**Planned technologies:**

- ASP.NET Core Web API
- React
- JavaScript
- CSS
- Entity Framework Core
- SQLite

## Project Architecture

The application follows an object-oriented architecture with separate responsibilities for game logic, chess rules, AI, and state management.

| Class / File | Responsibility |
|---|---|
| `Program.cs` | Application entry point |
| `ChessGame.cs` | Game flow and move execution |
| `ChessBoard.cs` | Chessboard representation |
| `MoveValidator.cs` | Chess rules and legal move validation |
| `GameState.cs` | Game state management |
| `ChessAI.cs` | Computer opponent, Minimax search, and position evaluation |
| `ChessGame.Tests/` | Automated unit tests |

The architecture will be extended to support a REST API and a React frontend while reusing the existing chess logic.

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

The computer opponent is available through the Player vs Computer game mode.

### Available Commands

| Command | Description |
|---|---|
| `history` | Display move history |
| `undo` | Undo the previous move |
| `captured` | Display captured pieces |
| `stats` | Display game statistics |
| `benchmark` | Run chess AI performance benchmarks |
| `save` | Save game progress |
| `load` | Load a saved game |
| `new` | Start a new game |
| `exit` | Exit the application |

## Automated Testing

The project includes an xUnit test suite covering chess rules, game behavior, and AI functionality.

The latest development milestone reached **78 passing automated tests**.

Test coverage includes:

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
- Chess AI behavior

Run the tests:

```bash
dotnet test ChessGame.Tests/ChessGame.Tests.csproj
```

## Continuous Integration

The project uses GitHub Actions to automate:

1. Restoring .NET dependencies
2. Building the application
3. Executing the xUnit test suite

The workflow runs on pushes to the configured branches and pull requests targeting `main`.

## Development Roadmap

The next development phase focuses on transforming the console application into a full-stack web application.

- [x] Implement chess rules and legal move validation
- [x] Implement Player vs Player gameplay
- [x] Implement Player vs Computer gameplay
- [x] Implement Minimax with Alpha-Beta pruning
- [x] Add positional evaluation and move ordering
- [x] Implement automated unit tests
- [x] Configure GitHub Actions
- [ ] Create an ASP.NET Core Web API
- [ ] Expose game operations through REST endpoints
- [ ] Develop an interactive React chessboard
- [ ] Connect the frontend to the backend API
- [ ] Implement persistent game storage using Entity Framework Core and SQLite
- [ ] Expand integration and frontend testing
- [ ] Prepare the web application for deployment

### Potential Future Improvements

- Multiple AI difficulty levels
- FEN and PGN support
- Improved user interface and accessibility
- Online multiplayer functionality

## Author

**Ráczempek Lajos**

GitHub: https://github.com/raczempeklajos

## License

No license has been specified for this repository.