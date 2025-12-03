using System.Threading.Tasks;

namespace TruekAppAPI.Services;

public interface IWalletService
{
    Task TransferAsync(int fromUserId, int toUserId, decimal amount, string? reference);

    Task ApplyP2PDepositAsync(int buyerUserId, int sellerUserId, decimal amountTrueCoins, int p2pOrderId);

    Task ApplyP2PWithdrawAsync(int sellerUserId, int buyerUserId, decimal amountTrueCoins, int p2pOrderId);
    
    Task ApplyTradeTransferAsync(int fromUserId, int toUserId, decimal amount, int tradeId);

}