using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TagTrack.Api.Data.Entities;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ImagePathInStorage { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ProductSource> Sources { get; set; } = new List<ProductSource>();
    public ICollection<PriceSnapshot> PriceSnapshots { get; set; } = new List<PriceSnapshot>();
    public ICollection<ProductAsset> Assets { get; set; } = new List<ProductAsset>();
}

public class ProductSource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Store { get; set; } = string.Empty; // e.g., amazon

    [MaxLength(100)]
    public string? ExternalId { get; set; } // e.g., ASIN

    [MaxLength(500)]
    public string? SourceUrl { get; set; }

    public bool IsPrimary { get; set; }

    public ICollection<PriceSnapshot> PriceSnapshots { get; set; } = new List<PriceSnapshot>();
}

public class PriceSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid ProductSourceId { get; set; }
    public ProductSource ProductSource { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Required, MaxLength(10)]
    public string Currency { get; set; } = "USD";

    public DateTimeOffset CollectedAt { get; set; } = DateTimeOffset.UtcNow;

    [MaxLength(2000)]
    public string? RawDataJson { get; set; }
}

public class Watchlist
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string UserId { get; set; } = string.Empty; // Supabase user id (UUID string)

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TargetPrice { get; set; }

    public bool Notify { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ProductAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required, MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty; // path in Supabase Storage

    [Required, MaxLength(50)]
    public string Type { get; set; } = string.Empty; // e.g., image, description

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
