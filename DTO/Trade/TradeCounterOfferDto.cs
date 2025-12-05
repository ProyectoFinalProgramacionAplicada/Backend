// DTO sugerido (si aún no lo tienes)
public class TradeCounterOfferDto
{
    public int? OfferedListingId { get; set; }      // Listing que ofreces ahora (o null si ya no ofreces uno)
    public int? TargetListingId { get; set; }       // Normalmente no cambia, pero por si acaso
    public double? OfferedTrueCoins { get; set; }   // TC que ofreces tú
    public double? RequestedTrueCoins { get; set; } // TC que pides de vuelta
    public string? Message { get; set; }            // Mensaje opcional de la contraoferta
}