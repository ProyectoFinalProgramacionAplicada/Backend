using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TruekAppAPI.Data;
using TruekAppAPI.DTO.Wallet;
using TruekAppAPI.Models;
using System.Security.Claims;

namespace TruekAppAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController(AppDbContext db) : ControllerBase
{
    // Obtener balance y últimos movimientos del usuario autenticado
    [HttpGet("me")]
    public async Task<ActionResult<WalletBalanceDto>> GetMyWallet()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var entries = await db.WalletEntries
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(20)
            .Select(e => new WalletEntryDto
            {
                Id = e.Id,
                Amount = e.Amount,
                Type = e.Type,
                CreatedAt = e.CreatedAt
            }).ToListAsync();

        var dto = new WalletBalanceDto
        {
            Balance = user.TrueCoinBalance,
            Entries = entries
        };

        return Ok(dto);
    }

    // Ajuste manual de saldo (solo Admin)
    [HttpPost("adjust")]
    [Authorize]
    public async Task<IActionResult> AdjustBalance([FromBody] AdjustBalanceDto payload)
    {
        int userId = payload.UserId;
        decimal amount = payload.Amount;
        string reason = payload.Reason;

        var user = await db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.TrueCoinBalance += amount;

        var entry = new WalletEntry
        {
            UserId = userId,
            Amount = amount,
            Type = WalletEntryType.AdminAdjustment,
            RefType = "AdminAdjustment",
            RefId = null
        };

        db.WalletEntries.Add(entry);
        await db.SaveChangesAsync();

        return NoContent();
    }

}
