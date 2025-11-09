using System.ComponentModel.DataAnnotations;

namespace TruekAppAPI.DTO.Auth;

public class UserLoginDto
{
    [EmailAddress] public string Email { get; set; } = default!;
    [MinLength(6)] public string Password { get; set; } = default!;
}