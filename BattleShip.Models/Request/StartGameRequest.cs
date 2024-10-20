﻿using FluentValidation;

namespace BattleShip.Models.Request;

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
            .InclusiveBetween(5, 15).WithMessage("SizeGrid must be between 5 and 15.")
            .When(x => x.SizeGrid.HasValue);
    }
}