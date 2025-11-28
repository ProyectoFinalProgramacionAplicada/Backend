using TruekAppAPI.Models;

namespace TruekAppAPI.DTO.P2P;

public class P2POrderDto
{
    public int Id { get; set; }

    public P2POrderType Type { get; set; }
    public P2POrderStatus Status { get; set; }

    public double AmountBob { get; set; }
    public double AmountTrueCoins { get; set; }
    public double Rate { get; set; }

    public int CreatorUserId { get; set; }
    public int? CounterpartyUserId { get; set; }

    public string? PaymentMethod { get; set; }

    public DateTime CreatedAt { get; set; }
}