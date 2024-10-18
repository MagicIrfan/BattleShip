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
        public AttackRequestValidator(int gridSize)
        {
            RuleFor(x => x.GameId)
                .NotEmpty().WithMessage("GameId is required.")
                .NotEqual(Guid.Empty).WithMessage("GameId must be a valid GUID.");

            RuleFor(x => x.AttackPosition)
                .NotNull().WithMessage("Attack position is required.");

            RuleFor(x => x.AttackPosition!.X)
                .GreaterThanOrEqualTo(0).WithMessage("X coordinate must be greater than or equal to 0.")
                .LessThan(gridSize).WithMessage($"X coordinate must be less than {gridSize}.");

            RuleFor(x => x.AttackPosition!.Y)
                .GreaterThanOrEqualTo(0).WithMessage("Y coordinate must be greater than or equal to 0.")
                .LessThan(gridSize).WithMessage($"Y coordinate must be less than {gridSize}.");
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



