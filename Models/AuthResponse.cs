namespace ChessServer.Models;

public class AuthResponse
{
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Token { get; set; } = null!;
}
