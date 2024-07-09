using System.ComponentModel.DataAnnotations;

namespace ChessServer.Models;

public class RegistrationRequest
{

    [Required]
    public string Name { get; set; } = null!;
    [Required]
    public string Password { get; set; } = null!;
}
