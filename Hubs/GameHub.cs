using Microsoft.AspNetCore.SignalR;
using ChessServer.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ChessServer.Hubs;

public class GameHub : Hub
{
    private IQueueService _queueService;

    public GameHub(IQueueService queueService)
    {
        _queueService = queueService;
    }

    private void SendPlayersInQueueCount()
    {
        Clients.All.SendAsync("playersInQueueCountUpdated", _queueService.Count());
    }

    public void GetPlayersInQueueCount()
    {
        Clients.Caller.SendAsync("playersInQueueCountUpdated", _queueService.Count());
    }

    [Authorize]
    public async Task JoinQueue()
    {
        string? userId = Context.User?.FindFirst("UserId")?.Value;
        if (userId is null) return;
        bool result = await _queueService.AddToQueue(userId);
        if (!result) return;
        SendPlayersInQueueCount();
    }

    [Authorize]
    public void LeaveQueue()
    {
        string? userId = Context.User?.FindFirst("UserId")?.Value;
        if (userId is null) return;
        bool result = _queueService.RemoveFromQueue(userId);
        if (!result) return;
        SendPlayersInQueueCount();
    }

    /* public void PlayMove(string from, string to) */
    /* { */
    /*     _board.Move(new Move(from, to)); */
    /*     Clients.All.SendAsync("newPgn", _board.ToPgn()); */
    /* } */

    /* public void GetPosition() */
    /* { */
    /*     Clients.Caller.SendAsync("newPgn", _board.ToPgn()); */
    /* } */
}

