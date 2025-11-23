namespace TruekAppAPI.DTO.Listing;

public class ListingDto
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    
    // --- DATOS DEL PRODUCTO ---
    public string? Description { get; set; }
    public decimal TrueCoinValue { get; set; }
    public bool IsPublished { get; set; }
    public string? ImageUrl { get; set; }
    
    // --- UBICACIÓN ---
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    // --- DATOS DEL VENDEDOR (Owner) ---
    public int OwnerUserId { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerAvatarUrl { get; set; } // <--- ¡Nuevo!
    public double OwnerRating { get; set; }     // <--- ¡Nuevo!
}