using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TruekAppAPI.Data;
using System.Security.Claims;
using TruekAppAPI.DTO.Auth;
using TruekAppAPI.Services; // Importante para IPasswordHasher

namespace TruekAppAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
// INYECCIÓN DE DEPENDENCIAS: Agregamos IPasswordHasher al constructor primario
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

        user.DisplayName = dto.DisplayName;
        user.Phone = dto.Phone; // Ahora validado por el DTO con Regex
        
        await db.SaveChangesAsync();
        
        // Retornamos el objeto user actualizado para refrescar la UI
        return Ok(new { 
            message = "Perfil actualizado", 
            user = new { user.DisplayName, user.Phone, user.AvatarUrl } 
        });
    }

    // 2. NUEVO: Cambiar Contraseña
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();

        // Verificamos la contraseña anterior
        if (!passwordHasher.VerifyPassword(user.PasswordHash,dto.OldPassword))
        {
            return BadRequest(new { message = "La contraseña anterior es incorrecta." });
        }

        // Hasheamos y guardamos la nueva
        user.PasswordHash = passwordHasher.HashPassword(dto.NewPassword);
        await db.SaveChangesAsync();

        return Ok(new { message = "Contraseña actualizada correctamente." });
    }

    // 3. Subir Avatar (Ya existía, lo ajustamos para ser más robusto)
    [HttpPost("me/avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Archivo inválido.");

        // Validar que sea imagen
        if (!file.ContentType.StartsWith("image/"))
            return BadRequest("El archivo debe ser una imagen.");

        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();

        // Crear directorio si no existe
        var folderName = Path.Combine("wwwroot", "uploads", "avatars"); // Usamos wwwroot para servir estáticos
        var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
        Directory.CreateDirectory(pathToSave);

        // Nombre único
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var fullPath = Path.Combine(pathToSave, fileName);

        using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        // Guardamos la URL relativa accesible desde web
        user.AvatarUrl = $"/uploads/avatars/{fileName}";
        await db.SaveChangesAsync();

        return Ok(new { avatarUrl = user.AvatarUrl });
    }
}