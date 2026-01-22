using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TagTrack.Api.Data;

namespace TagTrack.Api.Services;

public class PriceRefreshOptions
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 900; // 15 minutes default
    public int MaxProductsPerCycle { get; set; } = 25;
}

// Periodically fetch prices for a subset of products to keep the catalog warm.
public class PriceRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PriceRefreshOptions _options;
    private readonly ILogger<PriceRefreshService> _logger;

    public PriceRefreshService(IServiceScopeFactory scopeFactory, IOptions<PriceRefreshOptions> options, ILogger<PriceRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Price refresh service disabled via configuration");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(60, _options.IntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        _logger.LogInformation("Price refresh service started: interval {Interval}s, batch size {Batch}", interval.TotalSeconds, _options.MaxProductsPerCycle);

        // Kick off an initial refresh immediately
        try
        {
            await RefreshBatchAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Initial price refresh cycle failed");
        }

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RefreshBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Price refresh cycle failed");
            }
        }
    }

    private async Task RefreshBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ingestion = scope.ServiceProvider.GetRequiredService<PriceIngestionService>();

        var products = await db.Products
            .AsNoTracking()
            .Include(p => p.Sources)
            .OrderBy(p => p.UpdatedAt)
            .Take(_options.MaxProductsPerCycle)
            .ToListAsync(cancellationToken);

        if (products.Count == 0)
        {
            _logger.LogInformation("Price refresh: no products found");
            return;
        }

        foreach (var product in products)
        {
            var source = product.Sources.FirstOrDefault(s => s.IsPrimary) ?? product.Sources.FirstOrDefault();
            if (source is null) continue;

            try
            {
                await ingestion.FetchAndPersistAsync(product.Id, source.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh price for product {ProductId} source {SourceId}", product.Id, source.Id);
            }
        }
    }
}