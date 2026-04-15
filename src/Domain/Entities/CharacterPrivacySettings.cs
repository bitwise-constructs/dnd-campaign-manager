namespace DndCampaignManager.Domain.Entities;

/// <summary>
/// Per-character privacy settings controlling what other players can see.
/// The DM always sees everything regardless of these settings.
/// The owning player always sees their own full sheet.
/// Each field defaults to Private — the player opts IN to sharing.
/// </summary>
public class CharacterPrivacySettings : BaseEntity
{
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;

    // --- Visibility toggles (true = visible to other players) ---
    public bool ShowAbilityScores { get; set; } = false;
    public bool ShowHitPoints { get; set; } = false;
    public bool ShowArmorClass { get; set; } = false;
    public bool ShowInventory { get; set; } = false;
    public bool ShowPersonalityTraits { get; set; } = false;
    public bool ShowIdeals { get; set; } = false;
    public bool ShowBonds { get; set; } = false;
    public bool ShowFlaws { get; set; } = false;
    public bool ShowWishlist { get; set; } = false;
    public bool ShowBackstory { get; set; } = false;

    // Convenience: share everything at once
    public bool ShowAll { get; set; } = false;
}
