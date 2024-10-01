using BattleShip.Components;
using Blazored.Modal;
using Blazored.Modal.Services;

namespace BattleShip.Services;

public interface IGameModalService
{
    Task<string?> ShowModal(string title, string message);
}

public class GameModalService : IGameModalService
{
    private readonly IModalService _modalService;

    public GameModalService(IModalService modalService)
    {
        _modalService = modalService;
    }

    public async Task<string?> ShowModal(string title, string message)
    {
        var parameters = new ModalParameters();
        parameters.Add("Title", title);
        parameters.Add("Message", message);

        var options = new ModalOptions
        {
            HideCloseButton = true,
            DisableBackgroundCancel = true
        };


        var modal = _modalService.Show<GameModal>("Fin du jeu", parameters, options);
        var result = await modal.Result;

        return result?.Data?.ToString();
    }
}
