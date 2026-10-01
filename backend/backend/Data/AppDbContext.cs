using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Destination> Destinations => Set<Destination>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        /*
         * Tenant is the principal, Destination is the dependent.
         * Translation : Tenants has many destinations, restrict relationship.
         * Delete behavior is restricting.
         */
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Tenant>()
            .HasMany(t => t.Destinations)
            .WithOne()
            .HasForeignKey(d => d.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
