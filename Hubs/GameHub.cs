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
    private readonly IConnectionsService _connectionsService;

    public GameHub(IConnectionsService connectionsService, IQueueService queueService, IGameService gameService)
    {
        _queueService = queueService;
        _gameService = gameService;
        _connectionsService = connectionsService;
    }

    private string GetConnectionId()
    {
        return Context.ConnectionId;
    }

    private string? GetUserId()
    {
        string connectionId = GetConnectionId();
        string? userId = _connectionsService.GetUserId(connectionId);
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
        string connectionId = GetConnectionId();
        await Groups.AddToGroupAsync(connectionId, gameId.ToString());
        await Clients.Caller.SendAsync("gameDataUpdated", game);
    }

    public async Task UnsubscribeFromGame(int gameId)
    {
        string connectionId = GetConnectionId();
        await Groups.RemoveFromGroupAsync(connectionId, gameId.ToString());
    }

    [Authorize]
    public async Task JoinQueue()
    {
        string? userId = GetUserId();
        if (userId is null) return;
        string connectionId = GetConnectionId();
        bool added = _queueService.AddToQueue(connectionId);
        if (!added) return;
        if (_queueService.Count() >= 2)
        {
            List<string> connectionIds = _queueService.Take(2);
            _queueService.RemoveFromQueue(connectionIds[0]);
            _queueService.RemoveFromQueue(connectionIds[1]);
            Random rnd = new Random();
            int whitePlayerIndex = rnd.Next(2);
            int blackPlayerIndex = whitePlayerIndex == 0 ? 1 : 0;
            string? whitePlayerId = _connectionsService.GetUserId(connectionIds[whitePlayerIndex]);
            string? blackPlayerId = _connectionsService.GetUserId(connectionIds[blackPlayerIndex]);
            if (whitePlayerId is null || blackPlayerId is null) return;
            GameDto gameDto = await _gameService.CreateGameAsync(whitePlayerId, blackPlayerId);
            await Clients.Clients(connectionIds).SendAsync("gameCreated", gameDto);
        }
        SendPlayersInQueueCount();
    }

    private void RemoveFromQueue(string connectionId)
    {
        bool result = _queueService.RemoveFromQueue(connectionId);
        if (!result) return;
        SendPlayersInQueueCount();
    }

    [Authorize]
    public void LeaveQueue()
    {
        string? userId = GetUserId();
        if (userId is null) return;
        string connectionId = GetConnectionId();
        RemoveFromQueue(connectionId);
    }

    public async Task GetAllGames()
    {
        var games = await _gameService.GetAllGames();
        await Clients.Caller.SendAsync("gameListUpdated", games);
    }

    private PieceColor? GetPlayerColor(GameDto game)
    {
        string? userId = GetUserId();
        if (userId is null) return null;
        if (game.WhitePlayer.Id == userId) return PieceColor.White;
        if (game.BlackPlayer.Id == userId) return PieceColor.Black;
        return null;
    }

    private GameResult MapEndgameInfoToGameResult(EndGameInfo? endGameInfo)
    {
        if (endGameInfo is null) return GameResult.Ongoing;
        if (endGameInfo.WonSide is null) return GameResult.Draw;
        if (endGameInfo.WonSide == PieceColor.White) return GameResult.WhiteWins;
        if (endGameInfo.WonSide == PieceColor.Black) return GameResult.BlackWins;
        throw new ArgumentException();
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
            GameResult result = MapEndgameInfoToGameResult(board.EndGame);
            GameDto gameDto = await _gameService.UpdateGame(gameId, newPgn, result);
            if (result != GameResult.Ongoing) await Clients.Group(game.Id.ToString()).SendAsync("gameDataUpdated", gameDto);
            else await Clients.Group(game.Id.ToString()).SendAsync("pgnUpdated", game.Id, newPgn);
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

    public override async Task OnConnectedAsync()
    {
        string connectionId = GetConnectionId();
        string? userId = Context.User?.FindFirst("UserId")?.Value;
        _connectionsService.Add(connectionId, userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine("Disconnected");
        string? userId = GetUserId();
        string connectionId = GetConnectionId();
        RemoveFromQueue(connectionId);
        _connectionsService.Remove(connectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

