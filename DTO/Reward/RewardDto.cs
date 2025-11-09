namespace TruekAppAPI.DTO.Reward;

public class RewardDto
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public decimal CostTrueCoins { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
}