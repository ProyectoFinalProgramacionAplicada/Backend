using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TruekAppAPI.Data;
using TruekAppAPI.DTO.Review;
using TruekAppAPI.Models;
using System.Security.Claims;

namespace TruekAppAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReviewsController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateReview(UserReviewCreateDto dto)
    {
        var fromUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var trade = await db.Trades.FindAsync(dto.TradeId);
        if (trade == null || trade.Status != TradeStatus.Completed)
            return BadRequest("El trueque no está completado.");

        var review = new UserReview
        {
            FromUserId = fromUserId,
            ToUserId = dto.ToUserId,
            TradeId = dto.TradeId,
            Rating = dto.Rating,
            Comment = dto.Comment
        };

        db.UserReviews.Add(review);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(CreateReview), new { review.Id }, review);
    }

    [HttpGet("user/{userId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<UserReviewDto>>> GetReviews(int userId)
    {
        var reviews = await db.UserReviews
            .Where(r => r.ToUserId == userId)
            .Select(r => new UserReviewDto
            {
                Id = r.Id,
                FromUserId = r.FromUserId,
                ToUserId = r.ToUserId,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToListAsync();

        return Ok(reviews);
    }
}