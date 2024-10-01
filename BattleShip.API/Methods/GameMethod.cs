namespace BattleShip.API.Methods;

using Services;
using Models;
using FluentValidation;

public static class GameMethods
{
    public static async Task Rollback(IGameRepository gameRepository, IGameService gameService, Guid gameId)
    {
        var gameState = await gameRepository.GetGame(gameId);
        if (gameState == null)
        {
            Results.NotFound("Game not found");
            return;
        }

        if (gameState.AttackHistory.Count == 0)
        {
            Results.BadRequest("No moves to rollback");
            return;
        }

        await gameService.RollbackTurnAsync(gameState, gameId);

        Results.Ok(new
        {
            gameState.GameId,
            Message = "Last move rolled back successfully."
        });
    }

    public static async Task<IResult> ProcessAttack(AttackRequest attackRequest, IValidator<AttackRequest> validator, IGameRepository gameRepository, IGameService gameService)
    {
        var validationResult = await validator.ValidateAsync(attackRequest);

        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var gameState = await gameRepository.GetGame(attackRequest.GameId);
        if (gameState == null)
            return Results.NotFound("Game not found");

        var playerAttackResult = gameService.ProcessAttack(gameState.OpponentBoats, attackRequest.AttackPosition);
        var attackRecord = new GameState.AttackRecord(attackRequest.AttackPosition, isPlayerAttack: true, isHit: playerAttackResult);

        gameState.AttackHistory.Add(attackRecord);
        await gameRepository.UpdateGame(gameState);

        return await gameService.UpdateGameStateAsync(playerAttackResult, gameState);
    }

    public static async Task<IResult> StartGame(IGameService gameService)
    {
        return await gameService.StartGame();
    }
}
