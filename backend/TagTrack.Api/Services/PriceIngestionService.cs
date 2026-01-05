using Microsoft.EntityFrameworkCore;
using TagTrack.Api.Data;
using TagTrack.Api.Data.Entities;

namespace TagTrack.Api.Services;

public class PriceIngestionService
{
    private readonly AppDbContext _db;
    private readonly IPriceFetcher _fetcher;

    public PriceIngestionService(AppDbContext db, IPriceFetcher fetcher)
    {
        _db = db;
        _fetcher = fetcher;
    }

    public async Task<PriceSnapshot?> FetchAndPersistAsync(Guid productId, Guid? sourceId = null, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .Include(p => p.Sources)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null) return null;

        var source = sourceId.HasValue
            ? product.Sources.FirstOrDefault(s => s.Id == sourceId.Value)
            : product.Sources.FirstOrDefault(s => s.IsPrimary) ?? product.Sources.FirstOrDefault();

        if (source is null) return null;

        var fetched = await _fetcher.FetchAsync(product, source, cancellationToken);

        var snapshot = new PriceSnapshot
        {
            ProductId = product.Id,
            ProductSourceId = source.Id,
            Price = fetched.Price,
            Currency = fetched.Currency,
            CollectedAt = fetched.CollectedAt,
            RawDataJson = fetched.RawDataJson,
        };

        _db.PriceSnapshots.Add(snapshot);

        // Update cached metadata if provided
        if (!string.IsNullOrEmpty(fetched.Description) && string.IsNullOrEmpty(product.Description))
        {
            product.Description = fetched.Description;
        }
        if (!string.IsNullOrEmpty(fetched.ImageStoragePath) && string.IsNullOrEmpty(product.ImagePathInStorage))
        {
            product.ImagePathInStorage = fetched.ImageStoragePath;
        }
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return snapshot;
    }
}
