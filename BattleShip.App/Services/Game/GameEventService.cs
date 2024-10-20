namespace BattleShip.Services.Game;

public interface IGameEventService
{
    event Action? OnGameRestarted;
    public event Func<Task>? OnStateChanged;
    void RaiseGameRestarted();
    Task NotifyChange();
}
public class GameEventService : IGameEventService
{
    public event Action? OnGameRestarted;
    public event Func<Task>? OnStateChanged;

    public void RaiseGameRestarted()
    {
        OnGameRestarted?.Invoke();
    }

    public async Task NotifyChange()
    {
        if (OnStateChanged != null)
        {
            await OnStateChanged.Invoke();
        }
    }
}