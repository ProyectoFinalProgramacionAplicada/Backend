using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TruekAppAPI.Data;
using TruekAppAPI.DTO.Settings;
using TruekAppAPI.Models;

namespace TruekAppAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController(AppDbContext db) : ControllerBase
{
    // Obtener todas las settings (público)
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SettingDto>>> GetAll()
    {
        var settings = await db.Settings
            .Select(s => new SettingDto { Key = s.Key, Value = s.Value })
            .ToListAsync();
        return Ok(settings);
    }

    // Obtener setting por clave
    [HttpGet("{key}")]
    [AllowAnonymous]
    public async Task<ActionResult<SettingDto>> Get(string key)
    {
        var s = await db.Settings.FindAsync(key);
        if (s == null) return NotFound();
        return new SettingDto { Key = s.Key, Value = s.Value };
    }

    // Crear / actualizar setting (solo Admin)
    [HttpPost]
    [Authorize(Roles = nameof(AppRole.Admin))]
    public async Task<IActionResult> Upsert([FromBody] SettingDto dto)
    {
        var setting = await db.Settings.FindAsync(dto.Key);
        if (setting == null)
        {
            db.Settings.Add(new Setting { Key = dto.Key, Value = dto.Value });
        }
        else
        {
            setting.Value = dto.Value;
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    // Borrar setting (solo Admin)
    [HttpDelete("{key}")]
    [Authorize(Roles = nameof(AppRole.Admin))]
    public async Task<IActionResult> Delete(string key)
    {
        var setting = await db.Settings.FindAsync(key);
        if (setting == null) return NotFound();
        db.Settings.Remove(setting);
        await db.SaveChangesAsync();
        return NoContent();
    }
}