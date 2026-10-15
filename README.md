# ChessGame ♟️

**A full-stack chess application built with C#, .NET 10, ASP.NET Core, React and TypeScript.**

ChessGame started as a console-based C# project focused on object-oriented programming, chess rules, game state management and artificial intelligence.

It has since evolved into a full-stack web application with an interactive React frontend, an ASP.NET Core REST API and SQLite-based game storage.

The project demonstrates software architecture, algorithm design, backend and frontend development, automated testing and deployment.

## ♟️ Live Demo

**[Play Chess Online ↗](https://chess-game-indol.vercel.app)**

Play ChessGame directly in your browser without installing additional software.

**[View Source Code ↗](https://github.com/raczempeklajos/ChessGame)**

## Features

### Chess Gameplay

- Player vs Player mode
- Player vs Computer mode
- Standard chess piece movement
- Turn-based gameplay
- Legal move validation
- Check, checkmate and stalemate detection
- Kingside and queenside castling
- En passant captures
- Pawn promotion

### Game Management

- Move history
- Undo functionality
- Captured piece tracking
- Game statistics
- Game timers
- Board customization
- Game saving and loading

Some management features belong to the original console version and may differ from those available in the web interface.

### Chess AI

The application includes a computer opponent implementing:

- Minimax search algorithm
- Alpha-Beta pruning
- Material-based position evaluation
- Piece-square tables for positional evaluation
- Basic endgame evaluation
- Move ordering using MVV-LVA (Most Valuable Victim – Least Valuable Attacker)
- Search depth configuration
- AI benchmarking and performance statistics

The AI evaluates possible moves and selects an appropriate move based on the resulting board positions.

## Technology Stack

### Frontend

- React
- TypeScript
- Vite
- CSS

### Backend

- C#
- .NET 10
- ASP.NET Core Web API
- REST API architecture
- SQLite

### Development and Testing

- Object-Oriented Programming (OOP)
- xUnit
- Git and GitHub
- GitHub Actions
- JSON serialization

### Deployment

- **Vercel:** React frontend
- **Render:** ASP.NET Core backend
- **Docker:** Backend containerization

## Project Architecture

The project separates chess logic, game state management, AI, backend services and the frontend user interface.

The original C# application follows an object-oriented architecture with dedicated classes for different responsibilities.

| Class / File | Responsibility |
|---|---|
| `Program.cs` | Console application entry point |
| `ChessGame.cs` | Game flow and move execution |
| `ChessBoard.cs` | Chessboard representation |
| `MoveValidator.cs` | Chess rules and legal move validation |
| `GameState.cs` | Game state management |
| `ChessAI.cs` | Computer opponent and position evaluation |
| `ChessGame.Api/` | ASP.NET Core REST API |
| `chessgame-frontend/` | React and TypeScript web interface |
| `ChessGame.Tests/` | Automated unit tests |

### Full-Stack Architecture

The React frontend communicates with the ASP.NET Core backend through REST API endpoints.

The backend handles chess operations and game data, while the frontend provides the interactive user interface.

The production frontend is hosted on Vercel, and API requests are forwarded to the backend hosted on Render.

## Getting Started

### Requirements

- .NET 10 SDK
- Node.js and npm (for the web frontend)
- Git
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

### Run the Console Application

```bash
dotnet run
```

### Run the Backend API

```bash
dotnet run --project ChessGame.Api/ChessGame.Api.csproj
```

### Run the React Frontend

Open another terminal:

```bash
cd chessgame-frontend
npm install
npm run dev
```

The frontend runs using Vite's local development server.

For local full-stack development, the frontend must be configured to communicate with the locally running backend API.

## How to Play — Console Version

The console application uses standard chess coordinates.

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

The project includes an xUnit test suite covering chess rules, game behavior and AI functionality.

**Recorded development milestone: 78 passing automated tests.**

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

The workflow runs on pushes to configured branches and pull requests targeting `main`.

## Hosting Notes

The frontend is deployed on Vercel, while the ASP.NET Core backend is hosted on Render's free hosting tier.

After a period of inactivity, the backend may enter sleep mode, which can cause the first request to take longer.

SQLite game data is currently stored on the backend's local filesystem. Data may be lost when the service restarts or is redeployed.

The online version is intended as a portfolio demonstration.

## Development Roadmap

### Completed Milestones

- [x] Implement chess rules and legal move validation
- [x] Implement Player vs Player gameplay
- [x] Implement Player vs Computer gameplay
- [x] Implement Minimax with Alpha-Beta pruning
- [x] Add positional evaluation and move ordering
- [x] Implement automated unit tests
- [x] Configure GitHub Actions
- [x] Create an ASP.NET Core Web API
- [x] Expose game operations through REST endpoints
- [x] Develop an interactive React chessboard
- [x] Connect the frontend to the backend API
- [x] Implement SQLite-based game storage
- [x] Containerize the backend using Docker
- [x] Deploy the frontend on Vercel
- [x] Deploy the backend on Render
- [x] Publish a playable online demo

### Future Improvements

- [ ] Add durable production database storage
- [ ] Expand integration and frontend testing
- [ ] Add FEN and PGN support
- [ ] Improve accessibility and responsive design
- [ ] Implement online multiplayer functionality
- [ ] Expand chess AI capabilities and difficulty options

## Author

**Ráczempek Lajos**

GitHub: https://github.com/raczempeklajos

## License

No license has been specified for this repository.