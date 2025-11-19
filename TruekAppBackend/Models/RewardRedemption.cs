namespace TruekAppBackend.Models;

public class RewardRedemption : BaseEntity
{
    public int RewardId { get; set; }
    public Reward Reward { get; set; } = default!;
    public int UserId { get; set; }
    public User User { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Status { get; set; } = "Pending"; // Pending/Redeemed/Cancelled
    public DateTime? RedeemedAt { get; set; }
}