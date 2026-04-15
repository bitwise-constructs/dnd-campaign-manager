namespace DndCampaignManager.Domain.Entities;

/// <summary>
/// An item held in a character's inventory.
/// Can be synced from D&D Beyond or entered manually.
/// </summary>
public class InventoryItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Quantity { get; set; } = 1;
    public double? Weight { get; set; }
    public bool IsEquipped { get; set; }
    public bool IsAttuned { get; set; }
    public bool IsMagic { get; set; }
    public string? Rarity { get; set; }         // Free-text from DDB; may not match our enum
    public string? ItemType { get; set; }        // "Weapon", "Armor", "Adventuring Gear", etc.
    public string? Notes { get; set; }

    // DDB source tracking
    public long? DndBeyondItemId { get; set; }   // null if manually entered

    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
}
