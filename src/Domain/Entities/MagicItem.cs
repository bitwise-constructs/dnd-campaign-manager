using DndCampaignManager.Domain.Enums;

namespace DndCampaignManager.Domain.Entities;

public class MagicItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Rarity Rarity { get; set; }
    public ItemCategory Category { get; set; }
    public bool RequiresAttunement { get; set; }
    public string? AttunementRequirement { get; set; }
    public string? Source { get; set; } // e.g. "DMG p.152", "XGE p.34"

    public Guid CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;

    public ICollection<WishlistItem> WishlistEntries { get; set; } = new List<WishlistItem>();
    public ICollection<TreasureTableEntry> TreasureTableEntries { get; set; } = new List<TreasureTableEntry>();
}
