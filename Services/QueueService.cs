namespace ChessServer.Services;

public interface IQueueService
{
    bool AddToQueue(string connectionId);
    bool RemoveFromQueue(string connectionId);
    int Count();
    List<string> Take(int num);
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

    public bool AddToQueue(string connectionId)
    {
        bool result = _playersInQueue.Add(connectionId);
        return result;
    }

    public bool RemoveFromQueue(string connectionId)
    {
        bool result = _playersInQueue.Remove(connectionId);
        return result;
    }

    public List<string> Take(int num)
    {
        return _playersInQueue.Take(num).ToList();
    }

    public int Count()
    {
        return _playersInQueue.Count;
    }
}
