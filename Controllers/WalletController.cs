using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TruekAppAPI.Data;
using TruekAppAPI.DTO.Wallet;
using TruekAppAPI.Models;
using TruekAppAPI.Services;
using System.Security.Claims;

namespace TruekAppAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWalletService _walletService;

    public WalletController(AppDbContext db, IWalletService walletService)
    {
        _db = db;
        _walletService = walletService;
    }

    // ==========================================
    // GET /api/Wallet/me
    // Obtener balance y últimos movimientos
    // ==========================================
    [HttpGet("me")]
    public async Task<ActionResult<WalletBalanceDto>> GetMyWallet()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var entries = await _db.WalletEntries
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(20)
            .Select(e => new WalletEntryDto
            {
                Id = e.Id,
                Amount = e.Amount,
                Type = e.Type,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();

        var dto = new WalletBalanceDto
        {
            Balance = user.TrueCoinBalance,
            Entries = entries
        };

        return Ok(dto);
    }

    // ==========================================
    // POST /api/Wallet/adjust
    // Ajuste manual de saldo (admin)
    // ==========================================
    [HttpPost("adjust")]
    [Authorize] // aquí podrías poner [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdjustBalance([FromBody] AdjustBalanceDto payload)
    {
        int userId = payload.UserId;
        decimal amount = payload.Amount;
        string? reason = payload.Reason;

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.TrueCoinBalance += amount;

        var entry = new WalletEntry
        {
            UserId = userId,
            Amount = amount,
            Type = WalletEntryType.AdminAdjustment,
            RefType = reason ?? "AdminAdjustment",
            RefId = null
        };

        _db.WalletEntries.Add(entry);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ==========================================
    // POST /api/Wallet/transfer
    // Transferencia P2P interna de TrueCoins
    // ==========================================
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] WalletTransferRequestDto payload)
    {
        if (payload.Amount <= 0)
            return BadRequest("Amount must be greater than 0");

        var fromUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Nos aseguramos de usar decimal aunque el DTO viniera en double
        decimal amount = (decimal)payload.Amount;

        await _walletService.TransferAsync(
            fromUserId,
            payload.ToUserId,
            amount,
            payload.Reference
        );

        return NoContent();
    }
}
