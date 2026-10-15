namespace ChessGameApi.Models;

public sealed class SavedGameSession
{
    public ChessGamePersistenceState Game { get; set; } = new();

    public GameClockPersistenceState Clock { get; set; } = new();
}
