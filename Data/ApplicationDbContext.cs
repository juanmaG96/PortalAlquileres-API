using System.Text.Json;
using Marketplace.API.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Marketplace.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Strategic Indexes for Search Performance
            entity.HasIndex(e => e.City).HasDatabaseName("IX_Properties_City");
            entity.HasIndex(e => e.Price).HasDatabaseName("IX_Properties_Price");
            entity.HasIndex(e => e.PropertyType).HasDatabaseName("IX_Properties_PropertyType");
            entity.HasIndex(e => e.Rooms).HasDatabaseName("IX_Properties_Rooms");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_Properties_Status");
            entity.HasIndex(e => e.IsPremium).HasDatabaseName("IX_Properties_IsPremium");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Properties_CreatedAt");

            // Composite Index for common Search Filters
            entity.HasIndex(e => new { e.City, e.Status, e.IsDeleted, e.IsPremium })
                  .HasDatabaseName("IX_Properties_Search_Composite");

            // Soft Delete Query Filter
            entity.HasQueryFilter(p => !p.IsDeleted && p.Status == PropertyStatus.Active);

            // ImageUrls JSON Conversion with ValueComparer to prevent EF Core warning
            var stringListComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1 != null && c2 != null ? c1.SequenceEqual(c2) : c1 == c2,
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()
            );

            entity.Property(e => e.ImageUrls)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                  )
                  .Metadata.SetValueComparer(stringListComparer);
        });

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Evita usuarios duplicados
            entity.HasIndex(e => e.Username).IsUnique().HasDatabaseName("IX_AdminUsers_Username");
        });
    }
}
