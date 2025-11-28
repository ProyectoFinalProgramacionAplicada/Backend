namespace TruekAppAPI.Models;

public enum P2POrderType
{
    // El creador quiere comprar TrueCoins (mete BOB, recibe TrueCoins)
    Deposit = 0,

    // El creador quiere vender TrueCoins (recibe BOB, entrega TrueCoins)
    Withdraw = 1
}

public enum P2POrderStatus
{
    Pending = 0,   // Publicada, sin contraparte
    Matched = 1,   // Tomada por otro usuario
    Paid = 2,      // El comprador marcó que pagó en fiat
    Released = 3,  // El vendedor liberó los TrueCoins
    Cancelled = 4, // Cancelada
    Disputed = 5   // En disputa
}

/// <summary>
/// Modelo de la orden P2P (depósito o retiro)
/// </summary>
public class P2POrder : BaseEntity
{
    public P2POrderType Type { get; set; }

    // Usuario que crea la orden
    public int CreatorUserId { get; set; }
    public User CreatorUser { get; set; } = null!;

    // Usuario que toma la orden (puede ser null mientras está pendiente)
    public int? CounterpartyUserId { get; set; }
    public User? CounterpartyUser { get; set; }

    // Monto en moneda fiat (BOB)
    public double AmountBob { get; set; }

    // Monto en TrueCoins
    public double AmountTrueCoins { get; set; }

    // Tasa acordada (TrueCoins / BOB o como desees manejarlo)
    public double Rate { get; set; }

    // QR, datos bancarios, TigoMoney, etc.
    public string? PaymentMethod { get; set; }

    public P2POrderStatus Status { get; set; } = P2POrderStatus.Pending;
}