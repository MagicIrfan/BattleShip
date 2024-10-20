using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.Modal;
using Blazored.SessionStorage;
using Grpc.Net.Client.Web;
using Grpc.Net.Client;
using BattleShip;
using BattleShip.Services;
using Microsoft.AspNetCore.SignalR.Client;
using BattleShip.Services.Game;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(sp => new HubConnectionBuilder()
    .WithUrl("https://localhost:5134/gameHub")  
    .Build());

builder.Services.AddBlazoredModal();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Auth0", options.ProviderOptions);
    Console.WriteLine(options.ProviderOptions.Authority);
    options.ProviderOptions.AdditionalProviderParameters.Add("audience", builder.Configuration["Auth0:Audience"]);
});

builder.Services.AddScoped<IGameModalService, GameModalService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IHttpService, HttpService>();
builder.Services.AddScoped<IGameEventService, GameEventService>();
builder.Services.AddScoped<IGameLogicService, GameLogicService>();
builder.Services.AddScoped<IGameApiService, GameApiService>();
builder.Services.AddScoped<IGameStateService, GameStateService>();
builder.Services.AddScoped<IGameUIService, GameUIService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGameMultiplayerService, GameMultiplayerService>();
builder.Services.AddScoped<SignalRService>();

builder.Services.AddBlazoredSessionStorage();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
    var channel = GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions { HttpClient = httpClient });
    return new BattleShip.Grpc.GameService.GameServiceClient(channel);
});

await builder.Build().RunAsync();

