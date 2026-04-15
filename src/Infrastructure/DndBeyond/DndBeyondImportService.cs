using System.Net;
using System.Text.Json;
using DndCampaignManager.Application.Common.Interfaces;
using DndCampaignManager.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DndCampaignManager.Infrastructure.DndBeyond;

public class DndBeyondImportServiceOptions
{
    public const string SectionName = "DndBeyond";

    /// <summary>
    /// Base URL for the character service. The community-known default is:
    /// https://character-service.dndbeyond.com/character/v5/character
    /// This can change without notice — make it configurable.
    /// </summary>
    public string CharacterServiceBaseUrl { get; set; } =
        "https://character-service.dndbeyond.com/character/v5/character";

    /// <summary>
    /// Timeout in seconds for API calls. DDB can be slow under load.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// User-Agent to send. Some endpoints check for this.
    /// </summary>
    public string UserAgent { get; set; } = "DndCampaignManager/1.0";
}

public class DndBeyondImportService : IDndBeyondImportService
{
    private readonly HttpClient _httpClient;
    private readonly DndBeyondImportServiceOptions _options;
    private readonly ILogger<DndBeyondImportService> _logger;

    public DndBeyondImportService(
        HttpClient httpClient,
        IOptions<DndBeyondImportServiceOptions> options,
        ILogger<DndBeyondImportService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DndBeyondImportResult> FetchByCharacterIdAsync(
        long dndBeyondCharacterId, CancellationToken ct = default)
    {
        var url = $"{_options.CharacterServiceBaseUrl}/{dndBeyondCharacterId}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", _options.UserAgent);
            request.Headers.Add("Accept", "application/json");

            using var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var statusMsg = response.StatusCode switch
                {
                    HttpStatusCode.NotFound =>
                        $"Character #{dndBeyondCharacterId} not found on D&D Beyond. Check the ID and make sure the character sheet is public.",
                    HttpStatusCode.Forbidden =>
                        "D&D Beyond rejected the request. The character sheet may be set to private.",
                    HttpStatusCode.TooManyRequests =>
                        "D&D Beyond rate limit reached. Try again in a minute.",
                    HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway =>
                        "D&D Beyond is temporarily unavailable. Your cached character data is still shown.",
                    _ => $"D&D Beyond returned HTTP {(int)response.StatusCode}"
                };

                _logger.LogWarning("DDB fetch failed for #{Id}: {Status} {Msg}",
                    dndBeyondCharacterId, response.StatusCode, statusMsg);

                return new DndBeyondImportResult(false, null, null, statusMsg);
            }

