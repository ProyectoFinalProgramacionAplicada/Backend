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
public class UsersController(
    AppDbContext db, 
    IPasswordHasher passwordHasher,
    IStorageService storageService // <--- NUEVA INYECCIÓN
) : ControllerBase
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
    
// 3. Subir Avatar (Versión Cloud / Azure Blob Storage)
    [HttpPost("me/avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        // 1. Validaciones básicas de entrada
        if (file is null || file.Length == 0)
            return BadRequest("Archivo inválido.");

        if (!file.ContentType.StartsWith("image/"))
            return BadRequest("El archivo debe ser una imagen.");

        // 2. Obtener el usuario autenticado
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();

        try 
        {
            // 3. (Opcional) Borrar avatar anterior si existe para no acumular basura en la nube
            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                // No esperamos (await) obligatoriamente el borrado para no retrasar la respuesta, 
                // o puedes usar await si prefieres asegurar la limpieza.
                await storageService.DeleteFileAsync(user.AvatarUrl);
            }

            // 4. Subir el nuevo archivo a Azure
            // "avatars" es el nombre del contenedor que se creará en tu Storage Account
            string newAvatarUrl = await storageService.UploadFileAsync(file, "avatars");

            // 5. Actualizar la base de datos con la URL pública de la nube
            user.AvatarUrl = newAvatarUrl;
            await db.SaveChangesAsync();

            return Ok(new { avatarUrl = user.AvatarUrl });
        }
        catch (Exception ex)
        {
            // Manejo de errores profesional: No le damos el stack trace al usuario, pero lo logueamos internamente
            Console.WriteLine($"Error subiendo avatar: {ex.Message}");
            return StatusCode(500, "Ocurrió un error al procesar la imagen.");
        }
    }
}