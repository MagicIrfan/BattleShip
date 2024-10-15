using FluentValidation;

namespace BattleShip.Models;

public class StartGameRequest(int? sizeGrid, int difficulty)
{
    public int? SizeGrid { get; set; } = sizeGrid;
    public int Difficulty { get; set; } = difficulty;
}

public class StartGameRequestValidator : AbstractValidator<StartGameRequest>
{
    public StartGameRequestValidator()
    {
        RuleFor(x => x.Difficulty)
            .NotEmpty().WithMessage("Difficulty is required.")
            .InclusiveBetween(1, 2).WithMessage("Difficulty must be between 1 and 2.");
    }
}