namespace TruekAppBackend.Models;

public class Reward : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal CostTrueCoins { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}