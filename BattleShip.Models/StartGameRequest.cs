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
            .InclusiveBetween(1, 3).WithMessage("Difficulty must be between 1 and 3.");
        RuleFor(x => x.SizeGrid)
            .InclusiveBetween(5, 20).WithMessage("SizeGrid must be between 5 and 20.")
            .When(x => x.SizeGrid.HasValue);
    }
}