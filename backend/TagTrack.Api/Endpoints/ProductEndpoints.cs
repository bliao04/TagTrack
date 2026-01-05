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
            return Results.Created($"/api/products/{id}/sources/{source.Id}", source);
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

        group.MapPost("/{id:guid}/fetch", async (Guid id, [FromQuery] Guid? sourceId, [FromServices] PriceIngestionService ingestion) =>
        {
            var snapshot = await ingestion.FetchAndPersistAsync(id, sourceId);
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

        return group;
    }
}

public record CreateProductRequest(string Title, string Url, string? Description, string? ImagePathInStorage);
public record CreateSourceRequest(string Store, string? ExternalId, string? SourceUrl, bool IsPrimary);
