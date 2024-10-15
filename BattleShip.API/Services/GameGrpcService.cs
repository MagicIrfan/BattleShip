using BattleShip.Grpc;
using FluentValidation;
using Grpc.Core;
using AttackRequest = BattleShip.Grpc.AttackRequest;

namespace BattleShip.API.Services;

public class GameGrpcService(IGameService gameService, IValidator<Models.AttackRequest> validator) : Grpc.GameService.GameServiceBase
{
    public override async Task<StartGameResponse> StartGame(StartGameRequest request, ServerCallContext context)
    {
        var gameId = gameService.StartGame();

        var response = new StartGameResponse
        {
            GameId = gameId.ToString(),
        };

        return await Task.FromResult(response);
    }
    
    public override async Task<AttackResponse> Attack(AttackRequest request, ServerCallContext context)
    {
        var modelRequest = new Models.AttackRequest(new Models.Position(request.AttackPosition.X, request.AttackPosition.Y))
        {
            GameId = Guid.Parse(request.GameId)
        };
        
        var (isHit, isSunk, isWinner) = await gameService.ProcessAttack(modelRequest, validator);

        return new AttackResponse 
        {
            PlayerAttackResult = isHit ? (isSunk ? "Sunk" : "Hit") : "Miss",
            IsPlayerWinner = isWinner
        };
    }
}