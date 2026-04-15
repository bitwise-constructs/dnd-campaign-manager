namespace DndCampaignManager.Domain.Entities;

/// <summary>
/// The DM's general-purpose item pool for a campaign.
/// Contains items the DM wants available as loot independent of player wishlists.
/// One pool per campaign.
/// </summary>
public class DmItemPool : BaseEntity
{
    public Guid CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;

    public ICollection<WishlistItem> Items { get; set; } = new List<WishlistItem>();
}
