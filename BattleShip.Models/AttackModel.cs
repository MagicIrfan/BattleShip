namespace BattleShip.Models;
using FluentValidation;

public class AttackModel()
{
    public class AttackRequest(Guid gameId, Position attackPosition)
    {
        public Guid GameId { get; set; } = gameId;
        public Position AttackPosition { get; set; } = attackPosition;
    }

    public class AttackRequestValidator : AbstractValidator<AttackRequest>
    {
        public AttackRequestValidator()
        {
            RuleFor(x => x.GameId)
                .NotEmpty().WithMessage("GameId is required.")
                .NotEqual(Guid.Empty).WithMessage("GameId must be a valid GUID.");
            RuleFor(x => x.AttackPosition)
                .NotNull().WithMessage("Attack position is required.");

            RuleFor(x => x.AttackPosition!.X)
                .InclusiveBetween(0, 9).WithMessage("X coordinate must be between 0 and 9.");

            RuleFor(x => x.AttackPosition!.Y)
                .InclusiveBetween(0, 9).WithMessage("Y coordinate must be between 0 and 9.");
        }
    }
    
    public class AttackResponse
    {
        public bool PlayerIsHit { get; set; }
        public bool PlayerIsSunk { get; set; }
        public bool PlayerIsWinner { get; set; }
        public Position PlayerAttackPosition { get; set; }

        public bool? AiIsHit { get; set; }
        public bool? AiIsSunk { get; set; }
        public bool? AiIsWinner { get; set; }
        public Position? AiAttackPosition { get; set; }
    }

}



