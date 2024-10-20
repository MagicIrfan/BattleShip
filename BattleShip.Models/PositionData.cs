using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Models.State;

namespace BattleShip.Models;

public class PositionData
{
    public Position Position { get; set; }
    public PositionState? State { get; set; }
}
