using ChessServer.Models;
using ChessServer.Data;
using Chess;

namespace ChessServer.Services;

class GameService
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
        return game;
    }

    public async Task<Game?> GetGameByIdAsync(int gameId)
    {
        Game? game = await _context.FindAsync<Game>(gameId);
        return game;
    }
}
