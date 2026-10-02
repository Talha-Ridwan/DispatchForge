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
    public DbSet<EventType> EventTypes => Set<EventType>();

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
        modelBuilder.Entity<Tenant>()
            .HasMany(t => t.EventTypes)
            .WithOne()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<EventType>()
            .HasIndex(e => new { e.TenantId, e.Name })
            .IsUnique();
        modelBuilder.Entity<EventType>()
            .HasIndex(e => new {e.TenantId, e.BitPosition})
            .IsUnique();
        /*
         * Translation: Constraint query for the database
         */
        modelBuilder.Entity<EventType>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_EventType_BitPosition",
                "\"BitPosition\" BETWEEN 0 AND 63"));
    }
}
