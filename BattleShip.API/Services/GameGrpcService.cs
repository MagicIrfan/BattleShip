using BattleShip.API.Helpers;
using BattleShip.Grpc;
using BattleShip.Models;
using Grpc.Core;
using AttackRequest = BattleShip.Grpc.AttackRequest;
using Boat = BattleShip.Grpc.Boat;
using Position = BattleShip.Grpc.Position;

namespace BattleShip.API.Services;

public class GameGrpcService(IGameService gameService, IGameRepository gameRepository) : Grpc.GameService.GameServiceBase
{
    public override async Task<StartGameResponse> StartGame(StartGameRequest request, ServerCallContext context)
    {
        var gameId = Guid.NewGuid();
        var playerBoats = gameService.GenerateRandomBoats();
        var computerBoats = gameService.GenerateRandomBoats();

        var gameState = new GameState(
            gameId: gameId,
            playerBoats: playerBoats,
            opponentBoats: computerBoats,
            isPlayerWinner: false,
            isOpponentWinner: false
        );

        gameRepository.AddGame(gameId, gameState);

        var response = new StartGameResponse
        {
            GameId = gameId.ToString(),
            PlayerBoats = {
                playerBoats.Select(boat => new Boat {
                    Name = boat.Name,
                    Positions = { boat.Positions.Select(p => new Position { X = p.X, Y = p.Y, IsHit = p.IsHit }) }
                })
            }
        };

        return await Task.FromResult(response);
    }

    public override Task<AttackResponse> Attack(AttackRequest request, ServerCallContext context)
    {
        var gameId = Guid.Parse(request.GameId);
        var gameState = gameRepository.GetGame(gameId);

        if (gameState == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
        }

        var attackPosition = new Models.Position(request.AttackPosition.X, request.AttackPosition.Y);
        var playerAttackResult = AttackHelper.ProcessAttack(gameState.OpponentBoats, attackPosition);

        var computerAttackPosition = gameService.GenerateRandomPosition();
        var computerAttackResult = AttackHelper.ProcessAttack(gameState.PlayerBoats, computerAttackPosition);

        gameRepository.UpdateGame(gameState);

        return Task.FromResult(new AttackResponse
        {
            GameId = gameState.GameId.ToString(),
            PlayerAttackResult = playerAttackResult ? "Hit" : "Miss",
            ComputerAttackPosition = new Position { X = computerAttackPosition.X, Y = computerAttackPosition.Y },
            ComputerAttackResult = computerAttackResult ? "Hit" : "Miss",
            IsPlayerWinner = gameService.CheckIfAllBoatsSunk(gameState.OpponentBoats),
            IsComputerWinner = gameService.CheckIfAllBoatsSunk(gameState.PlayerBoats)
        });
    }
}