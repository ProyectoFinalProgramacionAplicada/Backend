namespace TruekAppAPI.Models;

public enum WalletEntryType
{
    EarnedByTrade,        // 0
    SpentOnReward,        // 1
    AdminAdjustment,      // 2
    Bonus,                // 3

    // 👇 NUEVOS TIPOS – SIEMPRE AL FINAL
    InternalTransferOut,  // 4
    InternalTransferIn,   // 5
    P2PDeposit,           // 6
    P2PWithdraw           // 7
}