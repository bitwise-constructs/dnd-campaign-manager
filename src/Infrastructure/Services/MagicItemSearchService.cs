using System.Net.Http.Json;
using System.Text.Json;
using DndCampaignManager.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DndCampaignManager.Infrastructure.Services;

public class MagicItemSearchService : IMagicItemSearchService
{
    private readonly IApplicationDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MagicItemSearchService> _logger;

    public MagicItemSearchService(
        IApplicationDbContext db,
        HttpClient httpClient,
        ILogger<MagicItemSearchService> logger)
    {
        _db = db;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<MagicItemSearchResult>> SearchAsync(
        string query, Guid campaignId, int maxResults = 15, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new List<MagicItemSearchResult>();

        // Run all three sources in parallel
        var localTask = SearchLocal(query, campaignId, ct);
        var open5eTask = SearchOpen5e(query, ct);
        var srdTask = SearchDnd5eApi(query, ct);

        await Task.WhenAll(localTask, open5eTask, srdTask);

        var results = new List<MagicItemSearchResult>();
        results.AddRange(localTask.Result);
        results.AddRange(open5eTask.Result);
        results.AddRange(srdTask.Result);

        // Deduplicate by name (case-insensitive), prefer local > open5e > srd
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deduped = new List<MagicItemSearchResult>();

        foreach (var item in results)
        {
            if (seen.Add(item.Name))
                deduped.Add(item);
        }

        return deduped.Take(maxResults).ToList();
    }

    private async Task<List<MagicItemSearchResult>> SearchLocal(
        string query, Guid campaignId, CancellationToken ct)
    {
        try
        {
            var items = await _db.MagicItems
                .Where(m => m.CampaignId == campaignId
                    && EF.Functions.Like(m.Name, $"%{query}%"))
                .OrderBy(m => m.Name)
                .Take(10)
                .Select(m => new MagicItemSearchResult(
                    m.Name,
                    m.Description,
                    m.Rarity.ToString(),
                    m.Category.ToString(),
                    m.Source,
                    "local",
                    m.Id
                ))
                .ToListAsync(ct);

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Local magic item search failed");
            return new List<MagicItemSearchResult>();
        }
    }

    private async Task<List<MagicItemSearchResult>> SearchOpen5e(
        string query, CancellationToken ct)
    {
        try
        {
            var url = $"https://api.open5e.com/v1/magicitems/?search={Uri.EscapeDataString(query)}&limit=10";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "application/json");

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return new List<MagicItemSearchResult>();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var results = new List<MagicItemSearchResult>();

            if (doc.RootElement.TryGetProperty("results", out var arr)
                && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    var name = item.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (name is null) continue;

                    var desc = item.TryGetProperty("desc", out var d) ? d.GetString() : null;
                    var rarity = item.TryGetProperty("rarity", out var r) ? r.GetString() : null;
                    var type = item.TryGetProperty("type", out var t) ? t.GetString() : null;
                    var docSlug = item.TryGetProperty("document__title", out var ds) ? ds.GetString() : "Open5e";

                    // Truncate description for search results
                    if (desc != null && desc.Length > 120)
                        desc = desc[..117] + "...";

                    results.Add(new MagicItemSearchResult(
                        name, desc, rarity, type, docSlug, "open5e", null));
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Open5e magic item search failed");
            return new List<MagicItemSearchResult>();
        }
    }

    private async Task<List<MagicItemSearchResult>> SearchDnd5eApi(
        string query, CancellationToken ct)
    {
        try
        {
            // dnd5eapi.co doesn't have a search endpoint — we fetch the full list
            // and filter client-side. The list is small (~70 SRD items) and can be cached.
            var url = "https://www.dnd5eapi.co/api/magic-items";
            using var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return new List<MagicItemSearchResult>();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var results = new List<MagicItemSearchResult>();

            if (doc.RootElement.TryGetProperty("results", out var arr)
                && arr.ValueKind == JsonValueKind.Array)
            {
                var queryLower = query.ToLowerInvariant();

                foreach (var item in arr.EnumerateArray())
                {
                    var name = item.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (name is null) continue;

                    if (!name.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var index = item.TryGetProperty("index", out var idx) ? idx.GetString() : null;

                    results.Add(new MagicItemSearchResult(
                        name, null, null, null, "SRD", "dnd5eapi", null));

                    if (results.Count >= 5) break;
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "dnd5eapi.co magic item search failed");
            return new List<MagicItemSearchResult>();
        }
    }
}
