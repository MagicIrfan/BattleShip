using System;
using System.Collections.Generic;

namespace BattleShip.Models.State
{
    public class GameState(
        Guid gameId,
        List<Player> players,
        int difficulty)
    {
        public Guid GameId { get; set; } = gameId;
        public List<Player> Players { get; set; } = players;

        public bool IsMultiplayer { get; set; } = false;
        public int GridSize { get; set; } = 10;
        public int Difficulty { get; set; } = difficulty;
        public List<AttackRecord> AttackHistory { get; set; } = [];

        public class AttackRecord(Position attackPosition, string playerId, bool isHit, bool isSunk)
        {
            public Position AttackPosition { get; set; } = attackPosition;
            public string PlayerId { get; set; } = playerId;
            public bool IsHit { get; set; } = isHit;
            public bool isSunk { get; set; } = isSunk;
        }
    }
}