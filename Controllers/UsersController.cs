using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TruekAppAPI.Data;
using System.Security.Claims;
using TruekAppAPI.DTO.Auth;
using TruekAppAPI.Services;

namespace TruekAppAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(AppDbContext db, IPasswordHasher passwordHasher) : ControllerBase
{
    // 1. Actualizar Datos Básicos (Nombre y Teléfono)
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UserUpdateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();

        // ✅ FIX: Validación con null-check
        var phoneExists = await db.Users
            .AnyAsync(u => u.Phone != null && u.Phone == dto.Phone && u.Id != id);
    
        if (phoneExists)
        {
            return BadRequest(new { message = "Este número de teléfono ya está registrado." });
        }

        user.DisplayName = dto.DisplayName;
        user.Phone = dto.Phone;
        
        await db.SaveChangesAsync();
        
        return Ok(new { 
            message = "Perfil actualizado", 
            user = new { user.DisplayName, user.Phone, user.AvatarUrl } 
        });
    }

    // 2. Cambiar Contraseña
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (!passwordHasher.VerifyPassword(user.PasswordHash, dto.OldPassword))
        {
            return BadRequest(new { message = "La contraseña anterior es incorrecta." });
        }

        user.PasswordHash = passwordHasher.HashPassword(dto.NewPassword);
        await db.SaveChangesAsync();

        return Ok(new { message = "Contraseña actualizada correctamente." });
    }

    // 3. Subir Avatar
    [HttpPost("me/avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Archivo inválido.");

        if (!file.ContentType.StartsWith("image/"))
            return BadRequest("El archivo debe ser una imagen.");

        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();

        var folderName = Path.Combine("wwwroot", "uploads", "avatars");
        var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
        Directory.CreateDirectory(pathToSave);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var fullPath = Path.Combine(pathToSave, fileName);

        using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        user.AvatarUrl = $"/uploads/avatars/{fileName}";
        await db.SaveChangesAsync();

        return Ok(new { avatarUrl = user.AvatarUrl });
    }
}