using TruekAppAPI.Models;

namespace TruekAppAPI.DTO.Wallet;

public class WalletEntryDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public WalletEntryType Type { get; set; }
    public DateTime CreatedAt { get; set; }
}