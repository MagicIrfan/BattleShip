﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShip.Models;

public class PositionData
{
    public Position Position { get; set; }
    public PositionState? State { get; set; }
}
