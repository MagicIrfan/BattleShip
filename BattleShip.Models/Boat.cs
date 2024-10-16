using FluentValidation;

namespace BattleShip.Models;

public class Boat(List<Position> positions)
{
    private static readonly Dictionary<int, string> BoatNamesBySize = new()
    {
        { 5, "Porte-avions" },
        { 4, "Croiseur" },
        { 3, "Contre-torpilleur" },
        { 2, "Torpilleur" }
    };

    public string Name { get; set; } = BoatNamesBySize.GetValueOrDefault(positions.Count, "Unknown");
    public List<Position> Positions { get; set; } = positions;
}

public class BoatValidator : AbstractValidator<Boat>
{
    private static readonly Dictionary<string, int> ExpectedBoatSizes = new()
    {
        { "Porte-avions", 5 },
        { "Croiseur", 4 },
        { "Contre-torpilleur", 3 },
        { "Torpilleur", 2 }
    };

    public BoatValidator()
    {
        RuleFor(boat => boat.Name)
            .Must(name => ExpectedBoatSizes.ContainsKey(name))
            .WithMessage("Invalid boat name.");

        RuleFor(boat => boat.Positions.Count)
            .Equal(boat => ExpectedBoatSizes[boat.Name])
            .WithMessage(boat => $"Boat {boat.Name} must have a size of {ExpectedBoatSizes[boat.Name]}.");

        RuleFor(boat => boat.Positions)
            .Must(BeAlignedStraight)
            .WithMessage("Boat positions must be aligned in a straight line.");
    }

    private bool BeAlignedStraight(List<Position> positions)
    {
        if (positions.Count < 2) return true;
        
        var isVertical = positions[0].X == positions[1].X;
        for (var i = 1; i < positions.Count; i++)
        {
            if (isVertical)
            {
                if (positions[i].X != positions[0].X) return false;
            }
            else
            {
                if (positions[i].Y != positions[0].Y) return false;
            }
        }
        return true;
    }
}