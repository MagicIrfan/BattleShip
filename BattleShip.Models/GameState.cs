using System;
using System.Collections.Generic;

namespace BattleShip.Models
{
    public class GameState(
        Guid gameId,
        string playerOneId,
        string playerTwoId,
        List<Boat> playerOneBoats,
        List<Boat> playerTwoBoats,
        bool isPlayerOneWinner,
        bool isPlayerTwoWinner)
    {
        public Guid GameId { get; set; } = gameId;
        public string PlayerOneId { get; set; } = playerOneId;
        public string? PlayerTwoId { get; set; } = playerTwoId;
        public List<Boat> PlayerOneBoats { get; set; } = playerOneBoats;
        public List<Boat> PlayerTwoBoats { get; set; } = playerTwoBoats; 
        public bool IsPlayerOneWinner { get; set; } = isPlayerOneWinner;
        public bool IsPlayerTwoWinner { get; set; } = isPlayerTwoWinner;
        public List<AttackRecord> AttackHistory { get; set; } = [];

        public class AttackRecord(Position attackPosition, string playerId, bool isHit, bool isSunk)
        {
            public Position AttackPosition { get; set; } = attackPosition;
            public string PlayerId { get; set; } = playerId;
            public bool IsHit { get; set; } = isHit;
            public bool isSunk { get; set; } = isSunk;
        }
        
        public void AssignPlayer2(string player2Id)
        {
            PlayerTwoId = player2Id;
        }

        public bool IsFull() => PlayerTwoId != null;
    }
}