using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.Modal;
using Grpc.Net.Client.Web;
using Grpc.Net.Client;
using BattleShip;
using BattleShip.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredModal();

/*builder.Services.AddMsalAuthentication(options => {
    builder.Configuration.Bind("AzureAd", options.ProviderOptions);
    options.ProviderOptions.DefaultAccessTokenScopes.Add("openid");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("profile");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("email");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("https://YOUR_AUTH0_DOMAIN/userinfo");
});*/

builder.Services.AddScoped<IGameModalService, GameModalService>();
builder.Services.AddScoped<IGameService, GameService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
    var channel = GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions { HttpClient = httpClient });
    return new BattleShip.Grpc.GameService.GameServiceClient(channel);
});

await builder.Build().RunAsync();

