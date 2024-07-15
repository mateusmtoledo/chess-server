using ChessServer.Models;
using ChessServer.Data;
using Chess;
using Microsoft.EntityFrameworkCore;

namespace ChessServer.Services;

public interface IGameService
{
    Task<Game> CreateGameAsync(string whitePlayerId, string blackPlayerId);
    Task<GameDto?> GetGameByIdAsync(int gameId);
    Task<List<GameDto>> GetAllGames();
    Task UpdateGame(int gameId, string newPgn, EndGameInfo? result);
}

public class GameService : IGameService
{
    private readonly ApplicationDbContext _context;

    public GameService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Game> CreateGameAsync(string whitePlayerId, string blackPlayerId)
    {
        Game game = new Game(whitePlayerId, blackPlayerId);
        game.Pgn = new ChessBoard().ToPgn();
        await _context.AddAsync<Game>(game);
        await _context.SaveChangesAsync();
        return game;
    }

    private static GameDto ProjectToGameDto(Game game)
    {
        return new GameDto
        {
            Id = game.Id,
            WhitePlayer = new UserDto
            {
                Id = game.WhitePlayer!.Id,
                Name = game.WhitePlayer.Name
            },
            BlackPlayer = new UserDto
            {
                Id = game.BlackPlayer!.Id,
                Name = game.BlackPlayer.Name
            },
            Pgn = game.Pgn,
            Result = game.Result
        };
    }

    public async Task<GameDto?> GetGameByIdAsync(int gameId)
    {
        GameDto? game = await _context.Games
          .Include((game) => game.WhitePlayer)
          .Include((game) => game.BlackPlayer)
          .Where((game) => game.Id == gameId)
          .Select((game) => ProjectToGameDto(game))
          .FirstOrDefaultAsync();
        return game;
    }

    private GameResult MapEndgameInfoToGameResult(EndGameInfo? endGameInfo)
    {
        if (endGameInfo is null) return GameResult.Ongoing;
        if (endGameInfo.WonSide is null) return GameResult.Draw;
        if (endGameInfo.WonSide == PieceColor.White) return GameResult.WhiteWins;
        if (endGameInfo.WonSide == PieceColor.Black) return GameResult.BlackWins;
        throw new ArgumentException();
    }

    public async Task<List<GameDto>> GetAllGames()
    {
        List<GameDto> games = await _context.Games
          .Include((game) => game.WhitePlayer)
          .Include((game) => game.BlackPlayer)
          .Select((game) => ProjectToGameDto(game))
          .ToListAsync();
        return games;
    }

    public async Task UpdateGame(int gameId, string newPgn, EndGameInfo? endGameInfo)
    {
        Game? game = await _context.Games.Where((game) => game.Id == gameId).FirstOrDefaultAsync();
        if (game is null) return; // TODO
        game.Pgn = newPgn;
        GameResult result = MapEndgameInfoToGameResult(endGameInfo);
        if (result != game.Result) game.Result = result;
        await _context.SaveChangesAsync();
    }
}
