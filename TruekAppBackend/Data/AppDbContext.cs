using Microsoft.EntityFrameworkCore;
using TruekAppBackend.Models;

namespace TruekAppBackend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingImage> ListingImages => Set<ListingImage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Índices
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Company>().HasIndex(x => x.IsActive);
        b.Entity<Listing>().HasIndex(x => x.IsPublished);

        // --- 🔧 RELACIONES TRADE - USER ---
        base.OnModelCreating(b);
    }
}