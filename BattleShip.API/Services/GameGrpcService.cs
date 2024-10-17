using BattleShip.Grpc;
using FluentValidation;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace BattleShip.API.Services;

public class GameGrpcService(IGameService gameService, IValidator<Models.AttackRequest> attackRequestValidator, IValidator<Models.StartGameRequest> startGameRequestValidator, IValidator<Models.Boat> boatValidator) : Grpc.GameService.GameServiceBase
{
    [Authorize]
    public override async Task<StartGameResponse> StartGame(StartGameRequest request, ServerCallContext context)
    {
        var modelRequest = new Models.StartGameRequest(request.SizeGrid, request.Difficulty);
        
        var gameId = await gameService.StartGame(modelRequest, startGameRequestValidator);

        var response = new StartGameResponse
        {
            GameId = gameId.ToString(),
        };

        return response;
    }
    
    [Authorize]
    public override async Task<AttackResponse> Attack(AttackRequest request, ServerCallContext context)
    {
        var modelRequest = new Models.AttackRequest(Guid.Parse(request.GameId), new Models.Position(request.AttackPosition.X, request.AttackPosition.Y))
        {
            GameId = Guid.Parse(request.GameId)
        };
        
        var (isHit, isSunk, isWinner, position) = await gameService.ProcessAttack(modelRequest, attackRequestValidator);

        var grpcPosition = new Position
        {
            X = position.X,
            Y = position.Y,
            IsHit = position.IsHit
        };

        return new AttackResponse 
        {
            IsHit = isHit,
            IsSunk = isSunk,
            IsWinner = isWinner,
            Position = grpcPosition,
        };
    }

    [Authorize]
    public override async Task<LeaderboardResponse> GetLeaderboard(GetLeaderboardRequest request, ServerCallContext context)
    {
        var result = await gameService.GetLeaderboard();
        return new LeaderboardResponse
        {
            Message = result.ToString()
        };
    }

    [Authorize]
    public override async Task<RollbackTurnResponse> RollbackTurn(RollbackTurnRequest request, ServerCallContext context)
    {
        var gameId = Guid.Parse(request.GameId);
        var result = await gameService.RollbackTurn(gameId);

        return new RollbackTurnResponse
        {
            Message = result.ToString()
        };
    }

    [Authorize]
    public override async Task<PlaceBoatsResponse> PlaceBoats(PlaceBoatsRequest request, ServerCallContext context)
    {
        var playerBoats = (from boat in request.PlayerBoats let positions = boat.Positions.Select(pos => new Models.Position(pos.X, pos.Y)).ToList() select new Models.Boat(positions)).ToList();

        var gameId = Guid.Parse(request.GameId);
        var result = await gameService.PlaceBoats(playerBoats, gameId, boatValidator);

        return new PlaceBoatsResponse
        {
            Message = result.ToString()
        };
    }
}
