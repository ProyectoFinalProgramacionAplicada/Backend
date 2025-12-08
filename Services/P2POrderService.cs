using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TruekAppAPI.Data;
using TruekAppAPI.DTO.P2P;
using TruekAppAPI.Models;

namespace TruekAppAPI.Services;

public class P2POrderService : IP2POrderService
{
    private readonly AppDbContext _db;
    private readonly IWalletService _walletService;

    public P2POrderService(AppDbContext db, IWalletService walletService)
    {
        _db = db;
        _walletService = walletService;
    }

    // 🔹 Nuevo método para soportar GET /api/P2POrders/{id}
    public async Task<P2POrderDto?> GetByIdAsync(int id)
    {
        var order = await _db.P2POrders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return null;

        return Map(order);
    }

    public async Task<P2POrderDto> CreateAsync(int creatorUserId, P2POrderCreateDto dto)
    {
        var order = new P2POrder
        {
            CreatorUserId = creatorUserId,
            Type = dto.Type,
            AmountBob = dto.AmountBob,
            AmountTrueCoins = dto.AmountTrueCoins,
            Rate = dto.Rate,
            PaymentMethod = dto.PaymentMethod,
            Status = P2POrderStatus.Pending
        };

        _db.P2POrders.Add(order);
        await _db.SaveChangesAsync();

        return Map(order);
    }

    public async Task<IEnumerable<P2POrderDto>> GetOrderBookAsync()
    {
        var orders = await _db.P2POrders
            .Where(o => o.Status == P2POrderStatus.Pending)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(Map).ToList();
    }

    public async Task<P2POrderDto> TakeAsync(int orderId, int takerUserId)
    {
        var order = await _db.P2POrders.FindAsync(orderId)
                    ?? throw new InvalidOperationException("Order not found");

        if (order.Status != P2POrderStatus.Pending)
            throw new InvalidOperationException("Order is not pending");

        if (order.CreatorUserId == takerUserId)
            throw new InvalidOperationException("You cannot take your own order");

        // 🔹 Validar saldo del taker cuando la orden es de tipo Depósito
        // En este caso el taker es el vendedor de TrueCoins.
        if (order.Type == P2POrderType.Deposit)
        {
            var amountTrueCoins = (decimal)order.AmountTrueCoins;

            if (amountTrueCoins <= 0)
                throw new InvalidOperationException("Invalid TrueCoins amount for this order");

            var takerBalance = await _walletService.GetTrueCoinBalanceAsync(takerUserId);

            if (takerBalance < amountTrueCoins)
                throw new InvalidOperationException("Insufficient TrueCoins balance to take this order");
        }

        // Si pasa todas las validaciones, se matchea la orden
        order.CounterpartyUserId = takerUserId;
        order.Status = P2POrderStatus.Matched;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Map(order);
    }


    public async Task<P2POrderDto> MarkPaidAsync(int orderId, int userId)
    {
        var order = await _db.P2POrders.FindAsync(orderId)
            ?? throw new InvalidOperationException("Order not found");

        if (order.Status != P2POrderStatus.Matched)
            throw new InvalidOperationException("Order is not matched");

        // Aquí podrías validar que userId sea el comprador fiat

        order.Status = P2POrderStatus.Paid;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Map(order);
    }

    public async Task<P2POrderDto> ReleaseAsync(int orderId, int userId)
    {
        var order = await _db.P2POrders.FindAsync(orderId)
            ?? throw new InvalidOperationException("Order not found");

        if (order.Status != P2POrderStatus.Paid)
            throw new InvalidOperationException("Order is not paid");

        if (!order.CounterpartyUserId.HasValue)
            throw new InvalidOperationException("Order has no counterparty");

        int buyerUserId, sellerUserId;

        if (order.Type == P2POrderType.Deposit)
        {
            // Creator es comprador, Counterparty es vendedor de TrueCoins
            buyerUserId = order.CreatorUserId;
            sellerUserId = order.CounterpartyUserId.Value;
        }
        else
        {
            // Withdraw: Creator vende sus TrueCoins, Counterparty compra
            sellerUserId = order.CreatorUserId;
            buyerUserId = order.CounterpartyUserId.Value;
        }

        // Solo el vendedor puede liberar los fondos
        if (userId != sellerUserId)
            throw new InvalidOperationException("Only seller can release funds");

        // order.AmountTrueCoins es double, wallet usa decimal
        var amount = (decimal)order.AmountTrueCoins;

        if (order.Type == P2POrderType.Deposit)
        {
            await _walletService.ApplyP2PDepositAsync(
                buyerUserId,
                sellerUserId,
                amount,
                order.Id
            );
        }
        else
        {
            await _walletService.ApplyP2PWithdrawAsync(
                sellerUserId,
                buyerUserId,
                amount,
                order.Id
            );
        }

        order.Status = P2POrderStatus.Released;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Map(order);
    }

    public async Task<P2POrderDto> CancelAsync(int orderId, int userId)
    {
        var order = await _db.P2POrders.FindAsync(orderId)
            ?? throw new InvalidOperationException("Order not found");

        if (order.Status != P2POrderStatus.Pending &&
            order.Status != P2POrderStatus.Matched)
            throw new InvalidOperationException("Cannot cancel at this stage");

        if (userId != order.CreatorUserId)
            throw new InvalidOperationException("You cannot cancel this order");

        order.Status = P2POrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Map(order);
    }
    
    public async Task<IEnumerable<P2POrderDto>> GetOrdersForUserAsync(int userId)
    {
        var orders = await _db.P2POrders
            .Where(o =>
                (o.CreatorUserId == userId ||
                 o.CounterpartyUserId == userId) &&
                o.Status != P2POrderStatus.Released &&
                o.Status != P2POrderStatus.Cancelled &&
                o.Status != P2POrderStatus.Pending)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(Map).ToList();
    }


    private static P2POrderDto Map(P2POrder order) => new P2POrderDto
    {
        Id = order.Id,
        Type = order.Type,
        Status = order.Status,
        AmountBob = order.AmountBob,
        AmountTrueCoins = order.AmountTrueCoins,
        Rate = order.Rate,
        CreatorUserId = order.CreatorUserId,
        CounterpartyUserId = order.CounterpartyUserId,
        PaymentMethod = order.PaymentMethod,
        CreatedAt = order.CreatedAt
    };
}
