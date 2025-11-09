using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TruekAppAPI.Data;
using TruekAppAPI.DTO.Listing;
using TruekAppAPI.Models;
using System.Security.Claims;

namespace TruekAppAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ListingsController(AppDbContext db) : ControllerBase
{
    [HttpGet("catalog")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ListingDto>>> GetCatalog(
        [FromQuery] int? ownerId,
        [FromQuery] string? q,
        [FromQuery] decimal? minValue,
        [FromQuery] decimal? maxValue)
    {
        var listings = db.Listings
            .Include(l => l.OwnerUser)
            .Where(l => l.IsPublished && l.IsAvailable);

        if (ownerId.HasValue)
            listings = listings.Where(l => l.OwnerUserId == ownerId);

        if (!string.IsNullOrEmpty(q))
            listings = listings.Where(l => l.Title.Contains(q));

        if (minValue.HasValue)
            listings = listings.Where(l => l.TrueCoinValue >= minValue);

        if (maxValue.HasValue)
            listings = listings.Where(l => l.TrueCoinValue <= maxValue);

        var result = await listings.Select(l => new ListingDto
        {
            Id = l.Id,
            Title = l.Title,
            TrueCoinValue = l.TrueCoinValue,
            ImageUrl = l.ImageUrl,
            IsPublished = l.IsPublished
        }).ToListAsync();

        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(ListingCreateDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var listing = new Listing
        {
            OwnerUserId = userId,
            Title = dto.Title,
            Description = dto.Description,
            TrueCoinValue = dto.TrueCoinValue,
            ImageUrl = dto.ImageUrl,
            Address = dto.Address,
            Lat = dto.Lat,
            Lng = dto.Lng,
            IsPublished = true
        };

        db.Listings.Add(listing);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Create), new { listing.Id }, listing);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ListingUpdateDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var listing = await db.Listings.FindAsync(id);
        if (listing == null) return NotFound();
        if (listing.OwnerUserId != userId) return Forbid();

        listing.Title = dto.Title ?? listing.Title;
        listing.Description = dto.Description ?? listing.Description;
        listing.TrueCoinValue = dto.TrueCoinValue ?? listing.TrueCoinValue;
        listing.ImageUrl = dto.ImageUrl ?? listing.ImageUrl;
        listing.IsPublished = dto.IsPublished ?? listing.IsPublished;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var listing = await db.Listings.FindAsync(id);
        if (listing == null) return NotFound();
        if (listing.OwnerUserId != userId) return Forbid();

        db.Listings.Remove(listing);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
