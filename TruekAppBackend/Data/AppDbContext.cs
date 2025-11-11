using Microsoft.EntityFrameworkCore;
using TruekAppBackend.Models;

namespace TruekAppBackend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Índices
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Company>().HasIndex(x => x.IsActive);

        base.OnModelCreating(b);
    }
}