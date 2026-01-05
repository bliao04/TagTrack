using TagTrack.Api.Data.Entities;

namespace TagTrack.Api.Services;

public record PriceFetchResult(
    decimal Price,
    string Currency,
    DateTimeOffset CollectedAt,
    string? Description,
    string? ImageStoragePath,
    string? RawDataJson
);

public interface IPriceFetcher
{
    Task<PriceFetchResult> FetchAsync(Product product, ProductSource source, CancellationToken cancellationToken = default);
}

public class AmazonMockFetcher : IPriceFetcher
{
    public Task<PriceFetchResult> FetchAsync(Product product, ProductSource source, CancellationToken cancellationToken = default)
    {
        // Deterministic mock price based on source ExternalId length to keep it stable across runs
        var seed = (source.ExternalId ?? source.SourceUrl ?? product.Title).Length;
        var price = 10 + (seed % 50); // $10 - $59 range
        var collected = DateTimeOffset.UtcNow;
        return Task.FromResult(new PriceFetchResult(
            Price: price,
            Currency: "USD",
            CollectedAt: collected,
            Description: product.Description ?? "Mock description for testing",
            ImageStoragePath: product.ImagePathInStorage,
            RawDataJson: null
        ));
    }
}
