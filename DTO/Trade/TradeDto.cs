using TruekAppAPI.Models;

namespace TruekAppAPI.DTO.Trade;

public class TradeDto
{
    public int Id { get; set; }
    public int RequesterUserId { get; set; }
    public int OwnerUserId { get; set; }
    public int TargetListingId { get; set; }
    public int? OfferedListingId { get; set; }
    public TradeStatus Status { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public double? OfferedTrueCoins { get; set; }
    public double? RequestedTrueCoins { get; set; }

    public int ListingOwnerId { get; set; }   // Vendedor
    public int InitiatorUserId { get; set; }  // Comprador

    public string? RequesterAvatarUrl { get; set; }
    public string? OwnerAvatarUrl { get; set; }
    public string? RequesterName { get; set; }
    public string? OwnerName { get; set; } 
    public string? ListingTitle { get; set; }
    public string? ListingImageUrl { get; set; }

    // 👇 NUEVO: quién hizo la última oferta / contraoferta
    public int? LastOfferByUserId { get; set; }
}