using BattleShip.API;
using BattleShip.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddSingleton<IGameRepository, GameRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/startGame", ([FromServices] IGameService gameService, [FromServices] IGameRepository gameRepository) =>
{
    var gameId = Guid.NewGuid();

    var playerBoats = gameService.GenerateRandomBoats();
    var computerBoats = gameService.GenerateRandomBoats();

    var gameState = new GameState(
        gameId: gameId,
        playerBoats: playerBoats,
        computerBoats: computerBoats,
        isPlayerWinner: false,
        isComputerWinner: false
    );
    
    gameRepository.AddGame(gameId, gameState);

    return Results.Ok(new
    {
        gameState.GameId, gameState.PlayerBoats
    });
});

app.MapPost("/attack", ([FromBody] AttackRequest attackRequest, [FromServices] IGameService gameService, [FromServices] IGameRepository gameRepository) =>
{
    var gameState = gameRepository.GetGame(attackRequest.GameId);
    if (gameState == null)
        return Results.NotFound("Game not found");

    var playerAttackResult = gameService.ProcessAttack(gameState.ComputerBoats, attackRequest.AttackPosition);

    if (gameService.CheckIfAllBoatsSunk(gameState.ComputerBoats))
    {
        gameState.IsPlayerWinner = true;
        return Results.Ok(new
        {
            gameState.GameId,
            PlayerAttackResult = "Hit",
            IsPlayerWinner = true,
            IsComputerWinner = false
        });
    }

    var computerAttackPosition = gameService.GenerateRandomPosition();
    var computerAttackResult = gameService.ProcessAttack(gameState.PlayerBoats, computerAttackPosition);

    if (gameService.CheckIfAllBoatsSunk(gameState.PlayerBoats))
    {
        gameState.IsComputerWinner = true;
        return Results.Ok(new
        {
            gameState.GameId,
            PlayerAttackResult = playerAttackResult ? "Hit" : "Miss",
            ComputerAttackPosition = computerAttackPosition,
            ComputerAttackResult = "Hit",
            IsPlayerWinner = false,
            IsComputerWinner = true
        });
    }

    return Results.Ok(new
    {
        gameState.GameId,
        PlayerAttackResult = playerAttackResult ? "Hit" : "Miss",
        ComputerAttackPosition = computerAttackPosition,
        ComputerAttackResult = computerAttackResult ? "Hit" : "Miss",
        IsPlayerWinner = false,
        IsComputerWinner = false
    });
});


app.Run();
