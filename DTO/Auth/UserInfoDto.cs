using TruekAppAPI.Models;

namespace TruekAppAPI.DTO.Auth;

public class UserInfoDto
{
    public int Id { get; set; }
    public string Email { get; set; } = default!;
    public AppRole Role { get; set; }
    public int? CompanyId { get; set; }
    public decimal TrueCoinBalance { get; set; }
}