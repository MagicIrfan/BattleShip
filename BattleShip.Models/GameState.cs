using System;
using System.Collections.Generic;

namespace BattleShip.Models
{
    public class GameState(
        Guid gameId,
        List<Boat> playerBoats,
        List<Boat> computerBoats,
        bool isPlayerWinner,
        bool isComputerWinner)
    {
        public Guid GameId { get; set; } = gameId;
        public List<Boat> PlayerBoats { get; set; } = playerBoats;
        public List<Boat> ComputerBoats { get; set; } = computerBoats;
        public bool IsPlayerWinner { get; set; } = isPlayerWinner;
        public bool IsComputerWinner { get; set; } = isComputerWinner;
    }
}