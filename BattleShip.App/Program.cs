using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.Modal;
using Grpc.Net.Client.Web;
using Grpc.Net.Client;
using BattleShip;
using BattleShip.Services;
using Microsoft.AspNetCore.SignalR.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(sp => new HubConnectionBuilder()
    .WithUrl("https://localhost:5134/gameHub")  
    .Build());

builder.Services.AddBlazoredModal();

builder.Services.AddScoped<IGameModalService, GameModalService>();
builder.Services.AddScoped<IGameService, BattleShip.Services.GameService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
    var channel = GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions { HttpClient = httpClient });
    return new BattleShip.Grpc.GameService.GameServiceClient(channel);
});

await builder.Build().RunAsync();

