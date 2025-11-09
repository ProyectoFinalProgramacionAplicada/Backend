namespace TruekAppAPI.DTO.Reward;

public class RewardRedemptionDto
{
    public int Id { get; set; }
    public int RewardId { get; set; }
    public string Code { get; set; } = default!;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
}