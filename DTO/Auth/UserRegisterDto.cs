using System.ComponentModel.DataAnnotations;
using TruekAppAPI.Models;

namespace TruekAppAPI.DTO.Auth;

public class UserRegisterDto
{
    [EmailAddress]
    [RegularExpression(@"^[A-Za-z0-9._%+-]+@gmail\.com$", ErrorMessage = "Usa un correo @gmail.com válido.")]
    public string Email { get; set; } = default!;

    [MinLength(6)]
    public string Password { get; set; } = default!;

    [RegularExpression(@"^\+[1-9]\d{6,14}$", ErrorMessage = "Teléfono en formato E.164, ej: +5917XXXXXXX")]
    public string Phone { get; set; } = default!;

    public AppRole Role { get; set; }
    public int? CompanyId { get; set; }
}