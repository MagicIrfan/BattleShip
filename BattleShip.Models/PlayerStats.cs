using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShip.Models;

public class PlayerStats
{
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int SunkenBoats { get; set; }

    public PlayerStats()
    {
        Wins = 0;
        Losses = 0;
        SunkenBoats = 0;
    }
}
