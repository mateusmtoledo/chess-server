namespace ChessServer.Models;

public class AuthRequest
{
    public string Name { get; set; } = null!;
    public string Password { get; set; } = null!;
}
