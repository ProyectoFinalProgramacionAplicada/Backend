using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TruekAppAPI.Data;
using System.Security.Claims;

namespace TruekAppAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(AppDbContext db) : ControllerBase
{
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] dynamic dto)
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.DisplayName = dto?.displayName ?? user.DisplayName;
        user.Phone = dto?.phone ?? user.Phone;
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("me/avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Archivo inválido.");

        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();

        var path = $"uploads/avatars/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        Directory.CreateDirectory("uploads/avatars");
        using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);
        user.AvatarUrl = "/" + path;
        await db.SaveChangesAsync();

        return Created(user.AvatarUrl, null);
    }
}