            var rawJson = await response.Content.ReadAsStringAsync(ct);
            return ParseCharacterJson(rawJson, dndBeyondCharacterId);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new DndBeyondImportResult(false, null, null,
                "D&D Beyond request timed out. Your cached character data is still shown.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "DDB network error for #{Id}", dndBeyondCharacterId);
            return new DndBeyondImportResult(false, null, null,
                "Could not reach D&D Beyond. Your cached character data is still shown.");
        }
    }

    public DndBeyondImportResult ParseFromJson(string rawJson)
    {
        return ParseCharacterJson(rawJson, expectedId: null);
    }

    // =========================================================================
    // JSON parsing — handles the known DDB character-service v5 response shape.
    // The response wraps the character in { "id": ..., "success": true, "data": { ... } }
    // or the character is the root object if coming from a /json export.
    // =========================================================================

    private DndBeyondImportResult ParseCharacterJson(string rawJson, long? expectedId)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            // The v5 endpoint wraps the character: { "id": N, "success": true, "data": { ... } }
            JsonElement charData;
            if (root.TryGetProperty("data", out var dataElement)
                && dataElement.ValueKind == JsonValueKind.Object)
            {
                charData = dataElement;
            }
            else
            {
                // Might be a raw character JSON (from /json export or user paste)
                charData = root;
            }

            var characterId = ExtractLong(charData, "id") ?? expectedId ?? 0;
            var name = ExtractString(charData, "name") ?? "Unknown";

            // Race — can be nested in "race" object or as "racialTraits"
            string? race = null;
            if (charData.TryGetProperty("race", out var raceElement))
            {
                race = raceElement.ValueKind == JsonValueKind.Object
                    ? ExtractString(raceElement, "fullName")
                        ?? ExtractString(raceElement, "baseName")
                    : raceElement.GetString();
            }

            // Class — DDB stores classes as an array
            string? className = null;
            int totalLevel = 0;
            if (charData.TryGetProperty("classes", out var classesElement)
                && classesElement.ValueKind == JsonValueKind.Array)
            {
                var classNames = new List<string>();
                foreach (var cls in classesElement.EnumerateArray())
                {
                    var clsName = ExtractString(cls, "definition", "name");
                    var clsLevel = ExtractInt(cls, "level") ?? 0;
                    totalLevel += clsLevel;
                    if (clsName != null) classNames.Add(clsName);
                }
                className = string.Join(" / ", classNames);
            }

            if (totalLevel == 0)
                totalLevel = ExtractInt(charData, "level") ?? 1;

            // Avatar
            var avatarUrl = ExtractString(charData, "decorations", "avatarUrl")
                ?? ExtractString(charData, "avatarUrl");

            // Stats — DDB stores base stats in a "stats" array
            int? str = null, dex = null, con = null, intl = null, wis = null, cha = null;
            if (charData.TryGetProperty("stats", out var statsElement)
                && statsElement.ValueKind == JsonValueKind.Array)
            {
                // Stats array is ordered: STR(1), DEX(2), CON(3), INT(4), WIS(5), CHA(6)
                var statsArray = statsElement.EnumerateArray().ToList();
                if (statsArray.Count >= 6)
                {
                    str = ExtractInt(statsArray[0], "value");
                    dex = ExtractInt(statsArray[1], "value");
                    con = ExtractInt(statsArray[2], "value");
                    intl = ExtractInt(statsArray[3], "value");
                    wis = ExtractInt(statsArray[4], "value");
                    cha = ExtractInt(statsArray[5], "value");
                }
            }

            // HP — base + constitution modifier × level, or the overridden value
            int? hp = ExtractInt(charData, "baseHitPoints")
                ?? ExtractInt(charData, "overrideHitPoints");

            // AC is calculated client-side by DDB, not directly in the JSON.
            // We store null and let the frontend show "—" or calculate it later.
            int? ac = null;

            // Traits — DDB stores under "traits" object or "notes" object
            string? personalityTraits = null, ideals = null, bonds = null, flaws = null;
            if (charData.TryGetProperty("traits", out var traitsElement)
                && traitsElement.ValueKind == JsonValueKind.Object)
            {
                personalityTraits = ExtractString(traitsElement, "personalityTraits");
                ideals = ExtractString(traitsElement, "ideals");
                bonds = ExtractString(traitsElement, "bonds");
                flaws = ExtractString(traitsElement, "flaws");
            }
            // Some JSON formats nest these under "notes"
            if (personalityTraits is null
                && charData.TryGetProperty("notes", out var notesElement)
                && notesElement.ValueKind == JsonValueKind.Object)
            {
                personalityTraits ??= ExtractString(notesElement, "personalityTraits");
                ideals ??= ExtractString(notesElement, "ideals");
                bonds ??= ExtractString(notesElement, "bonds");
                flaws ??= ExtractString(notesElement, "flaws");
            }

            // Inventory — DDB stores under "inventory" array
            var inventoryItems = new List<DndBeyondInventoryItem>();
            if (charData.TryGetProperty("inventory", out var invElement)
                && invElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in invElement.EnumerateArray())
                {
                    var def = item.TryGetProperty("definition", out var defEl)
                        ? defEl
                        : item;

                    var itemName = ExtractString(def, "name");
                    if (itemName is null) continue;

                    var itemId = ExtractLong(def, "id");
                    var desc = ExtractString(def, "description");
                    var qty = ExtractInt(item, "quantity") ?? 1;
                    var weight = def.TryGetProperty("weight", out var wEl)
                        && wEl.ValueKind == JsonValueKind.Number
                        ? wEl.GetDouble()
                        : (double?)null;

                    var equipped = item.TryGetProperty("equipped", out var eqEl)
                        && eqEl.ValueKind == JsonValueKind.True;
                    var attuned = item.TryGetProperty("isAttuned", out var atEl)
                        && atEl.ValueKind == JsonValueKind.True;
                    var magic = def.TryGetProperty("magic", out var magEl)
                        && magEl.ValueKind == JsonValueKind.True;

                    var rarity = ExtractString(def, "rarity");
                    var itemType = ExtractString(def, "type")
                        ?? ExtractString(def, "filterType");

                    inventoryItems.Add(new DndBeyondInventoryItem(
                        DndBeyondItemId: itemId,
                        Name: itemName,
                        Description: desc,
                        Quantity: qty,
                        Weight: weight,
                        IsEquipped: equipped,
                        IsAttuned: attuned,
                        IsMagic: magic,
                        Rarity: rarity,
                        ItemType: itemType
                    ));
                }
            }

            var ddbUrl = $"https://www.dndbeyond.com/characters/{characterId}";

            var data = new DndBeyondCharacterData(
                DndBeyondCharacterId: characterId,
                Name: name,
                Race: race,
                ClassName: className,
                Level: totalLevel,
                AvatarUrl: avatarUrl,
                HitPoints: hp,
                ArmorClass: ac,
                Strength: str,
                Dexterity: dex,
                Constitution: con,
                Intelligence: intl,
                Wisdom: wis,
                Charisma: cha,
                DndBeyondUrl: ddbUrl,
                PersonalityTraits: personalityTraits,
                Ideals: ideals,
                Bonds: bonds,
                Flaws: flaws,
                Inventory: inventoryItems
            );

            return new DndBeyondImportResult(true, data, rawJson, null);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse DDB character JSON");
            return new DndBeyondImportResult(false, null, rawJson,
                "Could not parse the D&D Beyond character JSON. The format may have changed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error parsing DDB character JSON");
            return new DndBeyondImportResult(false, null, rawJson,
                "An unexpected error occurred while parsing the character data.");
        }
    }

    // =========================================================================
    // JSON helpers — DDB's schema is inconsistent, so we defensively extract
    // =========================================================================

    private static string? ExtractString(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var key in path)
        {
            if (!current.TryGetProperty(key, out var next))
                return null;
            current = next;
        }
        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }

    private static int? ExtractInt(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var key in path)
        {
            if (!current.TryGetProperty(key, out var next))
                return null;
            current = next;
        }
        return current.ValueKind == JsonValueKind.Number ? current.GetInt32() : null;
    }

    private static long? ExtractLong(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var key in path)
        {
            if (!current.TryGetProperty(key, out var next))
                return null;
            current = next;
        }
        return current.ValueKind == JsonValueKind.Number ? current.GetInt64() : null;
    }
}
