using System.Collections.Generic;

public sealed class ChessGamePersistenceState
{
    public GameState CurrentState { get; set; } = new();

    public List<GameState> UndoHistory { get; set; } = new();
}
