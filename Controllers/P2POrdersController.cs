using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruekAppAPI.DTO.P2P;
using TruekAppAPI.Services;

namespace TruekAppAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class P2POrdersController : ControllerBase
{
    private readonly IP2POrderService _service;

    public P2POrdersController(IP2POrderService service)
    {
        _service = service;
    }

    // ==========================================
    // POST /api/P2POrders
    // Crear una nueva orden P2P
    // ==========================================
    [HttpPost]
    public async Task<ActionResult<P2POrderDto>> Create([FromBody] P2POrderCreateDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _service.CreateAsync(userId, dto);
        return Ok(result);
    }

    // ==========================================
    // GET /api/P2POrders/{id}
    // Obtener detalle de una orden específica
    // ==========================================
    [HttpGet("{id}")]
    [AllowAnonymous] // quitar si quieres restringir
    public async Task<ActionResult<P2POrderDto>> GetById(int id)
    {
        var order = await _service.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        return Ok(order);
    }

    // ==========================================
    // GET /api/P2POrders/book
    // Libro de órdenes públicas
    // ==========================================
    [HttpGet("book")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<P2POrderDto>>> GetOrderBook()
    {
        var result = await _service.GetOrderBookAsync();
        return Ok(result);
    }

    // ==========================================
    // PATCH /api/P2POrders/{id}/take
    // Tomar una orden
    // ==========================================
    [HttpPatch("{id}/take")]
    public async Task<ActionResult<P2POrderDto>> Take(int id)
    {
        var userId = GetCurrentUserId();

        try
        {
            var result = await _service.TakeAsync(id, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            // 400 con el mensaje de dominio (por ejemplo "Insufficient TrueCoins balance...")
            return BadRequest(ex.Message);
        }
    }


    // ==========================================
    // PATCH /api/P2POrders/{id}/paid
    // Marcar pago (comprador fiat)
    // ==========================================
    [HttpPatch("{id}/paid")]
    public async Task<ActionResult<P2POrderDto>> MarkPaid(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _service.MarkPaidAsync(id, userId);
        return Ok(result);
    }

    // ==========================================
    // PATCH /api/P2POrders/{id}/release
    // Vendedor libera los TrueCoins
    // ==========================================
    [HttpPatch("{id}/release")]
    public async Task<ActionResult<P2POrderDto>> Release(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _service.ReleaseAsync(id, userId);
        return Ok(result);
    }

    // ==========================================
    // PATCH /api/P2POrders/{id}/cancel
    // Cancelar (creador)
    // ==========================================
    [HttpPatch("{id}/cancel")]
    public async Task<ActionResult<P2POrderDto>> Cancel(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _service.CancelAsync(id, userId);
        return Ok(result);
    }
    
    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<P2POrderDto>>> GetMyOrders()
    {
        var userId = GetCurrentUserId();
        var result = await _service.GetOrdersForUserAsync(userId);
        return Ok(result);
    }

    // ==========================================
    // Helper
    // ==========================================
    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? throw new InvalidOperationException("User id claim not found");
        return int.Parse(claim.Value);
    }
}
