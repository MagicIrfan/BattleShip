namespace BattleShip.Models;

public class AttackModel()
{
    public class AttackRequest(Guid gameId, Position attackPosition)
    {
        public Guid GameId { get; set; } = gameId;
        public Position AttackPosition { get; set; } = attackPosition;
    }
    
    public class AttackResponse
    {
        public bool PlayerIsHit { get; set; }
        public bool PlayerIsSunk { get; set; }
        public bool PlayerIsWinner { get; set; }
        public Position PlayerAttackPosition { get; set; }

        public bool? AiIsHit { get; set; }
        public bool? AiIsSunk { get; set; }
        public bool? AiIsWinner { get; set; }
        public Position? AiAttackPosition { get; set; }
    }

}



