namespace TruekAppAPI.Models;

public class Trade : BaseEntity
{
    public int RequesterUserId { get; set; }
    public User RequesterUser { get; set; } = default!;
    public int OwnerUserId { get; set; }
    public User OwnerUser { get; set; } = default!;
    public int TargetListingId { get; set; }
    public Listing TargetListing { get; set; } = default!;
    public int? OfferedListingId { get; set; }
    public Listing? OfferedListing { get; set; }
    public TradeStatus Status { get; set; } = TradeStatus.Pending;
    public string? Message { get; set; }
    public string? ExchangeAddress { get; set; }
    public double? ExchangeLat { get; set; }
    public double? ExchangeLng { get; set; }
    public ICollection<TradeMessage> Messages { get; set; } = [];
    public double? OfferedTrueCoins { get; set; }   // TrueCoins que ofrece el solicitante para complementar
    public double? RequestedTrueCoins { get; set; } // TrueCoins que pide el solicitante para compensar diferencia
    
    public DateTime? CompletedAt { get; set; }
}
