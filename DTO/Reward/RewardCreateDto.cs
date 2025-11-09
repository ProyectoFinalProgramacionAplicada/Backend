using System.ComponentModel.DataAnnotations;

namespace TruekAppAPI.DTO.Reward;

public class RewardCreateDto
{
    [Required] public string Title { get; set; } = default!;
    public string? Description { get; set; }
    [Range(1, double.MaxValue)] public decimal CostTrueCoins { get; set; }
    public string? ImageUrl { get; set; }
}