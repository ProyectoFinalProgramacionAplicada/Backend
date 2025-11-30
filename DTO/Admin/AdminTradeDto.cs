using TruekAppAPI.Models;

namespace TruekAppAPI.DTO.Admin;

/// <summary>
/// DTO para trades en el panel de administración
/// </summary>
public class AdminTradeDto
{
    public int Id { get; set; }
    public int TargetListingId { get; set; }
    public int RequesterUserId { get; set; }  // Comprador
    public int OwnerUserId { get; set; }      // Vendedor
    public TradeStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Datos opcionales para más contexto
    public string? TargetListingTitle { get; set; }
    public decimal? TargetListingValue { get; set; }
}