namespace BattleShip.Models;

public class AttackRequest
{
    public Guid GameId { get; set; }
    public Position AttackPosition { get; set; }
}
