using BattleShip.Models;
using FluentValidation;

namespace BattleShip.API.Validator;


public class AttackRequestValidator : AbstractValidator<AttackModel.AttackRequest>
{
    public AttackRequestValidator(IGameRepository gameRepository)
    {
        RuleFor(x => x.GameId)
            .NotEmpty().WithMessage("GameId is required.")
            .NotEqual(Guid.Empty).WithMessage("GameId must be a valid GUID.");

        RuleFor(x => x.AttackPosition)
            .NotNull().WithMessage("Attack position is required.");

        RuleFor(x => x.AttackPosition.X)
            .Must((model, attackPositionX) =>
            {
                var gridSize = gameRepository.GetGame(model.GameId)?.GridSize ?? 10;
                return attackPositionX >= 0 && attackPositionX < gridSize;
            }).WithMessage("X position must be between 0 and the grid size.");

        RuleFor(x => x.AttackPosition.Y)
            .Must((model, attackPositionY) =>
            {
                var gridSize = gameRepository.GetGame(model.GameId)?.GridSize ?? 10;
                return attackPositionY >= 0 && attackPositionY < gridSize;
            }).WithMessage("Y position must be between 0 and the grid size.");
    }
}
