using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TruekAppAPI.Data;
using TruekAppAPI.DTO.Listing;
using TruekAppAPI.Models;
using System.Security.Claims;
using TruekAppAPI.Services;     
using NetTopologySuite.Geometries; 

namespace TruekAppAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
// MODIFICADO: Inyectamos el nuevo IStorageService para manejar archivos
public class ListingsController(
    AppDbContext db, 
    IGeoService geoService, 
    IStorageService storageService // <-- AÑADIDO
) : ControllerBase
{
    // ==================================================================
    // == MÉTODOS GET (Sin cambios)
    // ==================================================================

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

        var result = await listings.Select(l => new ListingDto
        {
            Id = l.Id,
            Title = l.Title,
            Description = l.Description,
            TrueCoinValue = l.TrueCoinValue,
            ImageUrl = l.ImageUrl,
            IsPublished = l.IsPublished,
            Latitude = l.Location.Y,
            Longitude = l.Location.X,
    
            // Mapeo del Vendedor
            OwnerUserId = l.OwnerUserId,
            OwnerName = l.OwnerUser.DisplayName,
            OwnerAvatarUrl = l.OwnerUser.AvatarUrl, // <--- Mapeamos la URL
            OwnerRating = db.UserReviews
                .Where(r => r.ToUserId == l.OwnerUserId)
                .Select(r => (double?)r.Rating) // Cast a nullable para evitar excepción si está vacío
                .Average() ?? 0.0 // Si es null (sin reviews), devuelve 0.0
        }).ToListAsync();

        return Ok(result);
    }

    [HttpGet("nearby")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ListingDto>>> GetNearbyListings(
        [FromQuery] double latitude, 
        [FromQuery] double longitude, 
        [FromQuery] double radiusInKm = 25)
    {
        // Validar parámetros
        if (latitude < -90 || latitude > 90)
            return BadRequest("Latitud inválida.");
        if (longitude < -180 || longitude > 180)
            return BadRequest("Longitud inválida.");
        if (radiusInKm <= 0 || radiusInKm > 100)
            return BadRequest("El radio debe estar entre 1 y 100 km.");
    
        var userLocation = geoService.CreatePoint(latitude, longitude);
        var radiusInMeters = radiusInKm * 1000;
    
        var nearbyListings = await db.Listings
            .Where(l => l.IsPublished && l.IsAvailable)
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

    
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ListingDto>> GetListingById(int id)
    {
        var listing = await db.Listings
            .Include(l => l.OwnerUser) 
            .Where(l => l.Id == id && l.IsPublished)
            .Select(l => new ListingDto
            {
                Id = l.Id,
                Title = l.Title,
                Description = l.Description,
                TrueCoinValue = l.TrueCoinValue,
                ImageUrl = l.ImageUrl,
                IsPublished = l.IsPublished,
                Latitude = l.Location.Y,
                Longitude = l.Location.X,
    
                // Mapeo del Vendedor
                OwnerUserId = l.OwnerUserId,
                OwnerName = l.OwnerUser.DisplayName,
                OwnerAvatarUrl = l.OwnerUser.AvatarUrl, // <--- Mapeamos la URL
                OwnerRating = db.UserReviews
                    .Where(r => r.ToUserId == l.OwnerUserId)
                    .Select(r => (double?)r.Rating) // Cast a nullable para evitar excepción si está vacío
                    .Average() ?? 0.0 // Si es null (sin reviews), devuelve 0.0
            })
            .FirstOrDefaultAsync();

        if (listing == null) return NotFound("Publicación no encontrada.");

        return Ok(listing);
    }

    // ==================================================================
    // == MÉTODOS POST/PUT/DELETE (Modificados para archivos)
    // ==================================================================

    [HttpPost]
    [Authorize]
    // MODIFICADO: Añadido [FromForm] para aceptar 'multipart/form-data' (archivos)
    public async Task<IActionResult> Create([FromForm] ListingCreateDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // --- AÑADIDO: Lógica de carga de archivos ---
        if (dto.ImageFile == null || dto.ImageFile.Length == 0)
        {
            return BadRequest("No se ha proporcionado un archivo de imagen.");
        }

        string imageUrl;
        try
        {
            // Usamos el servicio para subir el archivo. "listings" es el nombre del contenedor.
            imageUrl = await storageService.UploadFileAsync(dto.ImageFile, "listings");
        }
        catch (Exception ex)
        {
            // Manejo de error si la subida a Azure falla
            return StatusCode(500, $"Error interno al subir la imagen: {ex.Message}");
        }
        // --- FIN DE LA NUEVA LÓGICA ---

        var listing = new Listing
        {
            OwnerUserId = userId,
            Title = dto.Title,
            Description = dto.Description,
            TrueCoinValue = dto.TrueCoinValue,
            Lat = dto.Latitude,
            Lng = dto.Longitude,
            
            // MODIFICADO: Usamos la URL devuelta por el servicio de almacenamiento
            ImageUrl = imageUrl, 
            
            Location = geoService.CreatePoint(dto.Latitude, dto.Longitude),
            IsPublished = true
        };

        db.Listings.Add(listing);
        await db.SaveChangesAsync();
        
        // CONVENCIÓN: Devolver un DTO en lugar de la entidad de DB
        var responseDto = new ListingDto 
        {
            Id = listing.Id,
            Title = listing.Title,
            Description = listing.Description,
            TrueCoinValue = listing.TrueCoinValue,
            ImageUrl = listing.ImageUrl,
            IsPublished = listing.IsPublished,
            Latitude = listing.Location.Y,
            Longitude = listing.Location.X,
            OwnerUserId = listing.OwnerUserId
        };

        return CreatedAtAction(nameof(GetListingById), new { id = listing.Id }, responseDto);
    }

    [HttpPut("{id}")]
    // MODIFICADO: Añadido [FromForm]
    public async Task<IActionResult> Update(int id, [FromForm] ListingUpdateDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var listing = await db.Listings.FindAsync(id);
        if (listing == null) return NotFound();
        if (listing.OwnerUserId != userId) return Forbid();

        // --- AÑADIDO: Lógica condicional de carga de archivos ---
        if (dto.ImageFile != null && dto.ImageFile.Length > 0)
        {
            try
            {
                // Práctica profesional: Eliminar la imagen antigua antes de subir la nueva
                if (!string.IsNullOrEmpty(listing.ImageUrl))
                {
                    await storageService.DeleteFileAsync(listing.ImageUrl);
                }

                // Subir la nueva imagen
                listing.ImageUrl = await storageService.UploadFileAsync(dto.ImageFile, "listings");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al actualizar la imagen: {ex.Message}");
            }
        }
        // --- FIN DE LA NUEVA LÓGICA ---

        listing.Title = dto.Title ?? listing.Title;
        listing.Description = dto.Description ?? listing.Description;
        listing.TrueCoinValue = dto.TrueCoinValue ?? listing.TrueCoinValue;
        listing.IsPublished = dto.IsPublished ?? listing.IsPublished;

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

        // --- AÑADIDO: Borrar el archivo asociado de Blob Storage ---
        if (!string.IsNullOrEmpty(listing.ImageUrl))
        {
            try
            {
                await storageService.DeleteFileAsync(listing.ImageUrl);
            }
            catch (Exception ex)
            {
                // No bloquear la eliminación de la DB si falla el borrado del archivo,
                // pero se debe registrar (logging).
                Console.WriteLine($"Error al eliminar archivo de storage: {ex.Message}");
            }
        }
        // --- FIN DE LA NUEVA LÓGICA ---

        db.Listings.Remove(listing);
        await db.SaveChangesAsync();
        return NoContent();
    }
}