namespace ChessServer.Services;

public interface IQueueService
{
    Task<bool> AddToQueue(string userId);
    bool RemoveFromQueue(string userId);
    int Count();
}

public class QueueService : IQueueService
{
    private readonly IGameService _gameService;
    private HashSet<string> _playersInQueue;

    public QueueService(IGameService gameService, [FromKeyedServices("playersInQueue")] HashSet<string> playersInQueue)
    {
        _gameService = gameService;
        _playersInQueue = playersInQueue;
    }

    public async Task<bool> AddToQueue(string userId)
    {
        bool result = _playersInQueue.Add(userId);
        if (_playersInQueue.Count >= 2)
        {
            List<string> players = _playersInQueue.Take(2).ToList();
            _playersInQueue.Remove(players[0]);
            _playersInQueue.Remove(players[1]);
            await _gameService.CreateGameAsync(players[0], players[1]);
        }
        return result;
    }

    public bool RemoveFromQueue(string userId)
    {
        bool result = _playersInQueue.Remove(userId);
        return result;
    }

    public int Count()
    {
        return _playersInQueue.Count;
    }
}
