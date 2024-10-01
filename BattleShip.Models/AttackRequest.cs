﻿namespace BattleShip.Models;
using FluentValidation;

public abstract class AttackRequest(Position attackPosition)
{
    public Guid GameId { get; set; }
    public bool IsMultiplayer { get; set; }
    public Position AttackPosition { get; set; } = attackPosition;
}

public abstract class AttackRequestValidator : AbstractValidator<AttackRequest>
{
    protected AttackRequestValidator()
    {
        RuleFor(x => x.GameId)
            .NotEmpty().WithMessage("GameId is required.")
            .NotEqual(Guid.Empty).WithMessage("GameId must be a valid GUID.");

        RuleFor(x => x.IsMultiplayer)
            .NotNull().WithMessage("IsMultiplayer is required.");

        RuleFor(x => x.AttackPosition)
            .NotNull().WithMessage("Attack position is required.");

        RuleFor(x => x.AttackPosition.X)
            .InclusiveBetween(0, 9).WithMessage("X coordinate must be between 0 and 9.");

        RuleFor(x => x.AttackPosition.Y)
            .InclusiveBetween(0, 9).WithMessage("Y coordinate must be between 0 and 9.");
    }
}

