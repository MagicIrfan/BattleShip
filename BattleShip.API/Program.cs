using Auth0.AspNetCore.Authentication;
using BattleShip.API;
using BattleShip.API.Methods;
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

app.MapPost("/startGame", [Authorize] async (IGameService gameService) => 
    await GameMethods.StartGame(gameService));

app.MapPost("/attack", [Authorize] async (AttackRequest attackRequest, IValidator<AttackRequest> validator, IGameRepository gameRepository, IGameService gameService) => 
        await GameMethods.ProcessAttack(attackRequest, validator, gameRepository, gameService))
.Produces(200)
.Produces(404)
.ProducesValidationProblem();

app.MapPost("/rollback", [Authorize] async ([FromQuery] Guid gameId, IGameRepository gameRepository, IGameService gameService) =>
{
    await GameMethods.Rollback(gameRepository, gameService, gameId);
});

app.MapGet("/login",  async (context) =>
{
    await AuthenticationMethods.Login(context, "/");
});

app.MapPost("/logout", [Authorize] async (context) =>
{
    await AuthenticationMethods.Logout(context);
});

app.MapGet("/profile", async (HttpContext context) =>
{
    return await AuthenticationMethods.Profile(context);
});

app.Run();
