namespace DndCampaignManager.Domain.Entities;

public class Campaign : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DmUserId { get; set; } = string.Empty; // Entra Object ID

    public ICollection<Character> Characters { get; set; } = new List<Character>();
    public ICollection<MagicItem> MagicItems { get; set; } = new List<MagicItem>();
    public ICollection<TreasureTable> TreasureTables { get; set; } = new List<TreasureTable>();
    public DmItemPool? DmItemPool { get; set; }
}
