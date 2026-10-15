export type AiDifficulty = "easy" | "medium" | "hard";
export type TimeControl = 5 | 10 | 15;

export interface GameState {
  id: string;
  board: string[];
  currentPlayer: "White" | "Black";
  history: string[];
  captured: string[];
  message?: string | null;
  status: "Playing" | "Check" | "Checkmate" | "Stalemate" | "Timeout";
  gameOver: boolean;
  winner: string | null;
  gameStarted: boolean;
  timeControlMinutes: number;
  whiteTimeMs: number;
  blackTimeMs: number;
}

export interface LegalMovesResponse {
  from: string;
  legalMoves: string[];
}

export interface AiMoveResponse extends GameState {
  aiMove: string;
  difficulty: AiDifficulty;
}

const API_BASE = "/api/games";

async function readResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const contentType = response.headers.get("content-type") ?? "";

    if (contentType.includes("application/json")) {
      const body: { message?: string } = await response.json();
      throw new Error(body.message ?? `API error: ${response.status}`);
    }

    const message = await response.text();
    throw new Error(message || `API error: ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export async function createGame(): Promise<GameState> {
  const response = await fetch(API_BASE, {
    method: "POST",
  });

  return readResponse<GameState>(response);
}

export async function getGame(gameId: string): Promise<GameState> {
  const response = await fetch(`${API_BASE}/${gameId}`);

  return readResponse<GameState>(response);
}

export async function startGame(
  gameId: string,
  minutes: TimeControl
): Promise<GameState> {
  const response = await fetch(`${API_BASE}/${gameId}/start`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ minutes }),
  });

  return readResponse<GameState>(response);
}

export async function makeMove(
  gameId: string,
  from: string,
  to: string,
  promotion?: string
): Promise<GameState> {
  const response = await fetch(`${API_BASE}/${gameId}/moves`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ from, to, promotion }),
  });

  return readResponse<GameState>(response);
}

export async function getLegalMoves(
  gameId: string,
  from: string
): Promise<LegalMovesResponse> {
  const response = await fetch(
    `${API_BASE}/${gameId}/legal-moves?from=${encodeURIComponent(from)}`
  );

  return readResponse<LegalMovesResponse>(response);
}

export async function undoMove(gameId: string): Promise<GameState> {
  const response = await fetch(`${API_BASE}/${gameId}/undo`, {
    method: "POST",
  });

  return readResponse<GameState>(response);
}

export async function makeAiMove(
  gameId: string,
  difficulty: AiDifficulty = "medium"
): Promise<AiMoveResponse> {
  const response = await fetch(
    `${API_BASE}/${gameId}/ai-move?difficulty=${difficulty}`,
    {
      method: "POST",
    }
  );

  return readResponse<AiMoveResponse>(response);
}