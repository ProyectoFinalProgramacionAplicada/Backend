namespace TruekAppAPI.Models;

public class Company : BaseEntity
{
    public string Name { get; set; } = default!;
    public string OwnerName { get; set; } = default!;
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public bool IsActive { get; set; } = true;
}