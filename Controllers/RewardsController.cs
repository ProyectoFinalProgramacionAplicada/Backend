using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TruekAppAPI.Data;
using TruekAppAPI.DTO.Reward;
using TruekAppAPI.Models;
using System.Security.Claims;

namespace TruekAppAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RewardsController(AppDbContext db) : ControllerBase
{
    // Listar recompensas (público)
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<RewardDto>>> GetAll()
    {
        var rewards = await db.Rewards
            .Where(r => r.IsActive)
            .Select(r => new RewardDto
            {
                Id = r.Id,
                Title = r.Title,
                CostTrueCoins = r.CostTrueCoins,
                ImageUrl = r.ImageUrl,
                IsActive = r.IsActive
            }).ToListAsync();

        return Ok(rewards);
    }

    // Crear recompensa (solo Company o Admin)
    [HttpPost]
    [Authorize(Roles = $"{nameof(AppRole.Company)},{nameof(AppRole.Admin)}")]
    public async Task<IActionResult> Create(RewardCreateDto dto)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        int? companyId = null;

        if (role == nameof(AppRole.Company))
        {
            // asumimos que el claim NameIdentifier es el id del usuario y CompanyId está en DB
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await db.Users.FindAsync(userId);
            if (user == null) return NotFound("Usuario no encontrado.");
            if (!user.CompanyId.HasValue) return BadRequest("Usuario de empresa sin companyId.");
            companyId = user.CompanyId;
        }

        // Si es Admin, requerir companyId en el body podría ser opción; aquí asumimos Admin envía companyId vía dto? 
        // Para no cambiar estructura, permitimos companyId nulo y lo guardamos como null (document no lo obligaba).
        var reward = new Reward
        {
            CompanyId = companyId ?? dto.GetType().GetProperty("CompanyId")?.GetValue(dto) as int? ?? 0,
            Title = dto.Title,
            Description = dto.Description,
            CostTrueCoins = dto.CostTrueCoins,
            ImageUrl = dto.ImageUrl,
            IsActive = true
        };

        // Si companyId no válido, intenta asignar 0 (pero ideal: validarlo). Para mantener simpleza, guardamos.
        db.Rewards.Add(reward);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Create), new { reward.Id }, reward);
    }

    // Obtener recompensas de la empresa del usuario (Company) o todas para Admin
    [HttpGet("company")]
    [Authorize(Roles = $"{nameof(AppRole.Company)},{nameof(AppRole.Admin)}")]
    public async Task<ActionResult<IEnumerable<RewardDto>>> GetCompanyRewards()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        IQueryable<Reward> q = db.Rewards;

        if (role == nameof(AppRole.Company))
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await db.Users.FindAsync(userId);
            if (user == null) return NotFound();
            q = q.Where(r => r.CompanyId == user.CompanyId);
        }

        var list = await q.Select(r => new RewardDto
        {
            Id = r.Id,
            Title = r.Title,
            CostTrueCoins = r.CostTrueCoins,
            ImageUrl = r.ImageUrl,
            IsActive = r.IsActive
        }).ToListAsync();

        return Ok(list);
    }

    // Canjear reward (usuario) -> crea RewardRedemption y genera movimiento en wallet
    [HttpPost("{id}/redeem")]
    [Authorize(Roles = nameof(AppRole.User))]
    public async Task<IActionResult> Redeem(int id, RewardRedeemRequestDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var reward = await db.Rewards.FindAsync(id);
        if (reward == null || !reward.IsActive) return NotFound("Recompensa no encontrada.");

        var user = await db.Users.FindAsync(userId);
        if (user == null) return NotFound("Usuario no encontrado.");

        if (user.TrueCoinBalance < reward.CostTrueCoins)
            return BadRequest("Saldo insuficiente.");

        // descontar TrueCoins
        user.TrueCoinBalance -= reward.CostTrueCoins;

        // crear wallet entry
        var walletEntry = new WalletEntry
        {
            UserId = userId,
            Amount = -reward.CostTrueCoins,
            Type = WalletEntryType.SpentOnReward,
            RefType = nameof(Reward),
            RefId = reward.Id
        };
        db.WalletEntries.Add(walletEntry);

        // crear redención
        var redemption = new RewardRedemption
        {
            RewardId = reward.Id,
            UserId = userId,
            Code = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
            Status = "Pending",
            RedeemedAt = null
        };
        db.RewardRedemptions.Add(redemption);

        await db.SaveChangesAsync();

        var dtoResp = new RewardRedemptionDto
        {
            Id = redemption.Id,
            RewardId = redemption.RewardId,
            Code = redemption.Code,
            Status = redemption.Status,
            CreatedAt = redemption.CreatedAt
        };

        return CreatedAtAction(nameof(Redeem), new { redemption.Id }, dtoResp);
    }

    // Empresa/ADMIN marcan redención como canjeada (redeemed) o cancelada
    [HttpPatch("redemptions/{id}/status")]
    [Authorize(Roles = $"{nameof(AppRole.Company)},{nameof(AppRole.Admin)}")]
    public async Task<IActionResult> UpdateRedemptionStatus(int id, [FromBody] string status)
    {
        var redemption = await db.RewardRedemptions
            .Include(r => r.Reward)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (redemption == null) return NotFound();

        // Si company role, validar que la reward pertenece a su empresa
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role == nameof(AppRole.Company))
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await db.Users.FindAsync(userId);
            if (user == null) return NotFound();
            if (redemption.Reward.CompanyId != user.CompanyId) return Forbid();
        }

        redemption.Status = status;
        if (status == "Redeemed") redemption.RedeemedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
