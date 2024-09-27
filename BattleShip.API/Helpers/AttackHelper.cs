using BattleShip.Models;

namespace BattleShip.API.Helpers;

public static class AttackHelper
{
    public static bool ProcessAttack(List<Boat> boats, Position attackPosition)
    {
        foreach (var positionHit in boats.Select(boat => boat.Positions.FirstOrDefault(p => p.X == attackPosition.X && p.Y == attackPosition.Y)).OfType<Position>())
        {
            positionHit.IsHit = true;
            return true; 
        }

        return false;
    }

    public static void UndoLastAttack(GameState gameState, GameState.AttackRecord lastAttack)
    {
        if (lastAttack.IsPlayerAttack)
        {
            var boat = gameState.ComputerBoats.FirstOrDefault(b => b.Positions.Any(p => p.X == lastAttack.AttackPosition.X && p.Y == lastAttack.AttackPosition.Y));
            var position = boat?.Positions.FirstOrDefault(p => p.X == lastAttack.AttackPosition.X && p.Y == lastAttack.AttackPosition.Y);
            if (position != null)
                position.IsHit = false;
        }
        else
        {
            var boat = gameState.PlayerBoats.FirstOrDefault(b => b.Positions.Any(p => p.X == lastAttack.AttackPosition.X && p.Y == lastAttack.AttackPosition.Y));
            var position = boat?.Positions.FirstOrDefault(p => p.X == lastAttack.AttackPosition.X && p.Y == lastAttack.AttackPosition.Y);
            if (position != null)
                position.IsHit = false;
        }
    }
}