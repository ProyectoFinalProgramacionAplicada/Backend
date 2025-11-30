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

        // ✅ No permitir trades consigo mismo
        if (requesterId == targetListing.OwnerUserId)
            return BadRequest("No puedes hacer una oferta sobre tu propia publicación.");

        // ✅ Evitar trades duplicados
        var existingTrade = await db.Trades
            .FirstOrDefaultAsync(t =>
                t.RequesterUserId == requesterId &&
                t.OwnerUserId == targetListing.OwnerUserId &&
                t.TargetListingId == targetListing.Id &&
                (t.Status == TradeStatus.Pending || t.Status == TradeStatus.Accepted));

        if (existingTrade != null)
            return Ok(existingTrade);

        var trade = new Trade
        {
            RequesterUserId = requesterId,
            OwnerUserId = targetListing.OwnerUserId,
            TargetListingId = targetListing.Id,
            OfferedListingId = dto.OfferedListingId,
            Message = dto.Message,
            Status = TradeStatus.Pending,
            OfferedTrueCoins = dto.OfferedTrueCoins,
            RequestedTrueCoins = dto.RequestedTrueCoins
        };

        db.Trades.Add(trade);
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(dto.Message))
        {
            var message = new TradeMessage
            {
                TradeId = trade.Id,
                SenderUserId = requesterId,
                Text = dto.Message,
                CreatedAt = DateTime.UtcNow
            };
            db.TradeMessages.Add(message);
            await db.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(CreateTrade), new { trade.Id }, trade);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTrade(int id, TradeUpdateDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var trade = await db.Trades
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trade == null) return NotFound("Trueque no encontrado.");

        if (trade.RequesterUserId != userId) return Forbid();
        if (trade.Status != TradeStatus.Pending) return BadRequest("Solo se pueden editar trueques pendientes.");

        var targetListing = await db.Listings.FirstOrDefaultAsync(l => l.Id == dto.TargetListingId);
        if (targetListing == null) return NotFound("Publicación objetivo no encontrada.");

        trade.OfferedListingId = dto.OfferedListingId;
        trade.TargetListingId = dto.TargetListingId;
        trade.Message = dto.Message ?? trade.Message;
        trade.OfferedTrueCoins = dto.OfferedTrueCoins;
        trade.RequestedTrueCoins = dto.RequestedTrueCoins;

        await db.SaveChangesAsync();
        return NoContent();
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

    // ==========================================
    // POST /api/Trades/{id}/complete
    // Completar trueque (solo el vendedor)
    // ==========================================
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteTrade(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // 🔧 CAMBIO: Usar 'db' en lugar de '_db'
        var trade = await db.Trades
            .Include(t => t.TargetListing)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trade == null) return NotFound("Trueque no encontrado.");

        if (trade.OwnerUserId != userId)
        {
            return Forbid("Solo el dueño del producto puede finalizar el trueque.");
        }

        if (trade.Status == TradeStatus.Completed)
            return BadRequest("Este trueque ya está finalizado.");

        // ✅ LÓGICA DE FINALIZACIÓN
        trade.Status = TradeStatus.Completed;
        trade.CompletedAt = DateTime.UtcNow; // 🆕 Registrar fecha de finalización
        
        if (trade.TargetListing != null)
        {
            trade.TargetListing.IsAvailable = false;
            trade.TargetListing.IsPublished = false;
        }

        // 🔧 CAMBIO: Usar 'db' en lugar de '_db'
        var otherTrades = await db.Trades
            .Where(t => t.TargetListingId == trade.TargetListingId && t.Id != trade.Id && t.Status == TradeStatus.Pending)
            .ToListAsync();
        
        foreach (var other in otherTrades)
        {
            other.Status = TradeStatus.Cancelled;
        }

        // 🔧 CAMBIO: Usar 'db' en lugar de '_db'
        await db.SaveChangesAsync();
        return Ok(new { message = "Trueque finalizado con éxito." });
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var trade = await db.Trades
            .Include(t => t.Messages)
            .ThenInclude(m => m.SenderUser)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trade == null) return NotFound();
        if (trade.OwnerUserId != userId && trade.RequesterUserId != userId) return Forbid();

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

        var lastMessage = await db.TradeMessages
            .Where(m => m.TradeId == id && m.SenderUserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        if (lastMessage != null && (DateTime.UtcNow - lastMessage.CreatedAt).TotalSeconds < 5)
            return BadRequest("No puedes enviar mensajes idénticos tan seguido.");

        var message = new TradeMessage
        {
            TradeId = id,
            SenderUserId = userId,
            Text = text,
            CreatedAt = DateTime.UtcNow
        };

        db.TradeMessages.Add(message);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(SendMessage), new { message.Id }, message);
    }

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
                CreatedAt = t.CreatedAt,
                OfferedTrueCoins = t.OfferedTrueCoins,
                RequestedTrueCoins = t.RequestedTrueCoins,
                ListingOwnerId = t.OwnerUserId,
                InitiatorUserId = t.RequesterUserId
            })
            .ToListAsync();

        return Ok(trades);
    }
}