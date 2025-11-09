namespace TruekAppAPI.Models;

public class WalletEntry : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = default!;
    public decimal Amount { get; set; }
    public WalletEntryType Type { get; set; }
    public string? RefType { get; set; }
    public int? RefId { get; set; }
}