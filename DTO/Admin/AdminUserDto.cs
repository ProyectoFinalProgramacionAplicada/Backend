using TruekAppAPI.Models;

namespace TruekAppAPI.DTO.Admin;

/// <summary>
/// DTO para usuarios en el panel de administración
/// </summary>
public class AdminUserDto
{
    public int Id { get; set; }
    public string? DisplayName { get; set; }
    public string Email { get; set; } = default!;
    public AppRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public decimal TrueCoinBalance { get; set; }
}