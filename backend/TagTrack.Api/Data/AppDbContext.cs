using Microsoft.EntityFrameworkCore;
using TagTrack.Api.Data.Entities;

namespace TagTrack.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductSource> ProductSources => Set<ProductSource>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<Watchlist> Watchlists => Set<Watchlist>();
    public DbSet<ProductAsset> ProductAssets => Set<ProductAsset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.Url).IsUnique();
            entity.Property(p => p.Title).IsRequired();
            entity.Property(p => p.Url).IsRequired();
            entity.Property(p => p.UpdatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        });

        modelBuilder.Entity<ProductSource>(entity =>
        {
            entity.HasIndex(s => new { s.ProductId, s.Store, s.ExternalId }).IsUnique(false);
            entity.Property(s => s.Store).IsRequired();
            entity.HasOne(s => s.Product)
                .WithMany(p => p.Sources)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PriceSnapshot>(entity =>
        {
            entity.HasIndex(p => new { p.ProductId, p.ProductSourceId, p.CollectedAt });
            entity.Property(p => p.Currency).HasMaxLength(10).IsRequired();
            entity.HasOne(p => p.Product)
                .WithMany(p => p.PriceSnapshots)
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(p => p.ProductSource)
                .WithMany(s => s.PriceSnapshots)
                .HasForeignKey(p => p.ProductSourceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Watchlist>(entity =>
        {
            entity.HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();
            entity.Property(w => w.UserId).IsRequired();
            entity.HasOne(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(w => w.TargetPrice).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<ProductAsset>(entity =>
        {
            entity.HasIndex(a => new { a.ProductId, a.Type });
            entity.Property(a => a.StoragePath).IsRequired();
            entity.Property(a => a.Type).IsRequired();
            entity.HasOne(a => a.Product)
                .WithMany(p => p.Assets)
                .HasForeignKey(a => a.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
