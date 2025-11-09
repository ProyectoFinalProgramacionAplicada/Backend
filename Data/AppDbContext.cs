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

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Índices
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Company>().HasIndex(x => x.IsActive);
        b.Entity<Listing>().HasIndex(x => x.IsPublished);
        b.Entity<Trade>().HasIndex(x => x.Status);

        // --- 🔧 RELACIONES TRADE - USER ---
        b.Entity<Trade>()
            .HasOne(t => t.RequesterUser)
            .WithMany()
            .HasForeignKey(t => t.RequesterUserId)
            .OnDelete(DeleteBehavior.Restrict); // evita cascada

        b.Entity<Trade>()
            .HasOne(t => t.OwnerUser)
            .WithMany()
            .HasForeignKey(t => t.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict); // evita cascada

        // --- 🔧 RELACIONES TRADE - LISTING ---
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
        
        // --- 🔧 RELACIONES USERREVIEWS ---
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
        
        // Si Setting no tiene clave primaria:
        b.Entity<Setting>().HasNoKey();

        base.OnModelCreating(b);
    }
}
