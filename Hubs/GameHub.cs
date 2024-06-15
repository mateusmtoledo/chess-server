using Microsoft.AspNetCore.SignalR;
using Chess;

namespace ChessServer.Hubs;

public class GameHub : Hub
{
    private ChessBoard _board;

    public GameHub(ChessBoard board)
    {
        _board = board;
    }

    public void PlayMove(string from, string to)
    {
        _board.Move(new Move(from, to));
        Clients.All.SendAsync("newPgn", _board.ToPgn());
    }

    public void GetPosition()
    {
        Clients.Caller.SendAsync("newPgn", _board.ToPgn());
    }
}

