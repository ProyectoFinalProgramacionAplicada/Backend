namespace TruekAppAPI.DTO.Listing;

public class ListingUpdateDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal? TrueCoinValue { get; set; }
    public string? ImageUrl { get; set; }
    public bool? IsPublished { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}