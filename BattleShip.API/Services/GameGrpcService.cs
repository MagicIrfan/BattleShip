﻿using BattleShip.Grpc;
using FluentValidation;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace BattleShip.API.Services;

public class GameGrpcService(IGameService gameService, IValidator<Models.AttackRequest> validator) : Grpc.GameService.GameServiceBase
{
    [Authorize]
    public override async Task<StartGameResponse> StartGame(StartGameRequest request, ServerCallContext context)
    {
        var gameId = await gameService.StartGame();

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
        
        var (isHit, isSunk, isWinner) = await gameService.ProcessAttack(modelRequest, validator);

        return new AttackResponse 
        {
            IsHit = isHit,
            IsSunk = isSunk,
            IsWinner = isWinner
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
        var playerBoats = (from boat in request.PlayerBoats let positions = boat.Positions.Select(pos => new Models.Position(pos.X, pos.Y)).ToList() select new Models.Boat(boat.Name, positions)).ToList();

        var gameId = Guid.Parse(request.GameId);
        var result = await gameService.PlaceBoats(playerBoats, gameId);

        return new PlaceBoatsResponse
        {
            Message = result.ToString()
        };
    }
}
