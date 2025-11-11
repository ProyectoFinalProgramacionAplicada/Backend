namespace TruekAppBackend.DTO.Listing;

public class ListingDto
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public decimal TrueCoinValue { get; set; }
    public bool IsPublished { get; set; }
    public string? ImageUrl { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}