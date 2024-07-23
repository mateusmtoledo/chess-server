using System.Collections.Concurrent;

namespace ChessServer.Services;

public interface IConnectionsService
{
    void Add(string connectionId, string? userId);
    void Remove(string connectionId);
    string? GetUserId(string connectionId);
    List<string> GetConnectionsByUserId(string userId);
}

public class ConnectionsService : IConnectionsService
{
    private IDictionary<string, string?> _connections;

    public ConnectionsService()
    {
        _connections = new ConcurrentDictionary<string, string?>();
    }

    public void Add(string connectionId, string? userId)
    {
        _connections.Add(connectionId, userId);
    }

    public void Remove(string connectionId)
    {
        _connections.Remove(connectionId);
    }

    public string? GetUserId(string connectionId)
    {
        return _connections[connectionId];
    }

    public List<string> GetConnectionsByUserId(string userId)
    {
        var connections = new List<string>();
        foreach (var item in _connections)
        {
            if (item.Value == userId) connections.Add(item.Key);
        }
        return connections;
    }
}
