using DndCampaignManager.Domain.Enums;

namespace DndCampaignManager.Application.Common.Models;

/// <summary>
/// Character DTO returned from API. Privacy-sensitive fields are nullable — they return
/// null when the requesting user doesn't have visibility (not owner, not DM, field not shared).
/// The 'isOwner' flag tells the frontend whether to show edit controls.
/// </summary>
public record CharacterDto(
    Guid Id,
    string Name,
    string PlayerUserId,
    string? PlayerDisplayName,
    CharacterClass Class,
    int Level,
    string? Race,
    string? ImageUrl,
    Guid CampaignId,
    bool IsOwner,
    // D&D Beyond integration
    long? DndBeyondCharacterId,
    string? DndBeyondUrl,
    DndBeyondSyncStatus DndBeyondSyncStatus,
    DateTime? DndBeyondLastSyncedAt,
    string? DndBeyondLastSyncError,
    // Privacy-gated fields — null means "hidden from this viewer"
    int? HitPoints,
    int? ArmorClass,
    int? Strength,
    int? Dexterity,
    int? Constitution,
    int? Intelligence,
    int? Wisdom,
    int? Charisma,
    string? PersonalityTraits,
    string? Ideals,
    string? Bonds,
    string? Flaws,
    string? Backstory,
    List<InventoryItemDto>? Inventory,
    CharacterPrivacySettingsDto? PrivacySettings
);

public record CharacterPrivacySettingsDto(
    bool ShowAbilityScores,
    bool ShowHitPoints,
    bool ShowArmorClass,
    bool ShowInventory,
    bool ShowPersonalityTraits,
    bool ShowIdeals,
    bool ShowBonds,
    bool ShowFlaws,
    bool ShowWishlist,
    bool ShowBackstory,
    bool ShowAll
);

public record InventoryItemDto(
    Guid Id,
    string Name,
    string? Description,
    int Quantity,
    double? Weight,
    bool IsEquipped,
    bool IsAttuned,
    bool IsMagic,
    string? Rarity,
    string? ItemType,
    string? Notes
);

public record MagicItemDto(
    Guid Id,
    string Name,
    string? Description,
    Rarity Rarity,
    ItemCategory Category,
    bool RequiresAttunement,
    string? AttunementRequirement,
    string? Source,
    Guid CampaignId
);

public record WishlistItemDto(
    Guid Id,
    int Priority,
    string? Notes,
    int Weight,
    // Owner context — null for DM pool items
    Guid? CharacterId,
    string? CharacterName,
    // Linked magic item — null for custom items
    Guid? MagicItemId,
    string? MagicItemName,
    Rarity? MagicItemRarity,
    // Custom item fields — populated when MagicItemId is null
    string? CustomItemName,
    string? CustomItemRarity,
    bool IsCustom,
    // Computed display name (either MagicItem.Name or CustomItemName)
    string DisplayName
);

public record DmItemPoolDto(
    Guid Id,
    Guid CampaignId,
    List<WishlistItemDto> Items
);

/// <summary>
/// Used by the "Pick top N" feature — rolls against selected items using weights
/// </summary>
public record PickResultDto(
    List<WishlistItemDto> Picks,
    bool WeightsUsed
);

public record TreasureTableDto(
    Guid Id,
    string Name,
    string? Description,
    Guid CampaignId,
    List<TreasureTableEntryDto> Entries
);

public record TreasureTableEntryDto(
    Guid Id,
    int Weight,
    int? MinRoll,
    int? MaxRoll,
    Guid MagicItemId,
    string MagicItemName,
    Rarity MagicItemRarity,
    ItemCategory MagicItemCategory
);

public record CampaignDto(
    Guid Id,
    string Name,
    string? Description,
    string DmUserId,
    int CharacterCount,
    int MagicItemCount
);
