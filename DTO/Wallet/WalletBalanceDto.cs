namespace TruekAppAPI.DTO.Wallet;

public class WalletBalanceDto
{
    public decimal Balance { get; set; }
    public List<WalletEntryDto> Entries { get; set; } = [];
}