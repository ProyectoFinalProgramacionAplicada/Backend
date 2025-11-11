using System.ComponentModel.DataAnnotations;

namespace TruekAppBackend.DTO.Auth;

public class UserLoginDto
{
    [EmailAddress] public string Email { get; set; } = default!;
    [MinLength(6)] public string Password { get; set; } = default!;
}