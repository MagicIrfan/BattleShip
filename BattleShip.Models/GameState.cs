using System;
using System.Collections.Generic;

namespace BattleShip.Models
{
    public class GameState(
        Guid gameId,
        List<Boat> playerBoats,
        List<Boat> opponentBoats,
        bool isPlayerWinner,
        bool isOpponentWinner)
    {
        public Guid GameId { get; set; } = gameId;
        public List<Boat> PlayerBoats { get; set; } = playerBoats;
        public List<Boat> OpponentBoats { get; set; } = opponentBoats; // Change de ComputerBoats à OpponentBoats pour plus de clarté
        public bool IsPlayerWinner { get; set; } = isPlayerWinner;
        public bool IsOpponentWinner { get; set; } = isOpponentWinner;
        public List<AttackRecord> AttackHistory { get; set; } = [];

        public class AttackRecord(Position attackPosition, bool isPlayerAttack, bool isHit)
        {
            public Position AttackPosition { get; set; } = attackPosition;
            public bool IsPlayerAttack { get; set; } = isPlayerAttack;
            public bool IsHit { get; set; } = isHit;
        }
    }
}