namespace DndCampaignManager.Domain.Entities;

public class WishlistItem : BaseEntity
{
    public int Priority { get; set; } // 1 = most wanted
    public string? Notes { get; set; }
    public int Weight { get; set; } = 1; // DM-assigned weight for treasure table generation

    // Either linked to a known MagicItem OR a plain-text custom entry (not both required)
    public Guid? MagicItemId { get; set; }
    public MagicItem? MagicItem { get; set; }

    // Custom item fields — used when MagicItemId is null
    public string? CustomItemName { get; set; }
    public string? CustomItemRarity { get; set; } // Free text: "Rare", "Uncommon", etc.

    // Belongs to either a Character wishlist OR the DM's general pool (not both)
    public Guid? CharacterId { get; set; }
    public Character? Character { get; set; }

    public Guid? DmPoolId { get; set; }
    public DmItemPool? DmPool { get; set; }

    // Computed display name for convenience
    public string DisplayName => MagicItem?.Name ?? CustomItemName ?? "Unknown item";
    public bool IsCustom => MagicItemId == null;
}
