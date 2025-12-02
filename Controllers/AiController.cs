using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruekAppAPI.Services;

namespace TruekAppAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Solo usuarios logueados pueden usar la IA
public class AiController(IAiService aiService) : ControllerBase
{
    [HttpPost("enhance")]
    public async Task<IActionResult> EnhanceDescription([FromBody] TextRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Length < 5)
            return BadRequest("El texto es muy corto para ser mejorado.");

        var enhanced = await aiService.EnhanceDescriptionAsync(request.Text);
        return Ok(new { text = enhanced });
    }
}

public class TextRequestDto
{
    public string Text { get; set; } = string.Empty;
}