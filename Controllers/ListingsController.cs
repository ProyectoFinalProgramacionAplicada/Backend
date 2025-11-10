using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TruekAppAPI.Data;
using TruekAppAPI.DTO.Listing;
using TruekAppAPI.Models;
using System.Security.Claims;
using TruekAppAPI.Services;     // AÑADIDO: Para IGeoService
using NetTopologySuite.Geometries; // AÑADIDO: Para Point

namespace TruekAppAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
// MODIFICADO: Inyectamos el IGeoService junto al AppDbContext
public class ListingsController(AppDbContext db, IGeoService geoService) : ControllerBase
{
    [HttpGet("catalog")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ListingDto>>> GetCatalog(
        [FromQuery] int? ownerId,
        [FromQuery] string? q,
        [FromQuery] decimal? minValue,
        [FromQuery] decimal? maxValue)
    {
        var listings = db.Listings
            .Include(l => l.OwnerUser)
            .Where(l => l.IsPublished && l.IsAvailable);

        if (ownerId.HasValue)
            listings = listings.Where(l => l.OwnerUserId == ownerId);

        if (!string.IsNullOrEmpty(q))
            listings = listings.Where(l => l.Title.Contains(q));

        if (minValue.HasValue)
            listings = listings.Where(l => l.TrueCoinValue >= minValue);

        if (maxValue.HasValue)
            listings = listings.Where(l => l.TrueCoinValue <= maxValue);

        // MODIFICADO: Mapeamos la ubicación (Point) a Lat/Lng
        var result = await listings.Select(l => new ListingDto
        {
            Id = l.Id,
            Title = l.Title,
            TrueCoinValue = l.TrueCoinValue,
            ImageUrl = l.ImageUrl,
            IsPublished = l.IsPublished,
            // AÑADIDO: Mapear desde el objeto Location
            // ¡Recuerda! Y es Latitud, X es Longitud
            Latitude = l.Location.Y, 
            Longitude = l.Location.X
        }).ToListAsync();

        return Ok(result);
    }

    // --- AÑADIDO: NUEVO ENDPOINT "NEARBY" ---
    [HttpGet("nearby")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ListingDto>>> GetNearbyListings(
        [FromQuery] double latitude, 
        [FromQuery] double longitude, 
        [FromQuery] double radiusInKm = 5)
    {
        // 1. Crea el punto de ubicación del usuario usando el servicio
        var userLocation = geoService.CreatePoint(latitude, longitude);

        // 2. Convierte el radio de Km a Metros (STDistance usa metros)
        var radiusInMeters = radiusInKm * 1000;

        // 3. Consulta optimizada a la base de datos
        var nearbyListings = await db.Listings
            .Where(l => l.IsPublished && l.IsAvailable)
            // Esta función se traduce a SQL 'STDistance'
            .Where(l => l.Location.IsWithinDistance(userLocation, radiusInMeters)) 
            .Select(l => new ListingDto
            {
                Id = l.Id,
                Title = l.Title,
                TrueCoinValue = l.TrueCoinValue,
                ImageUrl = l.ImageUrl,
                IsPublished = l.IsPublished,
                Latitude = l.Location.Y,
                Longitude = l.Location.X
            })
            .ToListAsync();

        return Ok(nearbyListings);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(ListingCreateDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var listing = new Listing
        {
            OwnerUserId = userId,
            Title = dto.Title,
            Description = dto.Description,
            TrueCoinValue = dto.TrueCoinValue,
            ImageUrl = dto.ImageUrl,
            
            // MODIFICADO: Usamos el GeoService para crear el 'Point'
            Location = geoService.CreatePoint(dto.Latitude, dto.Longitude),
            
            // ELIMINADO: Ya no usamos Lat/Lng separados
            // Lat = dto.Lat, 
            // Lng = dto.Lng,
            
            IsPublished = true
            // IsAvailable = true (Valor por defecto)
        };

        db.Listings.Add(listing);
        await db.SaveChangesAsync();
        
        // (Mejora opcional): Devolver un DTO en lugar del modelo completo
        // Por ahora lo dejamos como lo tenías.
        return CreatedAtAction(nameof(Create), new { listing.Id }, listing);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ListingUpdateDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var listing = await db.Listings.FindAsync(id);
        if (listing == null) return NotFound();
        if (listing.OwnerUserId != userId) return Forbid();

        listing.Title = dto.Title ?? listing.Title;
        listing.Description = dto.Description ?? listing.Description;
        listing.TrueCoinValue = dto.TrueCoinValue ?? listing.TrueCoinValue;
        listing.ImageUrl = dto.ImageUrl ?? listing.ImageUrl;
        listing.IsPublished = dto.IsPublished ?? listing.IsPublished;

        // AÑADIDO: Permitir actualización de ubicación
        if (dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            listing.Location = geoService.CreatePoint(dto.Latitude.Value, dto.Longitude.Value);
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var listing = await db.Listings.FindAsync(id);
        if (listing == null) return NotFound();
        if (listing.OwnerUserId != userId) return Forbid();

        db.Listings.Remove(listing);
        await db.SaveChangesAsync();
        return NoContent();
    }
}