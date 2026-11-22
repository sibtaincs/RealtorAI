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

        // Fallback 1: DuckDuckGo instant answer (no key).
        try
        {
            var url = $"https://api.duckduckgo.com/?q={Uri.EscapeDataString(query)}&format=json&no_html=1&skip_disambig=1";
            using var res = await _http.GetAsync(url);
            if (res.IsSuccessStatusCode)
            {
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
                if (list.Count > 0) return list.Take(count).ToList();
            }
        }
        catch { }

        // Fallback 2: DuckDuckGo HTML results (no key).
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://html.duckduckgo.com/html/");
            req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            req.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["q"] = query });
            using var res = await _http.SendAsync(req);
            if (!res.IsSuccessStatusCode) return Array.Empty<WebSearchResultDto>();
            var html = await res.Content.ReadAsStringAsync();
            var list = new List<WebSearchResultDto>();
            var matches = System.Text.RegularExpressions.Regex.Matches(
                html,
                @"<a[^>]*class=""result__a""[^>]*href=""([^""]+)""[^>]*>(.*?)</a>.*?<a[^>]*class=""result__snippet""[^>]*>(.*?)</a>",
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                if (list.Count >= count) break;
                var rawUrl = System.Net.WebUtility.HtmlDecode(m.Groups[1].Value);
                var title = System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(m.Groups[2].Value, "<.*?>", string.Empty));
                var snippet = System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(m.Groups[3].Value, "<.*?>", string.Empty));
                var finalUrl = rawUrl;
                if (rawUrl.Contains("uddg=", StringComparison.Ordinal))
                {
                    var idx = rawUrl.IndexOf("uddg=", StringComparison.Ordinal) + 5;
                    var end = rawUrl.IndexOf('&', idx);
                    finalUrl = end > idx ? rawUrl.Substring(idx, end - idx) : rawUrl.Substring(idx);
                    finalUrl = Uri.UnescapeDataString(finalUrl);
                }
                if (!string.IsNullOrWhiteSpace(title))
                {
                    list.Add(new WebSearchResultDto(title.Trim(), snippet.Trim(), finalUrl));
                }
            }
            return list;
        }
        catch
        {
            return Array.Empty<WebSearchResultDto>();
        }
    }
}