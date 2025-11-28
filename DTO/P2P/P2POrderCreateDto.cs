using TruekAppAPI.Models;

namespace TruekAppAPI.DTO.P2P;

public class P2POrderCreateDto
{
    // Deposit: usuario quiere comprar TrueCoins con BOB
    // Withdraw: usuario quiere vender TrueCoins y recibir BOB
    public P2POrderType Type { get; set; }

    // Monto en bolivianos
    public double AmountBob { get; set; }

    // Monto en TrueCoins (ya calculado con la tasa)
    public double AmountTrueCoins { get; set; }

    // Tasa (por ejemplo TrueCoins / BOB)
    public double Rate { get; set; }

    // Texto libre: “Cuenta BCP 123…”, “QR TigoMoney…”, etc.
    public string? PaymentMethod { get; set; }
}