import { useEffect, useRef, useState } from "react";
import {
  createGame,
  getGame,
  getLegalMoves,
  makeAiMove,
  makeMove,
  startGame,
  undoMove,
  type AiDifficulty,
  type GameState,
  type TimeControl,
} from "../services/chessApi";
import "./ChessBoard.css";

const pieceSymbols: Record<string, string> = {
  K: "♔",
  Q: "♕",
  R: "♖",
  B: "♗",
  N: "♘",
  P: "♙",
  k: "♚",
  q: "♛",
  r: "♜",
  b: "♝",
  n: "♞",
  p: "♟",
};

type SelectedSquare = {
  row: number;
  col: number;
};

type PendingPromotion = {
  from: string;
  to: string;
  isWhite: boolean;
};

type GameMode = "pvp" | "computer";
type PlayerColor = "White" | "Black";

const promotionPieces = [
  { value: "q", name: "Queen" },
  { value: "r", name: "Rook" },
  { value: "b", name: "Bishop" },
  { value: "n", name: "Knight" },
];

function toCoordinate(row: number, col: number): string {
  return `${String.fromCharCode(97 + col)}${8 - row}`;
}

function errorMessage(error: unknown, fallback: string): string {
  return error instanceof Error ? error.message : fallback;
}

function formatTime(milliseconds: number): string {
  const seconds = Math.ceil(Math.max(0, milliseconds) / 1000);
  const minutes = Math.floor(seconds / 60);

  return `${String(minutes).padStart(2, "0")}:${String(
    seconds % 60
  ).padStart(2, "0")}`;
}

