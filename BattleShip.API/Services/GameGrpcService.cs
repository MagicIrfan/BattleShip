using BattleShip.Models;
using Grpc.Core;

namespace BattleShip.API.Services;

using Grpc;
using System.Linq;
using System.Threading.Tasks;

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

        await Task.Run(() => gameRepository.AddGame(gameId, gameState));

        var response = new StartGameResponse
        {
            GameId = gameId.ToString()
        };

        foreach (var boat in playerBoats)
        {
            var grpcBoat = new Boat { Name = boat.Name };
            grpcBoat.Positions.AddRange(boat.Positions.Select(p => new Position { X = p.X, Y = p.Y, IsHit = p.IsHit }));
            response.PlayerBoats.Add(grpcBoat);
        }

        return response;
    }

    public override async Task<AttackResponse> Attack(AttackRequest request, ServerCallContext context)
    {
        var gameId = Guid.Parse(request.GameId);
        var gameState = await Task.Run(() => gameRepository.GetGame(gameId));
        if (gameState == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));

        var attackPosition = new Models.Position(request.AttackPosition.X,request.AttackPosition.Y);
        var playerAttackResult = await Task.Run(() => gameService.ProcessAttack(gameState.OpponentBoats, attackPosition));

        var computerAttackPosition = await Task.Run(gameService.GenerateRandomPosition);
        var computerAttackResult = await Task.Run(() => gameService.ProcessAttack(gameState.PlayerBoats, computerAttackPosition));

        var response = new AttackResponse
        {
            GameId = gameState.GameId.ToString(),
            PlayerAttackResult = playerAttackResult ? "Hit" : "Miss",
            ComputerAttackPosition = new Position { X = computerAttackPosition.X, Y = computerAttackPosition.Y },
            ComputerAttackResult = computerAttackResult ? "Hit" : "Miss",
            IsPlayerWinner = await Task.Run(() => gameService.CheckIfAllBoatsSunk(gameState.OpponentBoats)),
            IsComputerWinner = await Task.Run(() => gameService.CheckIfAllBoatsSunk(gameState.PlayerBoats))
        };

        return response;
    }

}
