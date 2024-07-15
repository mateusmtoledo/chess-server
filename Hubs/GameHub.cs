using Microsoft.AspNetCore.SignalR;
using ChessServer.Services;
using Chess;
using ChessServer.Models;
using Microsoft.AspNetCore.Authorization;

namespace ChessServer.Hubs;

public class GameHub : Hub
{
    private readonly IQueueService _queueService;
    private readonly IGameService _gameService;

    public GameHub(IQueueService queueService, IGameService gameService)
    {
        _queueService = queueService;
        _gameService = gameService;
    }

    private string? GetUserId()
    {
        string? userId = Context.User?.FindFirst("UserId")?.Value;
        return userId;
    }

    private void SendPlayersInQueueCount()
    {
        Clients.All.SendAsync("playersInQueueCountUpdated", _queueService.Count());
    }

    public void GetPlayersInQueueCount()
    {
        Clients.Caller.SendAsync("playersInQueueCountUpdated", _queueService.Count());
    }

    private bool IsGameDone(GameDto game)
    {
        return game.Result != GameResult.Ongoing;
    }

    public async Task SubscribeToGame(int gameId)
    {
        GameDto? game = await _gameService.GetGameByIdAsync(gameId);
        if (game is null) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());
        await Clients.Caller.SendAsync("gameDataUpdated", game);
    }

    public async Task UnsubscribeFromGame(int gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId.ToString());
    }

    [Authorize]
    public async Task JoinQueue()
    {
        string? userId = GetUserId();
        if (userId is null) return;
        bool result = await _queueService.AddToQueue(userId);
        if (!result) return;
        SendPlayersInQueueCount();
    }

    private void RemoveFromQueue(string userId)
    {
        bool result = _queueService.RemoveFromQueue(userId);
        if (!result) return;
        SendPlayersInQueueCount();
    }

    [Authorize]
    public void LeaveQueue()
    {
        string? userId = GetUserId();
        if (userId is null) return;
        RemoveFromQueue(userId);
    }

    public async Task GetAllGames()
    {
        var games = await _gameService.GetAllGames();
        await Clients.Caller.SendAsync("gameListUpdated", games);
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine("Connected");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine("Disconnected");
        string? userId = GetUserId();
        if (userId is not null)
        {
            RemoveFromQueue(userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    private PieceColor? GetPlayerColor(GameDto game)
    {
        string? userId = GetUserId();
        if (userId is null) return null;
        if (game.WhitePlayer.Id == userId) return PieceColor.White;
        if (game.BlackPlayer.Id == userId) return PieceColor.Black;
        return null;
    }

    [Authorize]
    public async Task PlayMove(int gameId, string from, string to)
    {
        GameDto? game = await _gameService.GetGameByIdAsync(gameId);
        if (game is null || IsGameDone(game)) return; // TODO
        string currentPgn = game.Pgn;
        ChessBoard board = ChessBoard.LoadFromPgn(currentPgn);
        PieceColor? playerColor = GetPlayerColor(game);
        if (playerColor != board.Turn) return; // TODO
        try
        {
            board.Move(new Move(from, to));
            string newPgn = board.ToPgn();
            EndGameInfo? endGameInfo = board.EndGame;
            await _gameService.UpdateGame(gameId, newPgn, endGameInfo);
            await Clients.Group(game.Id.ToString()).SendAsync("pgnUpdated", game.Id, newPgn);
        }
        catch (ChessException e)
        {
            Console.WriteLine(e.Message);
            await Clients.Caller.SendAsync("invalidMove", gameId, from, to);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}