export default function ChessBoard() {
  const [game, setGame] = useState<GameState | null>(null);
  const [gameMode, setGameMode] = useState<GameMode>("pvp");
  const [difficulty, setDifficulty] = useState<AiDifficulty>("medium");
  const [timeControl, setTimeControl] = useState<TimeControl>(10);

  const [flipped, setFlipped] = useState(false);
  const [selectedSquare, setSelectedSquare] =
    useState<SelectedSquare | null>(null);
  const [pendingPromotion, setPendingPromotion] =
    useState<PendingPromotion | null>(null);
  const [legalMoves, setLegalMoves] = useState<string[]>([]);

  const [loading, setLoading] = useState(true);
  const [moving, setMoving] = useState(false);
  const [loadingMoves, setLoadingMoves] = useState(false);
  const [thinking, setThinking] = useState(false);
  const [error, setError] = useState("");

  const busyRef = useRef(false);
  const generationRef = useRef(0);
  const gameRef = useRef<GameState | null>(null);
  const pollingRef = useRef(false);

  function updateGame(next: GameState) {
    gameRef.current = next;
    setGame(next);
  }

  function clearSelection() {
    setSelectedSquare(null);
    setLegalMoves([]);
    setPendingPromotion(null);
  }

  useEffect(() => {
    const generation = ++generationRef.current;

    async function loadInitialGame() {
      try {
        const created = await createGame();

        if (generation === generationRef.current) {
          updateGame(created);
          setError("");
        }
      } catch (err) {
        if (generation === generationRef.current) {
          setError(errorMessage(err, "Failed to create game."));
        }
      } finally {
        if (generation === generationRef.current) {
          setLoading(false);
        }
      }
    }

    void loadInitialGame();

    return () => {
      generationRef.current++;
    };
  }, []);

  // The server is the authoritative source for clock values.
  useEffect(() => {
    const interval = window.setInterval(async () => {
      const current = gameRef.current;

      if (
        !current ||
        !current.gameStarted ||
        current.gameOver ||
        busyRef.current ||
        pollingRef.current
      ) {
        return;
      }

      pollingRef.current = true;
      const generation = generationRef.current;

      try {
        const updated = await getGame(current.id);

        if (
          generation === generationRef.current &&
          gameRef.current?.id === current.id &&
          !busyRef.current
        ) {
          updateGame(updated);

          if (updated.gameOver) {
            clearSelection();
          }
        }
      } catch {
        // A temporary polling error must not interrupt the game.
      } finally {
        pollingRef.current = false;
      }
    }, 1000);

    return () => window.clearInterval(interval);
  }, []);

  const busy = loading || moving || loadingMoves || thinking;
  const gameStarted = game?.gameStarted ?? false;
  const gameFinished = game?.gameOver ?? false;

  async function handleStartGame() {
    if (
      !game ||
      gameStarted ||
      gameFinished ||
      busy ||
      busyRef.current
    ) {
      return;
    }

    busyRef.current = true;
    setMoving(true);
    setError("");
    clearSelection();

    try {
      const updated = await startGame(game.id, timeControl);
      updateGame(updated);
    } catch (err) {
      setError(errorMessage(err, "Failed to start game."));
    } finally {
      setMoving(false);
      busyRef.current = false;
    }
  }

  async function handleNewGame() {
    if (busyRef.current || busy) return;

    busyRef.current = true;
    generationRef.current++;

    setLoading(true);
    setError("");
    clearSelection();

    try {
      const created = await createGame();

      updateGame(created);
      setFlipped(false);
    } catch (err) {
      setError(errorMessage(err, "Failed to create a new game."));
    } finally {
      setLoading(false);
      busyRef.current = false;
    }
  }

  async function handleUndo() {
    if (
      !game ||
      !gameStarted ||
      game.history.length === 0 ||
      busy ||
      busyRef.current ||
      pendingPromotion
    ) {
      return;
    }

    busyRef.current = true;
    setMoving(true);
    setError("");
    clearSelection();

    try {
      let updated = await undoMove(game.id);

      // In computer mode, undo both the AI move and the
      // preceding player move when possible.
      if (
        gameMode === "computer" &&
        updated.history.length > 0 &&
        updated.currentPlayer === "Black"
      ) {
        updated = await undoMove(game.id);
      }

      updateGame(updated);
    } catch (err) {
      setError(errorMessage(err, "Failed to undo move."));

      try {
        updateGame(await getGame(game.id));
      } catch {
        // Preserve the last known state if refreshing fails.
      }
    } finally {
      setMoving(false);
      busyRef.current = false;
    }
  }

  async function selectPiece(
    currentGame: GameState,
    row: number,
    col: number
  ) {
    const from = toCoordinate(row, col);

    setSelectedSquare({ row, col });
    setLegalMoves([]);
    setError("");
    setLoadingMoves(true);

    try {
      const result = await getLegalMoves(currentGame.id, from);

      if (gameRef.current?.id === currentGame.id) {
        setLegalMoves(result.legalMoves);
      }
    } catch (err) {
      clearSelection();
      setError(errorMessage(err, "Failed to load legal moves."));
    } finally {
      setLoadingMoves(false);
    }
  }

  async function submitPlayerMove(
    currentGame: GameState,
    from: string,
    to: string,
    promotion?: string
  ) {
    if (busyRef.current || currentGame.gameOver) return;

    busyRef.current = true;
    setMoving(true);
    setError("");

    try {
      const updated = await makeMove(
        currentGame.id,
        from,
        to,
        promotion
      );

      updateGame(updated);
      clearSelection();

      if (
        gameMode === "computer" &&
        !updated.gameOver &&
        updated.currentPlayer === "Black"
      ) {
        setThinking(true);

        try {
          const aiResult = await makeAiMove(
            updated.id,
            difficulty
          );

          updateGame(aiResult);
        } catch (err) {
          setError(
            errorMessage(
              err,
              "The computer could not make a move."
            )
          );

          // Refresh the game in case the AI ran out of time.
          try {
            updateGame(await getGame(updated.id));
          } catch {
            // Preserve the last known game state.
          }
        } finally {
          setThinking(false);
        }
      }
    } catch (err) {
      setError(errorMessage(err, "Invalid move."));
      clearSelection();

      // A rejected move may mean the clock expired.
      try {
        updateGame(await getGame(currentGame.id));
      } catch {
        // Preserve the last known game state.
      }
    } finally {
      setMoving(false);
      busyRef.current = false;
    }
  }

  async function handleSquareClick(row: number, col: number) {
    if (
      !game ||
      !gameStarted ||
      gameFinished ||
      pendingPromotion ||
      busy ||
      busyRef.current ||
      (gameMode === "computer" &&
        game.currentPlayer === "Black")
    ) {
      return;
    }

    const piece = game.board[row][col];
    const coordinate = toCoordinate(row, col);
    const isWhiteTurn = game.currentPlayer === "White";

    const isOwnPiece =
      piece !== "." &&
      (piece === piece.toUpperCase()) === isWhiteTurn;

    if (selectedSquare === null) {
      if (isOwnPiece) {
        await selectPiece(game, row, col);
      }

      return;
    }

    if (
      selectedSquare.row === row &&
      selectedSquare.col === col
    ) {
      clearSelection();
      return;
    }

    if (isOwnPiece) {
      await selectPiece(game, row, col);
      return;
    }

    if (!legalMoves.includes(coordinate)) return;

    const from = toCoordinate(
      selectedSquare.row,
      selectedSquare.col
    );

    const movingPiece =
      game.board[selectedSquare.row][selectedSquare.col];

    const isPawn =
      movingPiece === "P" || movingPiece === "p";

    const reachesLastRank =
      (isWhiteTurn && row === 0) ||
      (!isWhiteTurn && row === 7);

    if (isPawn && reachesLastRank) {
      setPendingPromotion({
        from,
        to: coordinate,
        isWhite: isWhiteTurn,
      });

      return;
    }

    await submitPlayerMove(game, from, coordinate);
  }

  async function handlePromotion(piece: string) {
    if (
      !game ||
      !pendingPromotion ||
      busyRef.current ||
      game.gameOver
    ) {
      return;
    }

    const promotion = pendingPromotion.isWhite
      ? piece.toUpperCase()
      : piece;

    await submitPlayerMove(
      game,
      pendingPromotion.from,
      pendingPromotion.to,
      promotion
    );
  }

  if (loading && !game) {
    return <p>Loading chess game...</p>;
  }

  if (!game) {
    return (
      <p role="alert">
        {error || "Unable to load game."}
      </p>
    );
  }

  const lastMove = game.history.at(-1);

  const lastMoveSquares = lastMove
    ? lastMove.match(/[a-h][1-8]/g)
    : null;

  const capturedPieces = game.captured ?? [];

  const whiteCaptured = capturedPieces.filter(
    (piece) => piece === piece.toLowerCase()
  );

  const blackCaptured = capturedPieces.filter(
    (piece) => piece === piece.toUpperCase()
  );

  const clocks: Record<PlayerColor, number> = {
    White: gameStarted
      ? game.whiteTimeMs
      : timeControl * 60_000,
    Black: gameStarted
      ? game.blackTimeMs
      : timeControl * 60_000,
  };

  const boardDisabled =
    !gameStarted ||
    gameFinished ||
    pendingPromotion !== null ||
    busy ||
    (gameMode === "computer" &&
      game.currentPlayer === "Black");

  return (
    <div className="chess-game">
      {/* Game mode selection */}
      <div className="game-mode-selector">
        <span>Game mode:</span>

        <button
          type="button"
          className={gameMode === "pvp" ? "mode-active" : ""}
          onClick={() => setGameMode("pvp")}
          disabled={busy || gameStarted}
        >
          Player vs Player
        </button>

        <button
          type="button"
          className={
            gameMode === "computer" ? "mode-active" : ""
          }
          onClick={() => setGameMode("computer")}
          disabled={busy || gameStarted}
        >
          Player vs Computer
        </button>
      </div>

      {/* Computer difficulty */}
      {gameMode === "computer" && (
        <div className="game-mode-selector">
          <span>Computer difficulty:</span>

          {(["easy", "medium", "hard"] as AiDifficulty[]).map(
            (level) => (
              <button
                key={level}
                type="button"
                className={
                  difficulty === level ? "mode-active" : ""
                }
                onClick={() => setDifficulty(level)}
                disabled={busy || gameStarted}
              >
                {level.charAt(0).toUpperCase() +
                  level.slice(1)}
              </button>
            )
          )}
        </div>
      )}

      {/* Time control */}
      <div className="game-mode-selector">
        <span>Time control:</span>

        {([5, 10, 15] as TimeControl[]).map(
          (minutes) => (
            <button
              key={minutes}
              type="button"
              className={
                timeControl === minutes ? "mode-active" : ""
              }
              onClick={() => setTimeControl(minutes)}
              disabled={busy || gameStarted}
              title="Choose time control before starting"
            >
              {minutes} min
            </button>
          )
        )}
      </div>

      {/* Server-controlled chess clocks */}
      <div className="chess-clocks" aria-label="Chess clocks">
        {(["White", "Black"] as PlayerColor[]).map(
          (player) => {
            const isActive =
              gameStarted &&
              !gameFinished &&
              !loading &&
              game.currentPlayer === player;

            return (
              <div
                key={player}
                className={[
                  "chess-clock",
                  isActive ? "clock-active" : "",
                  clocks[player] <= 30000
                    ? "clock-low"
                    : "",
                ].join(" ")}
              >
                <span className="clock-player">
                  {player}
                </span>

                <strong className="clock-time">
                  {formatTime(clocks[player])}
                </strong>
              </div>
            );
          }
        )}
      </div>

      {/* Pawn promotion */}
      {pendingPromotion && (
        <div className="promotion-overlay">
          <div
            className="promotion-dialog"
            role="dialog"
            aria-modal="true"
            aria-label="Choose promotion piece"
          >
            <h3>Choose promotion piece</h3>

            <div className="promotion-options">
              {promotionPieces.map((piece) => {
                const symbolKey = pendingPromotion.isWhite
                  ? piece.value.toUpperCase()
                  : piece.value;

                return (
                  <button
                    type="button"
                    key={piece.value}
                    onClick={() =>
                      void handlePromotion(piece.value)
                    }
                    disabled={busy}
                    aria-label={piece.name}
                    title={piece.name}
                  >
                    {pieceSymbols[symbolKey]}
                  </button>
                );
              })}
            </div>
          </div>
        </div>
      )}

      {/* Game status */}
      {game.status === "Timeout" && (
        <div
          className="game-alert game-over-alert"
          role="status"
        >
          Time is up! {game.winner} wins on time.
        </div>
      )}

      {game.status === "Check" && (
        <div
          className="game-alert check-alert"
          role="status"
        >
          Check! {game.currentPlayer} king is under attack.
        </div>
      )}

      {game.status === "Checkmate" && (
        <div
          className="game-alert game-over-alert"
          role="status"
        >
          Checkmate! {game.winner} wins the game.
        </div>
      )}

      {game.status === "Stalemate" && (
        <div
          className="game-alert game-over-alert"
          role="status"
        >
          Stalemate! The game ends in a draw.
        </div>
      )}

      {thinking && !gameFinished && (
        <div className="game-alert" role="status">
          Computer is thinking...
        </div>
      )}

      {/* Start Game */}
      {!gameStarted && !gameFinished && (
        <div className="game-status">
          <button
            type="button"
            className="new-game-button"
            onClick={() => void handleStartGame()}
            disabled={busy}
          >
            Start Game
          </button>
        </div>
      )}

      {/* Game controls */}
      <div className="game-status">
        <span>
          Current player:{" "}
          <strong>{game.currentPlayer}</strong>
        </span>

        <button
          type="button"
          className="new-game-button"
          onClick={() => void handleNewGame()}
          disabled={busy}
        >
          {loading ? "Starting..." : "New Game"}
        </button>

        <button
          type="button"
          className="undo-button"
          onClick={() => void handleUndo()}
          disabled={
            !gameStarted ||
            game.history.length === 0 ||
            busy ||
            pendingPromotion !== null
          }
        >
          Undo Move
        </button>

        <button
          type="button"
          className="flip-button"
          onClick={() =>
            setFlipped((previous) => !previous)
          }
          disabled={busy || pendingPromotion !== null}
        >
          Flip Board
        </button>
      </div>

      {/* Chess board */}
      <div
        className="chess-board"
        role="group"
        aria-label="Chess board"
      >
        {(flipped
          ? [...game.board].reverse()
          : game.board
        ).map((boardRow, displayRow) =>
          (flipped
            ? boardRow.split("").reverse()
            : boardRow.split("")
          ).map((piece, displayCol) => {
            const rowIndex = flipped
              ? 7 - displayRow
              : displayRow;

            const colIndex = flipped
              ? 7 - displayCol
              : displayCol;

            const isLight =
              (rowIndex + colIndex) % 2 === 0;

            const isSelected =
              selectedSquare?.row === rowIndex &&
              selectedSquare?.col === colIndex;

            const coordinate = toCoordinate(
              rowIndex,
              colIndex
            );

            const isLegalMove =
              legalMoves.includes(coordinate);

            const isLastMove =
              lastMoveSquares?.includes(coordinate) ?? false;

            return (
              <button
                type="button"
                key={`${rowIndex}-${colIndex}`}
                className={[
                  "square",
                  isLight ? "light" : "dark",
                  isLastMove ? "last-move" : "",
                  isSelected ? "selected" : "",
                  isLegalMove ? "legal-move" : "",
                  isLegalMove && piece !== "."
                    ? "legal-capture"
                    : "",
                ].join(" ")}
                onClick={() =>
                  void handleSquareClick(rowIndex, colIndex)
                }
                disabled={boardDisabled}
                aria-label={`${coordinate}: ${
                  piece === "." ? "empty" : piece
                }`}
                aria-pressed={isSelected}
              >
                {pieceSymbols[piece] ?? ""}
              </button>
            );
          })
        )}
      </div>

      {/* Captured pieces */}
      <div
        className="captured-pieces"
        aria-label="Captured pieces"
      >
        <div className="captured-group">
          <h3>White captured</h3>

          <div
            className="captured-symbols"
            aria-label="Pieces captured by White"
          >
            {whiteCaptured.length === 0 ? (
              <span className="captured-empty">
                None yet
              </span>
            ) : (
              whiteCaptured.map((piece, index) => (
                <span
                  key={`white-${index}`}
                  aria-label={piece}
                >
                  {pieceSymbols[piece]}
                </span>
              ))
            )}
          </div>
        </div>

        <div className="captured-group">
          <h3>Black captured</h3>

          <div
            className="captured-symbols"
            aria-label="Pieces captured by Black"
          >
            {blackCaptured.length === 0 ? (
              <span className="captured-empty">
                None yet
              </span>
            ) : (
              blackCaptured.map((piece, index) => (
                <span
                  key={`black-${index}`}
                  aria-label={piece}
                >
                  {pieceSymbols[piece]}
                </span>
              ))
            )}
          </div>
        </div>
      </div>

      {/* Errors */}
      {error && (
        <p className="game-error" role="alert">
          {error}
        </p>
      )}

      {/* Move history */}
      <div className="move-history">
        <h3>Move history</h3>

        {game.history.length === 0 ? (
          <p>No moves yet.</p>
        ) : (
          <div className="move-history-scroll">
            <table className="move-history-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>White</th>
                  <th>Black</th>
                </tr>
              </thead>

              <tbody>
                {Array.from(
                  {
                    length: Math.ceil(
                      game.history.length / 2
                    ),
                  },
                  (_, index) => (
                    <tr key={index}>
                      <td>{index + 1}.</td>
                      <td>{game.history[index * 2]}</td>
                      <td>
                        {game.history[index * 2 + 1] ?? ""}
                      </td>
                    </tr>
                  )
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}