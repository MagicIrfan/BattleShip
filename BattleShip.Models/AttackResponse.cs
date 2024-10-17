using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShip.Models;

public class AttackResponse
{
    public bool PlayerIsHit { get; set; }
    public bool PlayerIsSunk { get; set; }
    public bool PlayerIsWinner { get; set; }
    public Position PlayerAttackPosition { get; set; }
    public bool AiIsHit { get; set; }
    public bool AiIsSunk { get; set; }
    public bool AiIsWinner { get; set; }
    public Position AiAttackPosition { get; set; }
}
