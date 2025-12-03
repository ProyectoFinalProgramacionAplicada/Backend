using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TruekAppAPI.Data;
using TruekAppAPI.DTO.Trade;
using TruekAppAPI.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR; // Importante
using TruekAppAPI.Hubs;             // Importante
using TruekAppAPI.Services;         // 👈 para IWalletService

namespace TruekAppAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TradesController : ControllerBase
{
    private readonly AppDbContext db;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IWalletService _walletService; // 👈 nuevo campo

    // Constructor con Hub + WalletService
    public TradesController(
        AppDbContext db,
        IHubContext<ChatHub> hubContext,
        IWalletService walletService)
    {
        this.db = db;
        _hubContext = hubContext;
        _walletService = walletService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTrade(TradeCreateDto dto)
    {
        var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var targetListing = await db.Listings.Include(l => l.OwnerUser)
            .FirstOrDefaultAsync(l => l.Id == dto.TargetListingId);
        if (targetListing == null) return NotFound("Publicación no encontrada.");

        if (requesterId == targetListing.OwnerUserId)
            return BadRequest("No puedes hacer una oferta sobre tu propia publicación.");

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

        var trade = await db.Trades.FirstOrDefaultAsync(t => t.Id == id);

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

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteTrade(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var trade = await db.Trades
            .Include(t => t.TargetListing)
            .Include(t => t.RequesterUser) // comprador
            .Include(t => t.OwnerUser).Include(trade => trade.OfferedListing) // vendedor
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trade == null) return NotFound("Trueque no encontrado.");

        if (trade.OwnerUserId != userId)
            return Forbid("Solo el dueño del producto puede finalizar el trueque.");

        if (trade.Status == TradeStatus.Completed)
            return BadRequest("Este trueque ya está finalizado.");

        if (trade.Status == TradeStatus.Cancelled)
            return BadRequest("No se puede completar un trueque cancelado.");

        // Cálculo neto (comprador vs vendedor)
        decimal offered   = (decimal)(trade.OfferedTrueCoins   ?? 0);
        decimal requested = (decimal)(trade.RequestedTrueCoins ?? 0);
        decimal net = offered - requested;
        // net > 0 => comprador paga al vendedor
        // net < 0 => vendedor paga al comprador
        // net = 0 => sin movimiento

        // 1) Transferencia de TrueCoins si aplica
        try
        {
            if (net > 0)
            {
                // Comprador -> Vendedor
                await _walletService.ApplyTradeTransferAsync(
                    fromUserId: trade.RequesterUserId,
                    toUserId: trade.OwnerUserId,
                    amount: net,
                    tradeId: trade.Id
                );
            }
            else if (net < 0)
            {
                // Vendedor -> Comprador (cambio)
                decimal refund = -net;
                await _walletService.ApplyTradeTransferAsync(
                    fromUserId: trade.OwnerUserId,
                    toUserId: trade.RequesterUserId,
                    amount: refund,
                    tradeId: trade.Id
                );
            }
        }
        catch (InvalidOperationException ex)
        {
            // Por ejemplo: saldo insuficiente
            return BadRequest(ex.Message);
        }

        // 2) Marcar trade como completado y cerrar otros
        trade.Status = TradeStatus.Completed;
        trade.CompletedAt = DateTime.UtcNow;

        if (trade.TargetListing != null)
        {
            trade.TargetListing.IsAvailable = false;
            trade.TargetListing.IsPublished = false;
        }
        
        if (trade.OfferedListing != null)
        {
            trade.OfferedListing.IsAvailable = false;
            trade.OfferedListing.IsPublished = false;
        }
            

        var otherTrades = await db.Trades
            .Where(t => t.TargetListingId == trade.TargetListingId
                        && t.Id != trade.Id
                        && t.Status == TradeStatus.Pending)
            .ToListAsync();

        foreach (var other in otherTrades)
        {
            other.Status = TradeStatus.Cancelled;
        }

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

    // ==========================================
    // Método SendMessage con SignalR
    // ==========================================
    [HttpPost("{id}/messages")]
    public async Task<IActionResult> SendMessage(int id, [FromBody] string text)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Usuario";

        var trade = await db.Trades.FindAsync(id);
        if (trade == null) return NotFound();

        // Validación anti-spam
        var lastMessage = await db.TradeMessages
            .Where(m => m.TradeId == id && m.SenderUserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        if (lastMessage != null && (DateTime.UtcNow - lastMessage.CreatedAt).TotalSeconds < 2)
            return BadRequest("Espera un momento antes de enviar otro mensaje.");

        var message = new TradeMessage
        {
            TradeId = id,
            SenderUserId = userId,
            Text = text,
            CreatedAt = DateTime.UtcNow
        };

        db.TradeMessages.Add(message);
        await db.SaveChangesAsync();

        var msgDto = new 
        {
            Id = message.Id,
            TradeId = message.TradeId,
            SenderUserId = message.SenderUserId,
            Text = message.Text,
            CreatedAt = message.CreatedAt,
            SenderUserName = userName
        };

        await _hubContext.Clients.Group(id.ToString()).SendAsync("ReceiveMessage", msgDto);

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
                InitiatorUserId = t.RequesterUserId,
            
                // Mapeos visuales (Fotos, Nombres y Producto)
                RequesterAvatarUrl = t.RequesterUser.AvatarUrl,
                OwnerAvatarUrl = t.OwnerUser.AvatarUrl,
                RequesterName = t.RequesterUser.DisplayName,
                OwnerName = t.OwnerUser.DisplayName,
                ListingTitle = t.TargetListing.Title,
                ListingImageUrl = t.TargetListing.ImageUrl
            })
            .ToListAsync();

        return Ok(trades);
    }
}