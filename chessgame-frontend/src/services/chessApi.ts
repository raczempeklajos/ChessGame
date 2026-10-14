
export interface GameState {
  id: string;
  board: string[];
  currentPlayer: string;
  history: string[];
  message?: string;
  captured: string[];
  status: "Playing" | "Check" | "Checkmate" | "Stalemate";
  gameOver: boolean;
  winner: string | null;
}

export interface LegalMovesResponse {
  from: string;
  legalMoves: string[];
}

const API_BASE = "/api/games";

async function readResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
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
  const response = await fetch(`/api/games/${gameId}/undo`, {
    method: "POST",
  });

  return readResponse<GameState>(response);
}

export interface AiMoveResponse extends GameState {
  aiMove: string;
}


export type AiDifficulty = "easy" | "medium" | "hard";

export interface AiMoveResponse extends GameState {
  aiMove: string;
  difficulty: AiDifficulty;
}

export async function makeAiMove(
  gameId: string,
  difficulty: AiDifficulty = "medium"
): Promise<AiMoveResponse> {
  const response = await fetch(
    `/api/games/${gameId}/ai-move?difficulty=${difficulty}`,
    {
      method: "POST",
    }
  );

  return readResponse<AiMoveResponse>(response);
}
