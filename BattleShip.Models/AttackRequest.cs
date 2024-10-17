namespace BattleShip.Models;
using FluentValidation;

public class AttackRequest(Guid gameId, Position? attackPosition)
{
    public Guid GameId { get; set; } = gameId;
    public Position? AttackPosition { get; set; } = attackPosition;
}

public class AttackRequestValidator : AbstractValidator<AttackRequest>
{
    public AttackRequestValidator()
    {
        RuleFor(x => x.GameId)
            .NotEmpty().WithMessage("GameId is required.")
            .NotEqual(Guid.Empty).WithMessage("GameId must be a valid GUID.");
    }
}

