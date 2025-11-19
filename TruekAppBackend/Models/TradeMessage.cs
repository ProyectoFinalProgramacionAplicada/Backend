namespace TruekAppBackend.Models;

public class TradeMessage : BaseEntity
{
    public int TradeId { get; set; }
    public Trade Trade { get; set; } = default!;
    public int SenderUserId { get; set; }
    public User SenderUser { get; set; } = default!;
    public string Text { get; set; } = default!;
}