using System.ComponentModel.DataAnnotations;

namespace TruekAppBackend.DTO.Listing;

public class ListingCreateDto
{
    [Required] public string Title { get; set; } = default!;
    public string? Description { get; set; }
    [Range(1, double.MaxValue)] public decimal TrueCoinValue { get; set; }
    [Required] public string ImageUrl { get; set; } = default!;
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}