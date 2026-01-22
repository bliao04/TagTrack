using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using DotNetEnv;
using System.IO;
using System.Text;
using TagTrack.Api.Data;
using TagTrack.Api.Data.Entities;
using TagTrack.Api.Endpoints;
using TagTrack.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure JWT authentication using Supabase JWT secret (from env var `SUPABASE_JWT_SECRET` or configuration)
// Load .env.local if present so we can read the DB password from it during development
try
{
    // load .env.local (silent if missing)
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env.local");
    if (File.Exists(envPath)) Env.Load(envPath);
}
catch
{
    // ignore load errors
}

var jwtSecret = builder.Configuration["SUPABASE_JWT_SECRET"] ?? builder.Configuration["Supabase:JwtSecret"];
if (string.IsNullOrEmpty(jwtSecret))
{
    // don't throw here; we'll still allow the app to build, but authentication will fail at runtime if used.
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        if (!string.IsNullOrEmpty(jwtSecret))
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            };
        }
    });

builder.Services.AddAuthorization();

// CORS for frontend
var allowedOrigins = (Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")
        ?? builder.Configuration["Cors:AllowedOrigins"]
        ?? "http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Domain services
builder.Services.AddHttpClient("amazon-scraper");
builder.Services.Configure<AmazonScraperOptions>(builder.Configuration.GetSection("Amazon"));
builder.Services.AddScoped<IPriceFetcher, AmazonScraperFetcher>();
builder.Services.AddScoped<PriceIngestionService>();
builder.Services.Configure<PriceRefreshOptions>(builder.Configuration.GetSection("PriceRefresh"));
builder.Services.AddHostedService<PriceRefreshService>();

// Build connection string by injecting password from environment (loaded from .env.local) into placeholder [DB_PASSWORD]
var rawConn = builder.Configuration.GetConnectionString("Default") ?? string.Empty;
var dbPassword = Environment.GetEnvironmentVariable("SUPABASE_DB_PASSWORD") ?? Environment.GetEnvironmentVariable("DB_PASSWORD");
if (!string.IsNullOrEmpty(dbPassword))
{
    rawConn = rawConn.Replace("[DB_PASSWORD]", dbPassword);
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(rawConn));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Minimal APIs
app.MapProductEndpoints();

// DB connectivity probe
app.MapGet("/health/db", async (AppDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok(new { status = "ok" })
            : Results.Problem("Database unreachable");
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// Seed demo data for quick testing (idempotent)
app.MapPost("/dev/seed", async (AppDbContext db) =>
{
    var url = "https://example.com/demo-product";
    var product = await db.Products.FirstOrDefaultAsync(p => p.Url == url);
    if (product is null)
    {
        product = new Product
        {
            Title = "Demo Product",
            Url = url,
            Description = "Demo product for testing fetch",
            ImagePathInStorage = null,
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
    }

    var source = await db.ProductSources.FirstOrDefaultAsync(s => s.ProductId == product.Id && s.Store == "amazon");
    if (source is null)
    {
        source = new ProductSource
        {
            ProductId = product.Id,
            Store = "amazon",
            ExternalId = "DEMO-ASIN-123",
            SourceUrl = "https://amazon.com/dp/DEMO-ASIN-123",
            IsPrimary = true,
        };
        db.ProductSources.Add(source);
        await db.SaveChangesAsync();
    }

    return Results.Ok(new
    {
        productId = product.Id,
        sourceId = source.Id,
        product = new
        {
            product.Id,
            product.Title,
            product.Url,
            product.Description,
            product.ImagePathInStorage,
            product.CreatedAt,
            product.UpdatedAt,
            sources = new[]
            {
                new
                {
                    source.Id,
                    source.ProductId,
                    source.Store,
                    source.ExternalId,
                    source.SourceUrl,
                    source.IsPrimary
                }
            }
        }
    });
});

// Seed multiple sample Amazon products for front-end browsing (idempotent)
app.MapPost("/dev/seed-sample-products", async (AppDbContext db) =>
{
    var samples = new[]
    {
        new { Title = "Apple AirPods Pro (2nd Gen)", Url = "https://www.amazon.com/dp/B0CHX1V2W9", ExternalId = "B0CHX1V2W9" },
        new { Title = "Kindle Paperwhite 16 GB", Url = "https://www.amazon.com/dp/B09SWW583J", ExternalId = "B09SWW583J" },
        new { Title = "Instant Pot Duo 7-in-1", Url = "https://www.amazon.com/dp/B08PQ2KWHS", ExternalId = "B08PQ2KWHS" }
    };

    var created = new List<object>();

    foreach (var s in samples)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Url == s.Url);
        if (product is null)
        {
            product = new Product
            { Title = s.Title, Url = s.Url, Description = null, ImagePathInStorage = null };
            db.Products.Add(product);
            await db.SaveChangesAsync();
        }

        var source = await db.ProductSources.FirstOrDefaultAsync(ps => ps.ProductId == product.Id && ps.Store == "amazon");
        if (source is null)
        {
            source = new ProductSource
            {
                ProductId = product.Id,
                Store = "amazon",
                ExternalId = s.ExternalId,
                SourceUrl = s.Url,
                IsPrimary = true
            };
            db.ProductSources.Add(source);
            await db.SaveChangesAsync();
        }

        created.Add(new
        {
            product.Id,
            product.Title,
            product.Url,
            product.Description,
            product.ImagePathInStorage,
            product.CreatedAt,
            product.UpdatedAt,
            source = new
            {
                source.Id,
                source.ProductId,
                source.Store,
                source.ExternalId,
                source.SourceUrl,
                source.IsPrimary
            }
        });
    }

    return Results.Ok(new { count = created.Count, items = created });
});

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.Run();