namespace TruekAppAPI.Models;

public class User : BaseEntity
{
    public int Id { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public AppRole Role { get; set; }
    public string? DisplayName { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public decimal TrueCoinBalance { get; set; }
    public int? CompanyId { get; set; } = null!;
    public Company? Company { get; set; }
    public bool IsActive { get; set; } = true;
}