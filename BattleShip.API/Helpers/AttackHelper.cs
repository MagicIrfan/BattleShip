using BattleShip.Models;
using BattleShip.Models.State;

namespace BattleShip.API.Helpers;

public static class AttackHelper
{
    public static (bool isHit, bool isSunk, List<Boat> updatedBoats) ProcessAttack(List<Boat> boats,
        Position attackPosition)
    {
        var isHit = false;
        Boat? hitBoat = null;

        foreach (var boat in boats)
        {
            var position = boat.Positions.FirstOrDefault(p => p.X == attackPosition.X && p.Y == attackPosition.Y);
            if (position is { IsHit: false })
            {
                position.IsHit = true;
                isHit = true;
                hitBoat = boat;
                break;
            }
        }

        var isSunk = hitBoat != null && hitBoat.Positions.All(p => p.IsHit);

        return (isHit, isSunk, boats);
    }


    public static void UndoLastAttack(List<Boat> boats, GameState.AttackRecord lastAttack)
    {
        var boat = boats.FirstOrDefault(b =>
            b.Positions.Any(p => p.X == lastAttack.AttackPosition.X && p.Y == lastAttack.AttackPosition.Y));
        var position =
            boat?.Positions.FirstOrDefault(
                p => p.X == lastAttack.AttackPosition.X && p.Y == lastAttack.AttackPosition.Y);
        if (position != null)
            position.IsHit = false;
    }
}