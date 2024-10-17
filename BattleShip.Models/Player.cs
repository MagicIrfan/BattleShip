namespace BattleShip.Models;

public class Player(
    string playerId,
    List<Boat> playerBoats,
    bool isPlayerWinner)
{
    public string PlayerId { get; set; } = playerId;
    public List<Boat> PlayerBoats { get; set; } = playerBoats;
    public bool IsPlayerWinner { get; set; } = isPlayerWinner;
}