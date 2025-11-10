namespace TruekAppAPI.Models;
using NetTopologySuite.Geometries;

public class Listing : BaseEntity
{
    public int Id { get; set; }
    public Point Location { get; set; }
    public int OwnerUserId { get; set; }
    public User OwnerUser { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal TrueCoinValue { get; set; }
    public string? ImageUrl { get; set; }
    public string? Address { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public bool IsPublished { get; set; }
    public bool IsAvailable { get; set; } = true;
    public ICollection<ListingImage> Images { get; set; } = [];
}