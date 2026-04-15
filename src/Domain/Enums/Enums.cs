namespace DndCampaignManager.Domain.Enums;

public enum Rarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    VeryRare = 3,
    Legendary = 4,
    Artifact = 5
}

public enum ItemCategory
{
    Armor = 0,
    Weapon = 1,
    Potion = 2,
    Ring = 3,
    Rod = 4,
    Scroll = 5,
    Staff = 6,
    Wand = 7,
    WondrousItem = 8
}

public enum CharacterClass
{
    Barbarian = 0,
    Bard = 1,
    Cleric = 2,
    Druid = 3,
    Fighter = 4,
    Monk = 5,
    Paladin = 6,
    Ranger = 7,
    Rogue = 8,
    Sorcerer = 9,
    Warlock = 10,
    Wizard = 11,
    Artificer = 12,
    BloodHunter = 13
}

public enum DndBeyondSyncStatus
{
    Unlinked = 0,    // No DDB character linked
    Synced = 1,      // Last sync succeeded
    SyncFailed = 2,  // Last sync attempt failed (stale data shown)
    Syncing = 3      // Sync in progress
}
