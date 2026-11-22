using System.Text.Json;
using RealEstateAI.Api.Dtos;

namespace RealEstateAI.Api.Services;

public interface IWebSearchService
{
    Task<IReadOnlyList<WebSearchResultDto>> SearchAsync(string query, int count = 5);
}

public class WebSearchService : IWebSearchService
{
    private readonly HttpClient _http;
    private readonly string _bingKey;
    private readonly string _bingEndpoint;

    public WebSearchService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _bingKey = config["WebSearch:ApiKey"] ?? string.Empty;
        _bingEndpoint = config["WebSearch:Endpoint"] ?? "https://api.bing.microsoft.com/v7.0/search";
    }

    public async Task<IReadOnlyList<WebSearchResultDto>> SearchAsync(string query, int count = 5)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<WebSearchResultDto>();

        if (!string.IsNullOrWhiteSpace(_bingKey))
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, $"{_bingEndpoint}?q={Uri.EscapeDataString(query)}&count={count}");
                req.Headers.Add("Ocp-Apim-Subscription-Key", _bingKey);
                using var res = await _http.SendAsync(req);
                if (res.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                    if (doc.RootElement.TryGetProperty("webPages", out var pages) &&
                        pages.TryGetProperty("value", out var values))
                    {
                        var list = new List<WebSearchResultDto>();
                        foreach (var item in values.EnumerateArray().Take(count))
                        {
                            list.Add(new WebSearchResultDto(
                                item.GetProperty("name").GetString() ?? string.Empty,
                                item.GetProperty("snippet").GetString() ?? string.Empty,
                                item.GetProperty("url").GetString() ?? string.Empty));
                        }
                        return list;
                    }
                }
            }
            catch { }
        }

        // Fallback: DuckDuckGo instant answer (no key).
        try
        {
            var url = $"https://api.duckduckgo.com/?q={Uri.EscapeDataString(query)}&format=json&no_html=1&skip_disambig=1";
            using var res = await _http.GetAsync(url);
            if (!res.IsSuccessStatusCode) return Array.Empty<WebSearchResultDto>();
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            var list = new List<WebSearchResultDto>();
            if (doc.RootElement.TryGetProperty("AbstractText", out var abs) && !string.IsNullOrWhiteSpace(abs.GetString()))
            {
                var sourceUrl = doc.RootElement.TryGetProperty("AbstractURL", out var au) ? au.GetString() ?? string.Empty : string.Empty;
                var heading = doc.RootElement.TryGetProperty("Heading", out var h) ? h.GetString() ?? query : query;
                list.Add(new WebSearchResultDto(heading, abs.GetString()!, sourceUrl));
            }
            if (doc.RootElement.TryGetProperty("RelatedTopics", out var topics))
            {
                foreach (var t in topics.EnumerateArray().Take(count - list.Count))
                {
                    if (t.TryGetProperty("Text", out var text) && !string.IsNullOrWhiteSpace(text.GetString()))
                    {
                        var u = t.TryGetProperty("FirstURL", out var fu) ? fu.GetString() ?? string.Empty : string.Empty;
                        list.Add(new WebSearchResultDto(text.GetString()!, text.GetString()!, u));
                    }
                }
            }
            return list.Take(count).ToList();
        }
        catch
        {
            return Array.Empty<WebSearchResultDto>();
        }
    }
}