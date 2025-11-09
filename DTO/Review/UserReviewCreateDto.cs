using System.ComponentModel.DataAnnotations;

namespace TruekAppAPI.DTO.Review;

public class UserReviewCreateDto
{
    [Required] public int ToUserId { get; set; }
    [Required] public int TradeId { get; set; }
    [Range(1, 5)] public int Rating { get; set; }
    public string? Comment { get; set; }
}