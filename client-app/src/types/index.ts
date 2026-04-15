export enum Rarity {
  Common = 0,
  Uncommon = 1,
  Rare = 2,
  VeryRare = 3,
  Legendary = 4,
  Artifact = 5,
}

export enum ItemCategory {
  Armor = 0,
  Weapon = 1,
  Potion = 2,
  Ring = 3,
  Rod = 4,
  Scroll = 5,
  Staff = 6,
  Wand = 7,
  WondrousItem = 8,
}

export enum CharacterClass {
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
  BloodHunter = 13,
}

export const RarityLabel: Record<Rarity, string> = {
  [Rarity.Common]: "Common",
  [Rarity.Uncommon]: "Uncommon",
  [Rarity.Rare]: "Rare",
  [Rarity.VeryRare]: "Very Rare",
  [Rarity.Legendary]: "Legendary",
  [Rarity.Artifact]: "Artifact",
};

export const RarityColor: Record<Rarity, string> = {
  [Rarity.Common]: "#9ca3af",
  [Rarity.Uncommon]: "#22c55e",
  [Rarity.Rare]: "#3b82f6",
  [Rarity.VeryRare]: "#a855f7",
  [Rarity.Legendary]: "#f59e0b",
  [Rarity.Artifact]: "#ef4444",
};

export const CategoryLabel: Record<ItemCategory, string> = {
  [ItemCategory.Armor]: "Armor",
  [ItemCategory.Weapon]: "Weapon",
  [ItemCategory.Potion]: "Potion",
  [ItemCategory.Ring]: "Ring",
  [ItemCategory.Rod]: "Rod",
  [ItemCategory.Scroll]: "Scroll",
  [ItemCategory.Staff]: "Staff",
  [ItemCategory.Wand]: "Wand",
  [ItemCategory.WondrousItem]: "Wondrous Item",
};

export const ClassLabel: Record<CharacterClass, string> = {
  [CharacterClass.Barbarian]: "Barbarian",
  [CharacterClass.Bard]: "Bard",
  [CharacterClass.Cleric]: "Cleric",
  [CharacterClass.Druid]: "Druid",
  [CharacterClass.Fighter]: "Fighter",
  [CharacterClass.Monk]: "Monk",
  [CharacterClass.Paladin]: "Paladin",
  [CharacterClass.Ranger]: "Ranger",
  [CharacterClass.Rogue]: "Rogue",
  [CharacterClass.Sorcerer]: "Sorcerer",
  [CharacterClass.Warlock]: "Warlock",
  [CharacterClass.Wizard]: "Wizard",
  [CharacterClass.Artificer]: "Artificer",
  [CharacterClass.BloodHunter]: "Blood Hunter",
};

export enum DndBeyondSyncStatus {
  Unlinked = 0,
  Synced = 1,
  SyncFailed = 2,
  Syncing = 3,
}

export const SyncStatusLabel: Record<DndBeyondSyncStatus, string> = {
  [DndBeyondSyncStatus.Unlinked]: "Not linked",
  [DndBeyondSyncStatus.Synced]: "Synced",
  [DndBeyondSyncStatus.SyncFailed]: "Sync failed",
  [DndBeyondSyncStatus.Syncing]: "Syncing…",
};

export interface CharacterDto {
  id: string;
  name: string;
  playerUserId: string;
  playerDisplayName: string | null;
  class: CharacterClass;
  level: number;
  race: string | null;
  imageUrl: string | null;
  campaignId: string;
  isOwner: boolean;
  // D&D Beyond integration
  dndBeyondCharacterId: number | null;
  dndBeyondUrl: string | null;
  dndBeyondSyncStatus: DndBeyondSyncStatus;
  dndBeyondLastSyncedAt: string | null;
  dndBeyondLastSyncError: string | null;
  // Privacy-gated fields — null means hidden from this viewer
  hitPoints: number | null;
  armorClass: number | null;
  strength: number | null;
  dexterity: number | null;
  constitution: number | null;
  intelligence: number | null;
  wisdom: number | null;
  charisma: number | null;
  personalityTraits: string | null;
  ideals: string | null;
  bonds: string | null;
  flaws: string | null;
  backstory: string | null;
  inventory: InventoryItemDto[] | null;
  // Only present for the owning player
  privacySettings: CharacterPrivacySettingsDto | null;
}

export interface CharacterPrivacySettingsDto {
  showAbilityScores: boolean;
  showHitPoints: boolean;
  showArmorClass: boolean;
  showInventory: boolean;
  showPersonalityTraits: boolean;
  showIdeals: boolean;
  showBonds: boolean;
  showFlaws: boolean;
  showWishlist: boolean;
  showBackstory: boolean;
  showAll: boolean;
}

export interface InventoryItemDto {
  id: string;
  name: string;
  description: string | null;
  quantity: number;
  weight: number | null;
  isEquipped: boolean;
  isAttuned: boolean;
  isMagic: boolean;
  rarity: string | null;
  itemType: string | null;
  notes: string | null;
}

export interface DndBeyondSyncResult {
  success: boolean;
  errorMessage: string | null;
  lastSyncedAt: string | null;
}

export interface DndBeyondBatchSyncResult {
  characterId: string;
  characterName: string;
  success: boolean;
  errorMessage: string | null;
}

export interface MagicItemDto {
  id: string;
  name: string;
  description: string | null;
  rarity: Rarity;
  category: ItemCategory;
  requiresAttunement: boolean;
  attunementRequirement: string | null;
  source: string | null;
  campaignId: string;
}

export interface WishlistItemDto {
  id: string;
  priority: number;
  notes: string | null;
  weight: number;
  characterId: string | null;
  characterName: string | null;
  magicItemId: string | null;
  magicItemName: string | null;
  magicItemRarity: Rarity | null;
  customItemName: string | null;
  customItemRarity: string | null;
  isCustom: boolean;
  displayName: string;
}

export interface DmItemPoolDto {
  id: string;
  campaignId: string;
  items: WishlistItemDto[];
}

export interface CampaignWishlistsDto {
  dmPool: DmItemPoolDto;
  characterWishlists: Record<string, WishlistItemDto[]>;
}

export interface PickResultDto {
  picks: WishlistItemDto[];
  weightsUsed: boolean;
}

export interface MagicItemSearchResult {
  name: string;
  description: string | null;
  rarity: string | null;
  category: string | null;
  source: string | null;
  searchSource: "local" | "open5e" | "dnd5eapi";
  localMagicItemId: string | null;
}

export interface TreasureTableDto {
  id: string;
  name: string;
  description: string | null;
  campaignId: string;
  entries: TreasureTableEntryDto[];
}

export interface TreasureTableEntryDto {
  id: string;
  weight: number;
  minRoll: number | null;
  maxRoll: number | null;
  magicItemId: string;
  magicItemName: string;
  magicItemRarity: Rarity;
  magicItemCategory: ItemCategory;
}

export interface SelectedItemForTable {
  magicItemId: string;
  weight: number;
}

export interface CampaignDto {
  id: string;
  name: string;
  description: string | null;
  dmUserId: string;
  characterCount: number;
  magicItemCount: number;
}
