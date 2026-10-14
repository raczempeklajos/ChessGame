
import { useEffect, useState } from "react";
import {
  createGame,
  getLegalMoves,
  makeMove,
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

function toCoordinate(row: number, col: number): string {
  return `${String.fromCharCode(97 + col)}${8 - row}`;
}

export default function ChessBoard() {
  const [game, setGame] = useState<GameState | null>(null);
  const [selectedSquare, setSelectedSquare] =
    useState<SelectedSquare | null>(null);
  const [legalMoves, setLegalMoves] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [moving, setMoving] = useState(false);
  const [loadingMoves, setLoadingMoves] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;

    async function startGame() {
      try {
        const newGame = await createGame();

        if (active) {
          setGame(newGame);
          setError("");
        }
      } catch (err) {
        if (active) {
          setError(
            err instanceof Error ? err.message : "Failed to start game."
          );
        }
      } finally {
        if (active) setLoading(false);
      }
    }

    void startGame();

    return () => {
      active = false;
    };
  }, []);

  async function handleNewGame() {
    if (loading || moving || loadingMoves) return;

    setLoading(true);
    setError("");
    setSelectedSquare(null);
    setLegalMoves([]);

    try {
      const newGame = await createGame();
      setGame(newGame);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Failed to create a new game."
      );
    } finally {
      setLoading(false);
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
      setSelectedSquare(null);
      setError(
        err instanceof Error
          ? err.message
          : "Failed to load legal moves."
      );
    } finally {
      setLoadingMoves(false);
    }
  }

  async function handleSquareClick(row: number, col: number) {
    if (!game || moving || loading || loadingMoves) return;

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
      setSelectedSquare(null);
      setLegalMoves([]);
      return;
    }

    if (isOwnPiece) {
      await selectPiece(game, row, col);
      return;
    }

    if (!legalMoves.includes(coordinate)) {
      return;
    }

    const from = toCoordinate(
      selectedSquare.row,
      selectedSquare.col
    );

    setMoving(true);
    setError("");

    try {
      const updatedGame = await makeMove(
        game.id,
        from,
        coordinate
      );

      setGame(updatedGame);
      setSelectedSquare(null);
      setLegalMoves([]);
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Invalid move."
      );
      setSelectedSquare(null);
      setLegalMoves([]);
    } finally {
      setMoving(false);
    }
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

  return (
    <div className="chess-game">
      <div className="game-status">
        <span>
          Current player:{" "}
          <strong>{game.currentPlayer}</strong>
        </span>

        <button
          type="button"
          className="new-game-button"
          onClick={() => void handleNewGame()}
          disabled={loading || moving || loadingMoves}
        >
          {loading ? "Starting..." : "New Game"}
        </button>
      </div>

      <div
        className="chess-board"
        role="group"
        aria-label="Chess board"
      >
        {game.board.map((boardRow, rowIndex) =>
          boardRow.split("").map((piece, colIndex) => {
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

            return (
              <button
                type="button"
                key={`${rowIndex}-${colIndex}`}
                className={[
                  "square",
                  isLight ? "light" : "dark",
                  isSelected ? "selected" : "",
                  isLegalMove ? "legal-move" : "",
                  isLegalMove && piece !== "."
                    ? "legal-capture"
                    : "",
                ].join(" ")}
                onClick={() =>
                  void handleSquareClick(
                    rowIndex,
                    colIndex
                  )
                }
                disabled={moving || loading || loadingMoves}
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

      {error && (
        <p className="game-error" role="alert">
          {error}
        </p>
      )}

      <div className="move-history">
        <h3>Move history</h3>

        {game.history.length === 0 ? (
          <p>No moves yet.</p>
        ) : (
          <ol>
            {game.history.map((move, index) => (
              <li key={`${index}-${move}`}>
                {move}
              </li>
            ))}
          </ol>
        )}
      </div>
    </div>
  );
}
