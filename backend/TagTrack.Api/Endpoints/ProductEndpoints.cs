using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagTrack.Api.Data;
using TagTrack.Api.Data.Entities;
using TagTrack.Api.Services;

namespace TagTrack.Api.Endpoints;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        // Temporary: allow anonymous for local testing; add auth when wiring Supabase JWT
        var group = app.MapGroup("/api/products");

        group.MapGet("/search", async (
            [FromQuery] string q,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromServices] AppDbContext db) =>
        {
            var query = (q ?? string.Empty).Trim();
            var p = Math.Max(1, page);
            var size = Math.Clamp(pageSize, 1, 50);

            var baseQuery = db.Products.AsQueryable();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var lowered = query.ToLower();
                baseQuery = baseQuery.Where(p =>
                    EF.Functions.ILike(p.Title, $"%{query}%") ||
                    EF.Functions.ILike(p.Url, $"%{query}%"));
            }

            var total = await baseQuery.CountAsync();

            var items = await baseQuery
                .OrderByDescending(p => p.UpdatedAt)
                .Skip((p - 1) * size)
                .Take(size)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Url,
                    p.Description,
                    p.ImagePathInStorage,
                    p.CreatedAt,
                    p.UpdatedAt,
                    LatestPrice = p.PriceSnapshots
                        .OrderByDescending(ps => ps.CollectedAt)
                        .Select(ps => new
                        {
                            ps.Price,
                            ps.Currency,
                            ps.CollectedAt,
                            ps.ProductSourceId
                        })
                        .FirstOrDefault(),
                    Sources = p.Sources.Select(s => new
                    {
                        s.Id,
                        s.Store,
                        s.ExternalId,
                        s.SourceUrl,
                        s.IsPrimary
                    })
                })
                .ToListAsync();

            return Results.Ok(new { total, page = p, pageSize = size, items });
        });

        group.MapGet("", async ([FromServices] AppDbContext db) =>
        {
            var products = await db.Products
                .OrderByDescending(p => p.UpdatedAt)
                .Take(50)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Url,
                    p.Description,
                    p.ImagePathInStorage,
                    p.CreatedAt,
                    p.UpdatedAt,
                    LatestPrice = p.PriceSnapshots
                        .OrderByDescending(ps => ps.CollectedAt)
                        .Select(ps => new
                        {
                            ps.Price,
                            ps.Currency,
                            ps.CollectedAt,
                            ps.ProductSourceId
                        })
                        .FirstOrDefault(),
                    Sources = p.Sources.Select(s => new
                    {
                        s.Id,
                        s.Store,
                        s.ExternalId,
                        s.SourceUrl,
                        s.IsPrimary
                    })
                })
                .ToListAsync();
            return Results.Ok(products);
        });

        group.MapGet("/{id:guid}", async (Guid id, [FromServices] AppDbContext db) =>
        {
            var product = await db.Products
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Url,
                    p.Description,
                    p.ImagePathInStorage,
                    p.CreatedAt,
                    p.UpdatedAt,
                    Sources = p.Sources.Select(s => new
                    {
                        s.Id,
                        s.Store,
                        s.ExternalId,
                        s.SourceUrl,
                        s.IsPrimary
                    }),
                    Prices = p.PriceSnapshots
                        .OrderByDescending(ps => ps.CollectedAt)
                        .Take(5)
                        .Select(ps => new
                        {
                            ps.Id,
                            ps.ProductSourceId,
                            ps.Price,
                            ps.Currency,
                            ps.CollectedAt,
                            ps.RawDataJson
                        })
                })
                .FirstOrDefaultAsync();

            return product is null ? Results.NotFound() : Results.Ok(product);
        });

        group.MapPost("", async ([FromBody] CreateProductRequest request, [FromServices] AppDbContext db) =>
        {
            // Enforce unique URL and return existing if present
            var existing = await db.Products
                .Where(p => p.Url == request.Url)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Url,
                    p.Description,
                    p.ImagePathInStorage,
                    p.CreatedAt,
                    p.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (existing is not null)
            {
                return Results.Conflict(new { message = "Product with this URL already exists.", product = existing });
            }

            var product = new Product
            {
                Title = request.Title,
                Url = request.Url,
                Description = request.Description,
                ImagePathInStorage = request.ImagePathInStorage,
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return Results.Created($"/api/products/{product.Id}", product);
        });

        group.MapPost("/{id:guid}/sources", async (Guid id, [FromBody] CreateSourceRequest request, [FromServices] AppDbContext db) =>
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product is null) return Results.NotFound();

            var source = new ProductSource
            {
                ProductId = product.Id,
                Store = request.Store,
                ExternalId = request.ExternalId,
                SourceUrl = request.SourceUrl,
                IsPrimary = request.IsPrimary,
            };
            db.ProductSources.Add(source);
            await db.SaveChangesAsync();

            // Project to DTO to avoid serialization cycles
            var dto = new
            {
                source.Id,
                source.ProductId,
                source.Store,
                source.ExternalId,
                source.SourceUrl,
                source.IsPrimary
            };

            return Results.Created($"/api/products/{id}/sources/{source.Id}", dto);
        });

        group.MapGet("/{id:guid}/prices", async (Guid id, [FromQuery] int? take, [FromServices] AppDbContext db) =>
        {
            var count = take.HasValue ? Math.Max(1, Math.Min(take.Value, 200)) : 50;
            var prices = await db.PriceSnapshots
                .Where(p => p.ProductId == id)
                .OrderByDescending(p => p.CollectedAt)
                .Take(count)
                .Select(ps => new
                {
                    ps.Id,
                    ps.ProductSourceId,
                    ps.Price,
                    ps.Currency,
                    ps.CollectedAt,
                    ps.RawDataJson
                })
                .ToListAsync();
            return Results.Ok(prices);
        });

        group.MapPost("/{id:guid}/fetch", async (Guid id, [FromQuery] Guid? sourceId, [FromServices] PriceIngestionService ingestion, CancellationToken cancellationToken) =>
        {
            var snapshot = await ingestion.FetchAndPersistAsync(id, sourceId, cancellationToken);
            if (snapshot is null) return Results.NotFound();

            return Results.Ok(new
            {
                snapshot.Id,
                snapshot.ProductId,
                snapshot.ProductSourceId,
                snapshot.Price,
                snapshot.Currency,
                snapshot.CollectedAt,
                snapshot.RawDataJson
            });
        });

        group.MapPost("/{id:guid}/fetch-live", async (Guid id, [FromQuery] Guid? sourceId, [FromServices] PriceIngestionService ingestion, CancellationToken cancellationToken) =>
        {
            var result = await ingestion.FetchLiveAsync(id, sourceId, cancellationToken);
            if (result is null) return Results.NotFound();

            return Results.Ok(new
            {
                result.Value.ProductId,
                result.Value.ProductSourceId,
                result.Value.Result.Price,
                result.Value.Result.Currency,
                result.Value.Result.CollectedAt,
                result.Value.Result.RawDataJson,
                result.Value.Result.Description,
                ImagePathInStorage = result.Value.Result.ImageStoragePath
            });
        });

        return group;
    }
}

public record CreateProductRequest(string Title, string Url, string? Description, string? ImagePathInStorage);
public record CreateSourceRequest(string Store, string? ExternalId, string? SourceUrl, bool IsPrimary);
