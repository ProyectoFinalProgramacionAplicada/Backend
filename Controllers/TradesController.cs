using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TruekAppAPI.Data;
using TruekAppAPI.DTO.Trade;
using TruekAppAPI.Models;
using System.Security.Claims;

namespace TruekAppAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TradesController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateTrade(TradeCreateDto dto)
    {
        var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var targetListing = await db.Listings.Include(l => l.OwnerUser)
            .FirstOrDefaultAsync(l => l.Id == dto.TargetListingId);
        if (targetListing == null) return NotFound("Publicación no encontrada.");

        var trade = new Trade
        {
            RequesterUserId = requesterId,
            OwnerUserId = targetListing.OwnerUserId,
            TargetListingId = targetListing.Id,
            OfferedListingId = dto.OfferedListingId,
            Message = dto.Message,
            Status = TradeStatus.Pending
        };

        db.Trades.Add(trade);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(CreateTrade), new { trade.Id }, trade);
    }

    [HttpPatch("{id}/accept")]
    public async Task<IActionResult> AcceptTrade(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var trade = await db.Trades.FindAsync(id);
        if (trade == null) return NotFound();
        if (trade.OwnerUserId != userId) return Forbid();

        trade.Status = TradeStatus.Accepted;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, TradeUpdateStatusDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var trade = await db.Trades.FindAsync(id);
        if (trade == null) return NotFound();
        if (trade.OwnerUserId != userId && trade.RequesterUserId != userId)
            return Forbid();

        trade.Status = dto.Status;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Buscar el trade junto con sus mensajes y los usuarios remitentes
        var trade = await db.Trades
            .Include(t => t.Messages)
            .ThenInclude(m => m.SenderUser)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trade == null)
            return NotFound();

        // Solo los usuarios involucrados pueden ver los mensajes
        if (trade.OwnerUserId != userId && trade.RequesterUserId != userId)
            return Forbid();

        // Mapeo a un formato limpio
        var messages = trade.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id,
                m.Text,
                m.CreatedAt,
                m.SenderUserId,
                SenderUserName = m.SenderUser.DisplayName ?? "Usuario"
            })
            .ToList();

        return Ok(messages);
    }


    [HttpPost("{id}/messages")]
    public async Task<IActionResult> SendMessage(int id, [FromBody] string text)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var trade = await db.Trades.FindAsync(id);
        if (trade == null) return NotFound();

        var message = new TradeMessage
        {
            TradeId = id,
            SenderUserId = userId,
            Text = text
        };

        db.TradeMessages.Add(message);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(SendMessage), new { message.Id }, message);
    }

    // 🔹 NUEVO ENDPOINT: obtener todos los trueques del usuario autenticado
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<TradeDto>>> GetMyTrades()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var trades = await db.Trades
            .Where(t => t.RequesterUserId == userId || t.OwnerUserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TradeDto
            {
                Id = t.Id,
                RequesterUserId = t.RequesterUserId,
                OwnerUserId = t.OwnerUserId,
                TargetListingId = t.TargetListingId,
                OfferedListingId = t.OfferedListingId,
                Status = t.Status,
                Message = t.Message,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(trades);
    }
}
