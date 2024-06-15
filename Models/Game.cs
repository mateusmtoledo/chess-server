using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChessServer.Models;

public enum GameResult
{
    Ongoing,
    WhiteWins,
    BlackWins,
    Draw,
}

public class Game
{
    public Game(string whitePlayerId, string blackPlayerId)
    {
        WhitePlayerId = whitePlayerId;
        BlackPlayerId = blackPlayerId;
    }

    [Key]
    public int Id { get; set; }
    [Required]
    public string Pgn { get; set; } = String.Empty;
    [ForeignKey(nameof(WhitePlayer))]
    public string WhitePlayerId { get; set; }
    [ForeignKey(nameof(BlackPlayer))]
    public string BlackPlayerId { get; set; }
    public GameResult Result { get; set; } = GameResult.Ongoing;

    public virtual ApplicationUser? WhitePlayer { get; set; }
    public virtual ApplicationUser? BlackPlayer { get; set; }
}
