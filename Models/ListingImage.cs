namespace TruekAppAPI.Models;

public class ListingImage : BaseEntity
{
    public int ListingId { get; set; }
    public Listing Listing { get; set; } = default!;
    public string Url { get; set; } = default!;
}