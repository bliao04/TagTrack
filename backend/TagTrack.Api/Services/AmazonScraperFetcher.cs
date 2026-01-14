using System.Globalization;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TagTrack.Api.Data.Entities;

namespace TagTrack.Api.Services;

public class AmazonScraperOptions
{
    public string BaseUrl { get; set; } = "https://www.amazon.com";
    public string UserAgent { get; set; } = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
    public string AcceptLanguage { get; set; } = "en-US,en;q=0.9";
}

public class AmazonScraperFetcher : IPriceFetcher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AmazonScraperOptions _options;
    private readonly ILogger<AmazonScraperFetcher> _logger;

    public AmazonScraperFetcher(IHttpClientFactory httpClientFactory, IOptions<AmazonScraperOptions> options, ILogger<AmazonScraperFetcher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PriceFetchResult> FetchAsync(Product product, ProductSource source, CancellationToken cancellationToken = default)
    {
        var url = ResolveUrl(product, source);

        _logger.LogInformation("Scraping Amazon price for product {ProductId} source {SourceId} at {Url}", product.Id, source.Id, url);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.Clear();
        request.Headers.UserAgent.ParseAdd(_options.UserAgent);
        request.Headers.AcceptLanguage.Clear();
        request.Headers.TryAddWithoutValidation("Accept-Language", _options.AcceptLanguage);

        var client = _httpClientFactory.CreateClient("amazon-scraper");
        client.Timeout = TimeSpan.FromSeconds(15);

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        var (price, currencySymbol) = ParsePrice(html);
        var currency = CurrencyFromSymbol(currencySymbol) ?? "USD";

        var title = ExtractInnerText(html, "//span[@id='productTitle']") ?? product.Title;
        var image = ExtractAttribute(html, "//img[@id='landingImage']", "src") ?? product.ImagePathInStorage;

        return new PriceFetchResult(
            Price: price,
            Currency: currency,
            CollectedAt: DateTimeOffset.UtcNow,
            Description: title,
            ImageStoragePath: image,
            RawDataJson: null
        );
    }

    private string ResolveUrl(Product product, ProductSource source)
    {
        if (!string.IsNullOrWhiteSpace(source.SourceUrl)) return source.SourceUrl!;
        if (!string.IsNullOrWhiteSpace(source.ExternalId)) return $"{_options.BaseUrl.TrimEnd('/')}/dp/{Uri.EscapeDataString(source.ExternalId!)}";
        if (!string.IsNullOrWhiteSpace(product.Url)) return product.Url;
        throw new InvalidOperationException("Amazon source must have a SourceUrl or ExternalId");
    }

    private (decimal price, string? symbol) ParsePrice(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Common Amazon price nodes
        var candidates = new[]
        {
            "//span[@id='priceblock_ourprice']",
            "//span[@id='priceblock_dealprice']",
            "//span[contains(@class,'a-price')]/span[contains(@class,'a-offscreen')]",
            "//span[contains(@data-a-color,'price')]/span[contains(@class,'a-offscreen')]"
        };

        foreach (var xpath in candidates)
        {
            var text = ExtractInnerText(doc, xpath);
            if (!string.IsNullOrWhiteSpace(text))
            {
                var parsed = ParseNumericPrice(text!, out var symbol);
                return (parsed, symbol);
            }
        }

        throw new InvalidOperationException("Unable to find price on Amazon page");
    }

    private static decimal ParseNumericPrice(string text, out string? symbol)
    {
        symbol = null;

        // Capture the first non-digit currency symbol
        var symbolMatch = Regex.Match(text, @"[^0-9.,\s]");
        if (symbolMatch.Success) symbol = symbolMatch.Value;

        var match = Regex.Match(text, @"([0-9][0-9,\.]*[0-9])");
        if (!match.Success) throw new InvalidOperationException("Unable to parse numeric price");

        var number = match.Groups[1].Value.Replace(",", "");
        if (decimal.TryParse(number, NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
        {
            return price;
        }

        throw new InvalidOperationException("Unable to parse numeric price");
    }

    private static string? CurrencyFromSymbol(string? symbol)
    {
        return symbol switch
        {
            "$" => "USD",
            "£" => "GBP",
            "€" => "EUR",
            "¥" => "JPY",
            _ => null
        };
    }

    private static string? ExtractInnerText(string html, string xpath)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return ExtractInnerText(doc, xpath);
    }

    private static string? ExtractInnerText(HtmlDocument doc, string xpath)
    {
        var node = doc.DocumentNode.SelectSingleNode(xpath);
        return node?.InnerText?.Trim();
    }

    private static string? ExtractAttribute(string html, string xpath, string attribute)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var node = doc.DocumentNode.SelectSingleNode(xpath);
        return node?.GetAttributeValue(attribute, null);
    }
}