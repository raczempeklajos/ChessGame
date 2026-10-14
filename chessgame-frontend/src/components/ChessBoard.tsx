
import { useEffect, useRef, useState } from "react";
import {
  createGame,
  getLegalMoves,
  makeAiMove,
  makeMove,
  undoMove,
  type AiDifficulty,
  type GameState,
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

export default function ChessBoard() {
  const [game, setGame] = useState<GameState | null>(null);
  const [gameMode, setGameMode] = useState<GameMode>("pvp");
  const [difficulty, setDifficulty] = useState<AiDifficulty>("medium");
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

  useEffect(() => {
    const generation = ++generationRef.current;

    async function startGame() {
      try {
        const newGame = await createGame();

        if (generation === generationRef.current) {
          setGame(newGame);
          setError("");
        }
      } catch (err) {
        if (generation === generationRef.current) {
          setError(errorMessage(err, "Failed to start game."));
        }
      } finally {
        if (generation === generationRef.current) {
          setLoading(false);
        }
      }
    }

    void startGame();

    return () => {
      generationRef.current++;
    };
  }, []);

  const busy = loading || moving || loadingMoves || thinking;

  function clearSelection() {
    setSelectedSquare(null);
    setLegalMoves([]);
    setPendingPromotion(null);
  }

  async function startNewGame(mode: GameMode) {
    if (busyRef.current || busy) return;

    busyRef.current = true;
    setLoading(true);
    setError("");
    clearSelection();

    try {
      const newGame = await createGame();

      setGame(newGame);
      setGameMode(mode);
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
      game.history.length === 0 ||
      busyRef.current ||
      busy ||
      pendingPromotion
    ) {
      return;
    }

    busyRef.current = true;
    setMoving(true);
    setError("");
    clearSelection();

    try {
      let updatedGame = await undoMove(game.id);

      // Undo both moves in computer mode when possible.
      if (
        gameMode === "computer" &&
        updatedGame.history.length > 0 &&
        updatedGame.currentPlayer === "Black"
      ) {
        updatedGame = await undoMove(game.id);
      }

      setGame(updatedGame);
    } catch (err) {
      setError(errorMessage(err, "Failed to undo the last move."));
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
      setLegalMoves(result.legalMoves);
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
    if (busyRef.current) return;

    busyRef.current = true;
    setMoving(true);
    setError("");

    try {
      const updatedGame = await makeMove(
        currentGame.id,
        from,
        to,
        promotion
      );

      setGame(updatedGame);
      clearSelection();

      if (
        gameMode === "computer" &&
        !updatedGame.gameOver &&
        updatedGame.currentPlayer === "Black"
      ) {
        setThinking(true);

        try {
          // Send the selected difficulty to the backend.
          const aiGame = await makeAiMove(updatedGame.id, difficulty);
          setGame(aiGame);
        } catch (err) {
          setError(
            errorMessage(err, "The computer could not make a move.")
          );
        } finally {
          setThinking(false);
        }
      }
    } catch (err) {
      setError(errorMessage(err, "Invalid move."));
      clearSelection();
    } finally {
      setMoving(false);
      busyRef.current = false;
    }
  }

  async function handleSquareClick(row: number, col: number) {
    if (
      !game ||
      game.gameOver ||
      pendingPromotion ||
      busy ||
      busyRef.current ||
      (gameMode === "computer" && game.currentPlayer === "Black")
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
    if (!game || !pendingPromotion || busyRef.current) return;

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

  const boardDisabled =
    game.gameOver ||
    pendingPromotion !== null ||
    busy ||
    (gameMode === "computer" && game.currentPlayer === "Black");

  return (
    <div className="chess-game">
      {/* Game mode selection */}
      <div className="game-mode-selector">
        <span>Game mode:</span>

        <button
          type="button"
          className={gameMode === "pvp" ? "mode-active" : ""}
          onClick={() => void startNewGame("pvp")}
          disabled={busy}
        >
          Player vs Player
        </button>

        <button
          type="button"
          className={gameMode === "computer" ? "mode-active" : ""}
          onClick={() => void startNewGame("computer")}
          disabled={busy}
        >
          Player vs Computer
        </button>
      </div>

      {/* AI difficulty selector */}
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
                disabled={busy}
              >
                {level.charAt(0).toUpperCase() + level.slice(1)}
              </button>
            )
          )}
        </div>
      )}

      {/* Pawn promotion dialog */}
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

      {thinking && (
        <div className="game-alert" role="status">
          Computer is thinking...
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
          onClick={() => void startNewGame(gameMode)}
          disabled={busy}
        >
          {loading ? "Starting..." : "New Game"}
        </button>

        <button
          type="button"
          className="undo-button"
          onClick={() => void handleUndo()}
          disabled={
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

      {/* Error messages */}
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
