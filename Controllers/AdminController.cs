using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TruekAppAPI.Data;
using TruekAppAPI.DTO.Admin;
using TruekAppAPI.Models;

namespace TruekAppAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }

    // ==========================================
    // HELPER: Verificar que el usuario actual es Admin
    // ==========================================
    private bool IsAdmin()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        return role == nameof(AppRole.Admin);
    }

    // ==========================================
    // GET /api/Admin/users
    // ==========================================
    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetAllUsers()
    {
        if (!IsAdmin())
            return Forbid("Solo administradores pueden acceder a este recurso.");

        var users = await _db.Users
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                DisplayName = u.DisplayName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                TrueCoinBalance = u.TrueCoinBalance
            })
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return Ok(users);
    }

    // ==========================================
    // GET /api/Admin/trades
    // ⚠️ CORRECCIÓN: Quitar .Include() cuando usas .Select()
    // ==========================================
    [HttpGet("trades")]
    public async Task<ActionResult<IEnumerable<AdminTradeDto>>> GetAllTrades()
    {
        if (!IsAdmin())
            return Forbid("Solo administradores pueden acceder a este recurso.");

        // 🔧 SIN Include - EF Core lo hace automáticamente en el Select
        var trades = await _db.Trades
            .Select(t => new AdminTradeDto
            {
                Id = t.Id,
                TargetListingId = t.TargetListingId,
                RequesterUserId = t.RequesterUserId,
                OwnerUserId = t.OwnerUserId,
                Status = t.Status,
                CreatedAt = t.CreatedAt,
                CompletedAt = t.CompletedAt,
                TargetListingTitle = t.TargetListing.Title,
                TargetListingValue = t.TargetListing.TrueCoinValue
            })
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(trades);
    }

    // ==========================================
    // GET /api/Admin/stats
    // ==========================================
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetSystemStats()
    {
        if (!IsAdmin())
            return Forbid("Solo administradores pueden acceder a este recurso.");

        var totalUsers = await _db.Users.CountAsync();
        var activeUsers = await _db.Users.CountAsync(u => u.IsActive);
        var totalTrades = await _db.Trades.CountAsync();
        var completedTrades = await _db.Trades.CountAsync(t => t.Status == TradeStatus.Completed);
        var totalListings = await _db.Listings.CountAsync();
        var publishedListings = await _db.Listings.CountAsync(l => l.IsPublished && l.IsAvailable);

        var completionRate = totalTrades > 0 
            ? Math.Round((double)completedTrades / totalTrades * 100, 2) 
            : 0;

        // ✅ CORRECCIÓN: Cast directo a double, sin ??
        var tradesWithDuration = await _db.Trades
            .Where(t => t.Status == TradeStatus.Completed && t.CompletedAt.HasValue)
            .Select(t => new { 
                Duration = EF.Functions.DateDiffHour(t.CreatedAt, t.CompletedAt!.Value)
            })
            .ToListAsync();

        var avgClosureTime = tradesWithDuration.Any() 
            ? Math.Round(tradesWithDuration.Average(t => (double)t.Duration), 2)
            : 0;

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var newUsersLast7Days = await _db.Users
            .CountAsync(u => u.CreatedAt >= sevenDaysAgo);

        return Ok(new
        {
            totalUsers,
            activeUsers,
            inactiveUsers = totalUsers - activeUsers,
            newUsersLast7Days,
            totalTrades,
            completedTrades,
            cancelledTrades = await _db.Trades.CountAsync(t => t.Status == TradeStatus.Cancelled),
            completionRate,
            avgClosureTimeHours = avgClosureTime,
            totalListings,
            publishedListings,
            generatedAt = DateTime.UtcNow
        });
    }

    // ==========================================
    // GET /api/Admin/wallet-activity
    // ⚠️ CORRECCIÓN: Quitar .Include() cuando usas .Select()
    // ==========================================
    [HttpGet("wallet-activity")]
    public async Task<ActionResult<object>> GetWalletActivity([FromQuery] int limit = 50)
    {
        if (!IsAdmin())
            return Forbid("Solo administradores pueden acceder a este recurso.");

        // 🔧 SIN Include - EF Core lo hace automáticamente en el Select
        var entries = await _db.WalletEntries
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .Select(e => new
            {
                e.Id,
                e.UserId,
                UserName = e.User.DisplayName ?? e.User.Email,
                e.Amount,
                e.Type,
                e.RefType,
                e.RefId,
                e.CreatedAt
            })
            .ToListAsync();

        return Ok(entries);
    }
}