using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TruekAppAPI.Data;
using System.Security.Claims;
using TruekAppAPI.DTO.Auth;

namespace TruekAppAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(AppDbContext db) : ControllerBase
{
    // MÉTODO CORREGIDO: Tipado fuerte y validaciones
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UserUpdateDto dto)
    {
        // 1. Validación de entrada
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // 2. Obtener ID del usuario autenticado
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(idClaim)) return Unauthorized();
        var id = int.Parse(idClaim);

        // 3. Buscar en BD
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound("Usuario no encontrado.");

        // 4. Actualización segura (Solo campos permitidos)
        user.DisplayName = dto.DisplayName;
        user.Phone = dto.Phone;

        // 5. Guardar cambios
        await db.SaveChangesAsync();

        // Retornamos los datos nuevos para que el Frontend se actualice al instante
        return Ok(new 
        { 
            message = "Perfil actualizado correctamente", 
            user = new { user.DisplayName, user.Phone } 
        });
    }

    // MÉTODO EXISTENTE: Se mantiene igual, solo limpieza menor
    [HttpPost("me/avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Archivo inválido.");

        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(idClaim)) return Unauthorized();
        var id = int.Parse(idClaim);

        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();

        // Aseguramos que el directorio exista
        var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "avatars");
        if (!Directory.Exists(uploadFolder))
        {
            Directory.CreateDirectory(uploadFolder);
        }

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadFolder, fileName);

        using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        // Ruta relativa para guardar en BD (ajustada a convención web)
        user.AvatarUrl = $"/uploads/avatars/{fileName}";
        
        await db.SaveChangesAsync();

        return Ok(new { avatarUrl = user.AvatarUrl });
    }
}