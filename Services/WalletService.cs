using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TruekAppAPI.Data;
using TruekAppAPI.Models;

namespace TruekAppAPI.Services;

public class WalletService : IWalletService
{
    private readonly AppDbContext _db;

    public WalletService(AppDbContext db)
    {
        _db = db;
    }

    // ======================================================
    //  Transferencia interna entre usuarios (no P2P formal)
    // ======================================================
    public async Task TransferAsync(int fromUserId, int toUserId, decimal amount, string? reference)
    {
        if (amount <= 0m)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        using var tx = await _db.Database.BeginTransactionAsync();

        var fromUser = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == fromUserId)
            ?? throw new InvalidOperationException("From user not found");

        var toUser = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == toUserId)
            ?? throw new InvalidOperationException("To user not found");

        if (fromUser.TrueCoinBalance < amount)
            throw new InvalidOperationException("Insufficient balance");

        // Debitar origen
        fromUser.TrueCoinBalance -= amount;
        _db.WalletEntries.Add(new WalletEntry
        {
            UserId = fromUserId,
            Amount = amount * -1m,
            Type = WalletEntryType.InternalTransferOut,
            RefType = reference ?? "InternalTransfer",
            RefId = null
        });

        // Acreditar destino
        toUser.TrueCoinBalance += amount;
        _db.WalletEntries.Add(new WalletEntry
        {
            UserId = toUserId,
            Amount = amount,
            Type = WalletEntryType.InternalTransferIn,
            RefType = reference ?? "InternalTransfer",
            RefId = null
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // ======================================================
    //  Depósito P2P (el comprador recibe TrueCoins)
    //  sellerUserId = vendedor que libera TC
    //  buyerUserId  = comprador que recibe TC
    // ======================================================
    public async Task ApplyP2PDepositAsync(int buyerUserId, int sellerUserId, decimal amountTrueCoins, int p2pOrderId)
    {
        if (amountTrueCoins <= 0m)
            throw new ArgumentException("Amount must be positive", nameof(amountTrueCoins));

        using var tx = await _db.Database.BeginTransactionAsync();

        var seller = await _db.Users.FirstOrDefaultAsync(u => u.Id == sellerUserId)
            ?? throw new InvalidOperationException("Seller not found");

        var buyer = await _db.Users.FirstOrDefaultAsync(u => u.Id == buyerUserId)
            ?? throw new InvalidOperationException("Buyer not found");

        if (seller.TrueCoinBalance < amountTrueCoins)
            throw new InvalidOperationException("Seller has insufficient balance");

        // Vendedor entrega TrueCoins
        seller.TrueCoinBalance -= amountTrueCoins;
        _db.WalletEntries.Add(new WalletEntry
        {
            UserId = sellerUserId,
            Amount = amountTrueCoins * -1m,
            Type = WalletEntryType.InternalTransferOut,
            RefType = "P2PDeposit",
            RefId = p2pOrderId
        });

        // Comprador recibe TrueCoins (marcamos como depósito P2P)
        buyer.TrueCoinBalance += amountTrueCoins;
        _db.WalletEntries.Add(new WalletEntry
        {
            UserId = buyerUserId,
            Amount = amountTrueCoins,
            Type = WalletEntryType.P2PDeposit,
            RefType = "P2PDeposit",
            RefId = p2pOrderId
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // ======================================================
    //  Retiro P2P (el usuario vende sus TrueCoins a un cajero)
    //  sellerUserId = usuario que retira y entrega TC
    //  buyerUserId  = cajero que recibe TC
    // ======================================================
    public async Task ApplyP2PWithdrawAsync(int sellerUserId, int buyerUserId, decimal amountTrueCoins, int p2pOrderId)
    {
        if (amountTrueCoins <= 0m)
            throw new ArgumentException("Amount must be positive", nameof(amountTrueCoins));

        using var tx = await _db.Database.BeginTransactionAsync();

        var seller = await _db.Users.FirstOrDefaultAsync(u => u.Id == sellerUserId)
            ?? throw new InvalidOperationException("Seller not found");

        var buyer = await _db.Users.FirstOrDefaultAsync(u => u.Id == buyerUserId)
            ?? throw new InvalidOperationException("Buyer not found");

        if (seller.TrueCoinBalance < amountTrueCoins)
            throw new InvalidOperationException("Seller has insufficient balance");

        // Usuario que retira: entrega TrueCoins → lo marcamos como P2PWithdraw
        seller.TrueCoinBalance -= amountTrueCoins;
        _db.WalletEntries.Add(new WalletEntry
        {
            UserId = sellerUserId,
            Amount = amountTrueCoins * -1m,
            Type = WalletEntryType.P2PWithdraw,
            RefType = "P2PWithdraw",
            RefId = p2pOrderId
        });

        // Cajero / contraparte recibe TrueCoins
        buyer.TrueCoinBalance += amountTrueCoins;
        _db.WalletEntries.Add(new WalletEntry
        {
            UserId = buyerUserId,
            Amount = amountTrueCoins,
            Type = WalletEntryType.InternalTransferIn,
            RefType = "P2PWithdraw",
            RefId = p2pOrderId
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
