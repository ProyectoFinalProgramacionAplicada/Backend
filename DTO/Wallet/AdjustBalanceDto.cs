namespace TruekAppAPI.DTO.Wallet
{
    public class AdjustBalanceDto
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}