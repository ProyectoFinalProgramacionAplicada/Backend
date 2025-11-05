namespace TruekAppAPI.Models;

public class UserReview : BaseEntity
{
    public int FromUserId { get; set; }
    public User FromUser { get; set; } = default!;
    public int ToUserId { get; set; }
    public User ToUser { get; set; } = default!;
    public int TradeId { get; set; }
    public Trade Trade { get; set; } = default!;
    public int Rating { get; set; }  // 1..5
    public string? Comment { get; set; }
}