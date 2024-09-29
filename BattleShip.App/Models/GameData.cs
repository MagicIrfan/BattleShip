namespace BattleShip.Models;

public class GameData
{
    public Guid GameId { get; set; }
    public List<Boat>? PlayerBoats { get; set; }
}


