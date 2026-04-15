namespace DndCampaignManager.Application.Common.Interfaces;

/// <summary>
/// Searches for magic items across multiple sources:
/// 1. Local campaign magic items (database)
/// 2. Open5e API (SRD + OGL third-party content)
/// 3. dnd5eapi.co (SRD content, fallback)
///
/// Results are merged and deduplicated by name.
/// </summary>
public interface IMagicItemSearchService
{
    Task<List<MagicItemSearchResult>> SearchAsync(
        string query,
        Guid campaignId,
        int maxResults = 15,
        CancellationToken ct = default);
}

public record MagicItemSearchResult(
    string Name,
    string? Description,
    string? Rarity,
    string? Category,
    string? Source,          // "DMG p.170", "SRD", "Tome of Beasts", etc.
    string SearchSource,     // "local", "open5e", "dnd5eapi"
    // If from local DB, include the ID so it can be linked directly
    Guid? LocalMagicItemId
);
