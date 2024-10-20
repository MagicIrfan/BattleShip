using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShip.Models.Response;

public class RollbackResponse
{
    public Guid GameId { get; set; }
    public Position LastIaAttackPosition { get; set; }
    public Position LastPlayerAttackPosition { get; set; }
    public string Message { get; set; }
}
