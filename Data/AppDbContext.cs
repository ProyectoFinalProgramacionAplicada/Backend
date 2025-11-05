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
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Company>().HasIndex(x => x.IsActive);
        b.Entity<Listing>().HasIndex(x => x.IsPublished);
        b.Entity<Trade>().HasIndex(x => x.Status);
        base.OnModelCreating(b);
    }
}