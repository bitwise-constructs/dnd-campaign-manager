using DndCampaignManager.Domain.Enums;

namespace DndCampaignManager.Domain.Entities;

public class Character : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string PlayerUserId { get; set; } = string.Empty; // Entra Object ID
    public string? PlayerDisplayName { get; set; }
    public CharacterClass Class { get; set; }
    public int Level { get; set; } = 1;
    public string? Race { get; set; }
    public string? ImageUrl { get; set; }

    // --- D&D Beyond link ---
    public long? DndBeyondCharacterId { get; set; }
    public string? DndBeyondUrl { get; set; }
    public DndBeyondSyncStatus DndBeyondSyncStatus { get; set; } = DndBeyondSyncStatus.Unlinked;
    public DateTime? DndBeyondLastSyncedAt { get; set; }
    public string? DndBeyondLastSyncError { get; set; }

    // --- Extended stats from DDB (nullable — only populated after a successful sync) ---
    public int? HitPoints { get; set; }
    public int? ArmorClass { get; set; }
    public int? Strength { get; set; }
    public int? Dexterity { get; set; }
    public int? Constitution { get; set; }
    public int? Intelligence { get; set; }
    public int? Wisdom { get; set; }
    public int? Charisma { get; set; }

    // Raw JSON snapshot from last successful DDB fetch — fallback source of truth
    public string? DndBeyondRawJson { get; set; }

    // --- Roleplay / personality (synced from DDB traits object, or entered manually) ---
    public string? PersonalityTraits { get; set; }
    public string? Ideals { get; set; }
    public string? Bonds { get; set; }
    public string? Flaws { get; set; }
    public string? Backstory { get; set; }

    public Guid CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;

    public CharacterPrivacySettings? PrivacySettings { get; set; }
    public ICollection<WishlistItem> Wishlist { get; set; } = new List<WishlistItem>();
    public ICollection<InventoryItem> Inventory { get; set; } = new List<InventoryItem>();
}
