namespace DndCampaignManager.Domain.Entities;

public class TreasureTable : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;

    public ICollection<TreasureTableEntry> Entries { get; set; } = new List<TreasureTableEntry>();
}

public class TreasureTableEntry : BaseEntity
{
    public int Weight { get; set; } = 1; // Relative weight for random rolling
    public int? MinRoll { get; set; }
    public int? MaxRoll { get; set; }

    public Guid TreasureTableId { get; set; }
    public TreasureTable TreasureTable { get; set; } = null!;

    public Guid MagicItemId { get; set; }
    public MagicItem MagicItem { get; set; } = null!;
}
