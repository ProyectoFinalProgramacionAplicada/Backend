namespace TruekAppAPI.DTO.Wallet;

public class WalletTransferRequestDto
{
    public int ToUserId { get; set; }
    public double Amount { get; set; }
    public string? Reference { get; set; }
}