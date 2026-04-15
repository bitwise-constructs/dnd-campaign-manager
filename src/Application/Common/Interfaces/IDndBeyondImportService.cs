using DndCampaignManager.Application.Common.Models;

namespace DndCampaignManager.Application.Common.Interfaces;

/// <summary>
/// Abstraction over D&D Beyond character data retrieval.
/// Infrastructure provides two implementations:
///   1. DndBeyondApiImportService — fetches from the undocumented character-service endpoint
///   2. DndBeyondJsonImportService — parses a user-uploaded JSON blob
/// Both return the same DndBeyondCharacterData, which the Application layer maps onto our Character entity.
/// </summary>
public interface IDndBeyondImportService
{
    /// <summary>
    /// Fetch character data from D&D Beyond by character ID.
    /// Returns null if the endpoint is unreachable or returns an error.
    /// </summary>
    Task<DndBeyondImportResult> FetchByCharacterIdAsync(long dndBeyondCharacterId, CancellationToken ct = default);

    /// <summary>
    /// Parse character data from a raw JSON string (user-uploaded fallback).
    /// </summary>
    DndBeyondImportResult ParseFromJson(string rawJson);
}

public record DndBeyondImportResult(
    bool Success,
    DndBeyondCharacterData? Data,
    string? RawJson,
    string? ErrorMessage
);

public record DndBeyondCharacterData(
    long DndBeyondCharacterId,
    string Name,
    string? Race,
    string? ClassName,
    int Level,
    string? AvatarUrl,
    int? HitPoints,
    int? ArmorClass,
    int? Strength,
    int? Dexterity,
    int? Constitution,
    int? Intelligence,
    int? Wisdom,
    int? Charisma,
    string DndBeyondUrl,
    // Roleplay traits — DDB stores these under data.traits
    string? PersonalityTraits,
    string? Ideals,
    string? Bonds,
    string? Flaws,
    // Inventory items
    List<DndBeyondInventoryItem> Inventory
);

public record DndBeyondInventoryItem(
    long? DndBeyondItemId,
    string Name,
    string? Description,
    int Quantity,
    double? Weight,
    bool IsEquipped,
    bool IsAttuned,
    bool IsMagic,
    string? Rarity,
    string? ItemType
);
