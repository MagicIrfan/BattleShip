using Auth0.AspNetCore.Authentication;
using BattleShip.API;
using BattleShip.API.Services;
using BattleShip.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddGrpc();

builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
builder.Services.AddSingleton<IGameRepository, GameRepository>();
builder.Services.AddValidatorsFromAssemblyContaining<AttackRequestValidator>();

builder.Services.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = builder.Configuration["Auth0:Domain"] ?? string.Empty;
    options.ClientId = builder.Configuration["Auth0:ClientId"] ?? string.Empty;
});
builder.Services.AddControllersWithViews();

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        corsPolicyBuilder =>
        {
            corsPolicyBuilder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("grpc-status", "grpc-message");
        });
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("AllowAllOrigins");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseGrpcWeb();

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<GameGrpcService>().EnableGrpcWeb();

app.UseHttpsRedirection();

app.MapHub<GameHub>("/gameHub");

var gameMethodsGroup = app.MapGroup("/api/game/");

gameMethodsGroup.MapPost("/startGame", [Authorize](IGameService gameService) => gameService.StartGame());
gameMethodsGroup.MapPost("/placeBoats", [Authorize]([FromBody] List<Boat> playerBoats, IGameService gameService) => gameService.PlaceBoats(playerBoats));
gameMethodsGroup.MapGet("/leaderboard", [Authorize](IGameService gameService) => gameService.GetLeaderboard());
gameMethodsGroup.MapPost("/rollback", [Authorize]([FromQuery] Guid gameId, IGameRepository gameRepository, IGameService gameService) => gameService.RollbackTurn(gameId));
gameMethodsGroup.MapPost("/attack", [Authorize] async (AttackRequest attackRequest, IValidator<AttackRequest> validator, IGameRepository gameRepository, IGameService gameService) =>
        await gameService.ProcessAttack(attackRequest, validator))
    .Produces(200)
    .Produces(404)
    .ProducesValidationProblem();


var authenticationMethodsGroup = app.MapGroup("/api/auth/");

authenticationMethodsGroup.MapGet("/login", async (IAuthenticationService authService) => await authService.Login());
authenticationMethodsGroup.MapPost("/logout", [Authorize] async (IAuthenticationService authService) => await authService.Logout());
authenticationMethodsGroup.MapGet("/profile", (IAuthenticationService authService) => authService.Profile());

app.Run();