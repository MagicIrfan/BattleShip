using System.Net.Http.Headers;
using System.Net.Http.Json;
using BattleShip.Models;

namespace BattleShip.Services;

public interface IUserService
{
    Task<User> LoadPlayerProfile();
}

public class UserService : IUserService
{
    private readonly IHttpService _httpService;

    public UserService(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public async Task<User> LoadPlayerProfile()
    {
        try
        {
            var response = await _httpService.SendHttpRequestAsync(HttpMethod.Get, "/auth/profile");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to load player profile");
            }

            var content = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
            return new User
            {
                Name = content?.UserName,
                Profile = content?.Picture
            };
        }
        catch (Exception ex)
        {
            // Remplacez Console.WriteLine par un système de journalisation
            Console.WriteLine($"An error occurred while loading the player profile: {ex.Message}");
            // Vous pouvez aussi relancer l'exception ou retourner null selon le cas d'utilisation
            throw; // Optionnel, selon la stratégie de gestion des erreurs souhaitée
        }
    }

}
