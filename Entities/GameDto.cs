using ChessServer.Models;

public class GameDto
{
    public int Id { get; set; }
    public UserDto WhitePlayer { get; set; } = null!;
    public UserDto BlackPlayer { get; set; } = null!;
    public string Pgn { get; set; } = String.Empty;
    public GameResult Result { get; set; } = GameResult.Ongoing;
}
