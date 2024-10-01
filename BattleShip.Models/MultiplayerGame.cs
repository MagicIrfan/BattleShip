namespace BattleShip.Models;

public class MultiplayerGame(string player1Id, string player1Name, GameState gameState)
{
    public string Player1Id { get; set; } = player1Id;
    public string Player1Name { get; set; } = player1Name;
    public string? Player2Id { get; private set; }
    public string? Player2Name { get; private set; }

    public GameState State { get; set; } = gameState;

    public void AssignPlayer2(string? player2Id, string player2Name)
    {
        Player2Id = player2Id;
        Player2Name = player2Name;
        State.OpponentBoats = []; 
    }

    public bool IsFull() => Player2Id != null;
}