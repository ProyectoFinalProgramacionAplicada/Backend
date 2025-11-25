using Microsoft.EntityFrameworkCore;
using TruekAppAPI.Models;

namespace TruekAppAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingImage> ListingImages => Set<ListingImage>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<TradeMessage> TradeMessages => Set<TradeMessage>();
    public DbSet<UserReview> UserReviews => Set<UserReview>();
    public DbSet<WalletEntry> WalletEntries => Set<WalletEntry>();
    public DbSet<Reward> Rewards => Set<Reward>();
    public DbSet<RewardRedemption> RewardRedemptions => Set<RewardRedemption>();
    public DbSet<Setting> Settings => Set<Setting>();

    // ⭐ NUEVO: P2POrders
    public DbSet<P2POrder> P2POrders => Set<P2POrder>();


    protected override void OnModelCreating(ModelBuilder b)
    {
        // Índices
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Company>().HasIndex(x => x.IsActive);
        b.Entity<Listing>().HasIndex(x => x.IsPublished);
        b.Entity<Trade>().HasIndex(x => x.Status);

        // --- 🔧 TRADE - USER ---
        b.Entity<Trade>()
            .HasOne(t => t.RequesterUser)
            .WithMany()
            .HasForeignKey(t => t.RequesterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Trade>()
            .HasOne(t => t.OwnerUser)
            .WithMany()
            .HasForeignKey(t => t.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- 🔧 TRADE - LISTING ---
        b.Entity<Trade>()
            .HasOne(t => t.TargetListing)
            .WithMany()
            .HasForeignKey(t => t.TargetListingId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Trade>()
            .HasOne(t => t.OfferedListing)
            .WithMany()
            .HasForeignKey(t => t.OfferedListingId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- 🔧 USERREVIEWS ---
        b.Entity<UserReview>()
            .HasOne(r => r.FromUser)
            .WithMany()
            .HasForeignKey(r => r.FromUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<UserReview>()
            .HasOne(r => r.ToUser)
            .WithMany()
            .HasForeignKey(r => r.ToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<UserReview>()
            .HasOne(r => r.Trade)
            .WithMany()
            .HasForeignKey(r => r.TradeId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- ⭐ NUEVO: RELACIONES P2POrder ---
        b.Entity<P2POrder>(entity =>
        {
            entity.HasKey(o => o.Id);

            // Relación con el usuario creador
            entity.HasOne(o => o.CreatorUser)
                  .WithMany()              // No defines colección en User, así que WithMany() vacío
                  .HasForeignKey(o => o.CreatorUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Relación con el usuario que toma la orden
            entity.HasOne(o => o.CounterpartyUser)
                  .WithMany()
                  .HasForeignKey(o => o.CounterpartyUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- SETTINGS sin clave ---
        b.Entity<Setting>().HasNoKey();

        base.OnModelCreating(b);
    }
}
