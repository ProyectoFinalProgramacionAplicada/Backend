namespace TruekAppAPI.DTO.Listing;

public class ListingDto
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    
    // --- CAMPOS AÑADIDOS ---
    public string? Description { get; set; } // <-- Añadido
    public int OwnerUserId { get; set; } // <-- Añadido
    public string? OwnerName { get; set; } // <-- Añadido
    // --- FIN DE CAMPOS AÑADIDOS ---

    public decimal TrueCoinValue { get; set; }
    public bool IsPublished { get; set; }
    public string? ImageUrl { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}