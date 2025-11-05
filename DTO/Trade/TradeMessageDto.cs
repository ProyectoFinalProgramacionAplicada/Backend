namespace TruekAppAPI.DTO.Trade;

public class TradeMessageDto
{
    public int Id { get; set; }
    public int SenderUserId { get; set; }
    public string Text { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
