using ChessServer.Models;
using ChessServer.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ChessServer.Services;

public interface IGameService
{
    Task<GameDto> CreateGameAsync(string whitePlayerId, string blackPlayerId);
    Task<GameDto?> GetGameByIdAsync(int gameId);
    Task<List<GameDto>> GetAllGames();
    Task<GameDto> UpdateGame(int gameId, string newPgn, GameResult result);
}

public class GameService : IGameService
{
    private readonly ApplicationDbContext _context;

    public GameService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GameDto> CreateGameAsync(string whitePlayerId, string blackPlayerId)
    {
        Game game = new Game(whitePlayerId, blackPlayerId);
        await _context.AddAsync<Game>(game);
        await _context.SaveChangesAsync();
        await _context.Entry(game).Reference(e => e.WhitePlayer).LoadAsync();
        await _context.Entry(game).Reference(e => e.BlackPlayer).LoadAsync();
        GameDto gameDto = ProjectToGameDto(game);
        return gameDto;
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

    public async Task<List<GameDto>> GetAllGames()
    {
        List<GameDto> games = await _context.Games
          .Include((game) => game.WhitePlayer)
          .Include((game) => game.BlackPlayer)
          .Select((game) => ProjectToGameDto(game))
          .ToListAsync();
        return games;
    }

    public async Task<GameDto> UpdateGame(int gameId, string newPgn, GameResult result)
    {
        Game? game = await _context.Games.Where((game) => game.Id == gameId).FirstOrDefaultAsync();
        if (game is null) throw new ArgumentException(); // TODO
        game.Pgn = newPgn;
        if (result != game.Result) game.Result = result;
        await _context.SaveChangesAsync();
        return ProjectToGameDto(game);
    }
}
