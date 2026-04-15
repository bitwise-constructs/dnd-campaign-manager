import { useState } from "react";
import { MsalProvider, useIsAuthenticated, useMsal } from "@azure/msal-react";
import { PublicClientApplication } from "@azure/msal-browser";
import { msalConfig } from "./services/authConfig";
import { setMsalInstance } from "./services/api";
import { CampaignProvider } from "./context/CampaignContext";
import { AppShell } from "./components/layout/AppShell";
import { CharacterList } from "./components/characters/CharacterList";
import { MagicItemsTable } from "./components/magic-items/MagicItemsTable";
import { WishlistView } from "./components/wishlists/WishlistView";
import { TreasureTableGenerator, TreasureTableView } from "./components/treasure-tables/TreasureTableView";
import type {
  CharacterDto,
  MagicItemDto,
  WishlistItemDto,
  TreasureTableDto,
  TreasureTableEntryDto,
} from "./types";
import { Rarity, ItemCategory, CharacterClass, DndBeyondSyncStatus } from "./types";
import "./index.css";

// Initialize MSAL
const msalInstance = new PublicClientApplication(msalConfig);
setMsalInstance(msalInstance);

// ----- Demo Data (remove when connecting to real API) -----
const DEMO_CAMPAIGN_ID = "00000000-0000-0000-0000-000000000001";

const demoCharactersInitial: CharacterDto[] = [
  {
    id: "c1", name: "Thorin Ironforge", playerUserId: "u1", playerDisplayName: "Alex",
    class: CharacterClass.Fighter, level: 8, race: "Dwarf", imageUrl: null,
    campaignId: DEMO_CAMPAIGN_ID, isOwner: false,
    dndBeyondCharacterId: 12345678, dndBeyondUrl: "https://www.dndbeyond.com/characters/12345678",
    dndBeyondSyncStatus: DndBeyondSyncStatus.Synced,
    dndBeyondLastSyncedAt: new Date(Date.now() - 3600000).toISOString(),
    dndBeyondLastSyncError: null,
    hitPoints: 76, armorClass: 18,
    strength: 18, dexterity: 12, constitution: 16, intelligence: 10, wisdom: 13, charisma: 8,
    personalityTraits: "I judge people by their actions, not their words.",
    ideals: "Honor. If I dishonor myself, I dishonor my whole clan.",
    bonds: "I will someday get revenge on the corrupt temple hierarchy who branded me a heretic.",
    flaws: "I am inflexible in my thinking.",
    backstory: "Thorin was exiled from his mountain hold after refusing to bow to a corrupt king. He seeks to prove his honor through deeds, not words.",
    inventory: [
      { id: "i1", name: "Longsword +1", description: "A finely crafted blade", quantity: 1, weight: 3, isEquipped: true, isAttuned: false, isMagic: true, rarity: "Uncommon", itemType: "Weapon", notes: null },
      { id: "i2", name: "Shield", description: null, quantity: 1, weight: 6, isEquipped: true, isAttuned: false, isMagic: false, rarity: null, itemType: "Armor", notes: null },
      { id: "i3", name: "Chain Mail", description: null, quantity: 1, weight: 55, isEquipped: true, isAttuned: false, isMagic: false, rarity: null, itemType: "Armor", notes: null },
      { id: "i4", name: "Potion of Healing", description: "Restores 2d4+2 HP", quantity: 3, weight: 0.5, isEquipped: false, isAttuned: false, isMagic: true, rarity: "Common", itemType: "Potion", notes: null },
    ],
    privacySettings: null, // Not owner in demo
  },
  {
    id: "c2", name: "Lyra Moonwhisper", playerUserId: "u2", playerDisplayName: "Jordan",
    class: CharacterClass.Wizard, level: 8, race: "Elf", imageUrl: null,
    campaignId: DEMO_CAMPAIGN_ID, isOwner: true, // Demo: this is "your" character
    dndBeyondCharacterId: 87654321, dndBeyondUrl: "https://www.dndbeyond.com/characters/87654321",
    dndBeyondSyncStatus: DndBeyondSyncStatus.SyncFailed,
    dndBeyondLastSyncedAt: new Date(Date.now() - 86400000 * 3).toISOString(),
    dndBeyondLastSyncError: "D&D Beyond endpoint unreachable. Your cached character data is still shown.",
    hitPoints: 42, armorClass: 15,
    strength: 8, dexterity: 14, constitution: 12, intelligence: 20, wisdom: 14, charisma: 10,
    personalityTraits: "I use polysyllabic words that convey the impression of great erudition.",
    ideals: "Knowledge. The path to power and self-improvement is through knowledge.",
    bonds: "I have an ancient text that holds terrible secrets about the planes.",
    flaws: "I overlook obvious solutions in favor of complicated ones.",
    backstory: "Lyra left the Elven Academy after discovering a banned text in the restricted section. She seeks to understand the cosmic forces it describes before darker powers find it first.",
    inventory: [
      { id: "i5", name: "Arcane Focus (Crystal)", description: null, quantity: 1, weight: 1, isEquipped: true, isAttuned: false, isMagic: false, rarity: null, itemType: "Adventuring Gear", notes: null },
      { id: "i6", name: "Spellbook", description: "Contains all prepared spells", quantity: 1, weight: 3, isEquipped: false, isAttuned: false, isMagic: false, rarity: null, itemType: "Adventuring Gear", notes: null },
      { id: "i7", name: "Pearl of Power", description: "Regain one spell slot", quantity: 1, weight: 0, isEquipped: true, isAttuned: true, isMagic: true, rarity: "Uncommon", itemType: "Wondrous Item", notes: null },
    ],
    privacySettings: {
      showAbilityScores: true,
      showHitPoints: true,
      showArmorClass: true,
      showInventory: false,
      showPersonalityTraits: true,
      showIdeals: true,
      showBonds: false,
      showFlaws: false,
      showWishlist: true,
      showBackstory: false,
      showAll: false,
    },
  },
  {
    id: "c3", name: "Bramble Thornwick", playerUserId: "u3", playerDisplayName: "Sam",
    class: CharacterClass.Druid, level: 7, race: "Halfling", imageUrl: null,
    campaignId: DEMO_CAMPAIGN_ID, isOwner: false,
    dndBeyondCharacterId: null, dndBeyondUrl: null,
    dndBeyondSyncStatus: DndBeyondSyncStatus.Unlinked,
    dndBeyondLastSyncedAt: null, dndBeyondLastSyncError: null,
    hitPoints: null, armorClass: null,
    strength: null, dexterity: null, constitution: null,
    intelligence: null, wisdom: null, charisma: null,
    personalityTraits: null, ideals: null, bonds: null, flaws: null,
    backstory: null, inventory: null,
    privacySettings: null,
  },
  {
    id: "c4", name: "Kael Shadowmend", playerUserId: "u4", playerDisplayName: "Morgan",
    class: CharacterClass.Rogue, level: 8, race: "Tiefling", imageUrl: null,
    campaignId: DEMO_CAMPAIGN_ID, isOwner: false,
    dndBeyondCharacterId: 55555555, dndBeyondUrl: "https://www.dndbeyond.com/characters/55555555",
    dndBeyondSyncStatus: DndBeyondSyncStatus.Synced,
    dndBeyondLastSyncedAt: new Date(Date.now() - 7200000).toISOString(),
    dndBeyondLastSyncError: null,
    hitPoints: 58, armorClass: 16,
    strength: 10, dexterity: 20, constitution: 14, intelligence: 12, wisdom: 12, charisma: 16,
    // Privacy: Kael's player shared stats and AC but kept traits private
    personalityTraits: null, ideals: null, bonds: null, flaws: null,
    backstory: null,
    inventory: null, // Hidden
    privacySettings: null,
  },
];

const demoItems: MagicItemDto[] = [
  { id: "m1", name: "Flame Tongue Longsword", description: "While aflame, deals extra fire damage", rarity: Rarity.Rare, category: ItemCategory.Weapon, requiresAttunement: true, attunementRequirement: null, source: "DMG p.170", campaignId: DEMO_CAMPAIGN_ID },
  { id: "m2", name: "Cloak of Displacement", description: "Projects an illusion making you harder to hit", rarity: Rarity.Rare, category: ItemCategory.WondrousItem, requiresAttunement: true, attunementRequirement: null, source: "DMG p.158", campaignId: DEMO_CAMPAIGN_ID },
  { id: "m3", name: "Staff of the Woodlands", description: "A powerful druidic focus", rarity: Rarity.Rare, category: ItemCategory.Staff, requiresAttunement: true, attunementRequirement: "Druid", source: "DMG p.204", campaignId: DEMO_CAMPAIGN_ID },
  { id: "m4", name: "Bag of Holding", description: "Interior is larger than its exterior dimensions", rarity: Rarity.Uncommon, category: ItemCategory.WondrousItem, requiresAttunement: false, attunementRequirement: null, source: "DMG p.153", campaignId: DEMO_CAMPAIGN_ID },
  { id: "m5", name: "Ring of Protection", description: "+1 bonus to AC and saving throws", rarity: Rarity.Rare, category: ItemCategory.Ring, requiresAttunement: true, attunementRequirement: null, source: "DMG p.191", campaignId: DEMO_CAMPAIGN_ID },
  { id: "m6", name: "Potion of Greater Healing", description: "Restores 4d4+4 hit points", rarity: Rarity.Uncommon, category: ItemCategory.Potion, requiresAttunement: false, attunementRequirement: null, source: "DMG p.187", campaignId: DEMO_CAMPAIGN_ID },
  { id: "m7", name: "Boots of Speed", description: "Click heels to double walking speed", rarity: Rarity.Rare, category: ItemCategory.WondrousItem, requiresAttunement: true, attunementRequirement: null, source: "DMG p.155", campaignId: DEMO_CAMPAIGN_ID },
  { id: "m8", name: "Vorpal Sword", description: "Severs heads on a natural 20", rarity: Rarity.Legendary, category: ItemCategory.Weapon, requiresAttunement: true, attunementRequirement: null, source: "DMG p.209", campaignId: DEMO_CAMPAIGN_ID },
  { id: "m9", name: "Wand of Fireballs", description: "7 charges, expend to cast Fireball", rarity: Rarity.Rare, category: ItemCategory.Wand, requiresAttunement: true, attunementRequirement: "Spellcaster", source: "DMG p.210", campaignId: DEMO_CAMPAIGN_ID },
  { id: "m10", name: "Amulet of Health", description: "Constitution score becomes 19", rarity: Rarity.Rare, category: ItemCategory.WondrousItem, requiresAttunement: true, attunementRequirement: null, source: "DMG p.150", campaignId: DEMO_CAMPAIGN_ID },
];

const demoWishlists: Record<string, WishlistItemDto[]> = {
  "Thorin Ironforge": [
    { id: "w1", priority: 1, notes: "Need better weapon!", characterId: "c1", characterName: "Thorin Ironforge", magicItemId: "m1", magicItemName: "Flame Tongue Longsword", magicItemRarity: Rarity.Rare },
    { id: "w2", priority: 2, notes: null, characterId: "c1", characterName: "Thorin Ironforge", magicItemId: "m5", magicItemName: "Ring of Protection", magicItemRarity: Rarity.Rare },
  ],
  "Lyra Moonwhisper": [
    { id: "w3", priority: 1, notes: "For the dragon fight", characterId: "c2", characterName: "Lyra Moonwhisper", magicItemId: "m9", magicItemName: "Wand of Fireballs", magicItemRarity: Rarity.Rare },
    { id: "w4", priority: 2, notes: null, characterId: "c2", characterName: "Lyra Moonwhisper", magicItemId: "m2", magicItemName: "Cloak of Displacement", magicItemRarity: Rarity.Rare },
  ],
  "Bramble Thornwick": [
    { id: "w5", priority: 1, notes: "Perfect thematically", characterId: "c3", characterName: "Bramble Thornwick", magicItemId: "m3", magicItemName: "Staff of the Woodlands", magicItemRarity: Rarity.Rare },
    { id: "w6", priority: 2, notes: "Party utility", characterId: "c3", characterName: "Bramble Thornwick", magicItemId: "m4", magicItemName: "Bag of Holding", magicItemRarity: Rarity.Uncommon },
  ],
  "Kael Shadowmend": [
    { id: "w7", priority: 1, notes: "Need the speed", characterId: "c4", characterName: "Kael Shadowmend", magicItemId: "m7", magicItemName: "Boots of Speed", magicItemRarity: Rarity.Rare },
    { id: "w8", priority: 2, notes: null, characterId: "c4", characterName: "Kael Shadowmend", magicItemId: "m10", magicItemName: "Amulet of Health", magicItemRarity: Rarity.Rare },
  ],
};

const demoTreasureTable: TreasureTableDto = {
  id: "tt1",
  name: "Dragon's Hoard — Session 12",
  description: "Treasure from the young red dragon encounter",
  campaignId: DEMO_CAMPAIGN_ID,
  entries: [
    { id: "e1", weight: 3, minRoll: 1, maxRoll: 30, magicItemId: "m6", magicItemName: "Potion of Greater Healing", magicItemRarity: Rarity.Uncommon, magicItemCategory: ItemCategory.Potion },
    { id: "e2", weight: 2, minRoll: 31, maxRoll: 50, magicItemId: "m4", magicItemName: "Bag of Holding", magicItemRarity: Rarity.Uncommon, magicItemCategory: ItemCategory.WondrousItem },
    { id: "e3", weight: 2, minRoll: 51, maxRoll: 70, magicItemId: "m5", magicItemName: "Ring of Protection", magicItemRarity: Rarity.Rare, magicItemCategory: ItemCategory.Ring },
    { id: "e4", weight: 2, minRoll: 71, maxRoll: 90, magicItemId: "m1", magicItemName: "Flame Tongue Longsword", magicItemRarity: Rarity.Rare, magicItemCategory: ItemCategory.Weapon },
    { id: "e5", weight: 1, minRoll: 91, maxRoll: 100, magicItemId: "m8", magicItemName: "Vorpal Sword", magicItemRarity: Rarity.Legendary, magicItemCategory: ItemCategory.Weapon },
  ],
};

// ----- Main Application -----

function AppContent() {
  const [activeTab, setActiveTab] = useState("characters");
  const [isDm] = useState(true); // Toggle for demo; real app checks Entra roles
  const isAuthenticated = useIsAuthenticated();
  const { accounts } = useMsal();

  const currentUserId = accounts[0]?.localAccountId || null;

  // Stateful characters — demo starts with initial data, DDB actions mutate in place
  const [characters, setCharacters] = useState<CharacterDto[]>(demoCharactersInitial);

  // Treasure table roll state
  const [rolledEntry, setRolledEntry] = useState<TreasureTableEntryDto | null>(null);
  const [rolledValue, setRolledValue] = useState<number | null>(null);

  // --- D&D Beyond handlers (demo mode — in production these call dndBeyondApi.*) ---

  const handleDdbLink = async (characterId: string, dndBeyondCharacterId: number) => {
    // In production: await dndBeyondApi.link(DEMO_CAMPAIGN_ID, characterId, dndBeyondCharacterId)
    setCharacters((prev) =>
      prev.map((c) =>
        c.id === characterId
          ? {
              ...c,
              dndBeyondCharacterId,
              dndBeyondUrl: `https://www.dndbeyond.com/characters/${dndBeyondCharacterId}`,
              dndBeyondSyncStatus: DndBeyondSyncStatus.Synced,
              dndBeyondLastSyncedAt: new Date().toISOString(),
              dndBeyondLastSyncError: null,
              // Simulate receiving stats from DDB
              hitPoints: 52, armorClass: 14,
              strength: 14, dexterity: 16, constitution: 14,
              intelligence: 11, wisdom: 16, charisma: 10,
            }
          : c
      )
    );
  };

  const handleDdbSync = async (characterId: string) => {
    // In production: await dndBeyondApi.sync(DEMO_CAMPAIGN_ID, characterId)
    // Simulate a re-sync — toggle between success and failure for demo
    setCharacters((prev) =>
      prev.map((c) => {
        if (c.id !== characterId) return c;
        if (c.dndBeyondSyncStatus === DndBeyondSyncStatus.SyncFailed) {
          return {
            ...c,
            dndBeyondSyncStatus: DndBeyondSyncStatus.Synced,
            dndBeyondLastSyncedAt: new Date().toISOString(),
            dndBeyondLastSyncError: null,
          };
        }
        return { ...c, dndBeyondLastSyncedAt: new Date().toISOString() };
      })
    );
  };

  const handleDdbUploadJson = async (characterId: string, _json: string) => {
    // In production: await dndBeyondApi.uploadJson(DEMO_CAMPAIGN_ID, characterId, json)
    setCharacters((prev) =>
      prev.map((c) =>
        c.id === characterId
          ? {
              ...c,
              dndBeyondSyncStatus: DndBeyondSyncStatus.Synced,
              dndBeyondLastSyncedAt: new Date().toISOString(),
              dndBeyondLastSyncError: null,
            }
          : c
      )
    );
  };

  const handleDdbUnlink = async (characterId: string) => {
    // In production: await dndBeyondApi.unlink(DEMO_CAMPAIGN_ID, characterId)
    setCharacters((prev) =>
      prev.map((c) =>
        c.id === characterId
          ? {
              ...c,
              dndBeyondCharacterId: null,
              dndBeyondUrl: null,
              dndBeyondSyncStatus: DndBeyondSyncStatus.Unlinked,
              dndBeyondLastSyncedAt: null,
              dndBeyondLastSyncError: null,
            }
          : c
      )
    );
  };

  const handlePrivacySave = async (characterId: string, settings: import("./types").CharacterPrivacySettingsDto) => {
    // In production: await privacyApi.update(DEMO_CAMPAIGN_ID, characterId, settings)
    setCharacters((prev) =>
      prev.map((c) =>
        c.id === characterId ? { ...c, privacySettings: settings } : c
      )
    );
  };

  const handleRoll = (tableId: string) => {
    // In production, call treasureTablesApi.roll(campaignId, tableId)
    const table = demoTreasureTable;
    const max = table.entries[table.entries.length - 1]?.maxRoll ?? 100;
    const roll = Math.floor(Math.random() * max) + 1;
    const entry = table.entries.find((e) => roll >= (e.minRoll ?? 1) && roll <= (e.maxRoll ?? max));
    setRolledValue(roll);
    setRolledEntry(entry ?? null);
  };

  const handleGenerateTable = (
    name: string,
    description: string,
    selectedItems: { magicItemId: string; weight: number }[]
  ) => {
    // In production, call treasureTablesApi.generate(campaignId, { name, description, selectedItems })
    console.log("Generate table:", { name, description, selectedItems });
    alert(`Table "${name}" generated with ${selectedItems.length} items! (Demo mode — connect API to persist)`);
  };

  if (!isAuthenticated) {
    return (
      <AppShell isDm={false} activeTab="" onTabChange={() => {}}>
        <div style={{ textAlign: "center", padding: "4rem 2rem" }}>
          <h1 style={{ marginBottom: "1rem" }}>Welcome, Adventurer</h1>
          <p style={{ color: "var(--text-secondary)", fontSize: "1.1rem", marginBottom: "2rem", fontStyle: "italic" }}>
            Sign in with your Microsoft account to access the campaign.
          </p>
        </div>
      </AppShell>
    );
  }

  return (
    <AppShell isDm={isDm} activeTab={activeTab} onTabChange={setActiveTab}>
      {activeTab === "characters" && (
        <>
          <div className="page-header">
            <h1>The Party</h1>
            {isDm && (
              <button
                className="btn btn-sm"
                onClick={async () => {
                  // In production: const results = await dndBeyondApi.syncAll(DEMO_CAMPAIGN_ID)
                  await Promise.all(
                    characters
                      .filter((c) => c.dndBeyondCharacterId != null)
                      .map((c) => handleDdbSync(c.id))
                  );
                }}
              >
                ↻ Sync All from D&D Beyond
              </button>
            )}
          </div>
          <CharacterList
            characters={characters}
            currentUserId={currentUserId}
            isDm={isDm}
            onDdbLink={handleDdbLink}
            onDdbSync={handleDdbSync}
            onDdbUploadJson={handleDdbUploadJson}
            onDdbUnlink={handleDdbUnlink}
            onPrivacySave={handlePrivacySave}
          />
        </>
      )}

      {activeTab === "wishlist" && (
        <>
          <div className="page-header">
            <h1>Magic Item Wishlists</h1>
          </div>
          <WishlistView
            wishlists={demoWishlists}
            currentUserId={currentUserId}
            isDm={isDm}
          />
        </>
      )}

      {activeTab === "magic-items" && isDm && (
        <>
          <div className="page-header">
            <h1>Magic Items Collection</h1>
            <button className="btn btn-primary">+ Add Item</button>
          </div>
          <MagicItemsTable
            items={demoItems}
            isDm={isDm}
          />
        </>
      )}

      {activeTab === "treasure-tables" && isDm && (
        <>
          <div className="page-header">
            <h1>Treasure Tables</h1>
          </div>

          <div style={{ display: "flex", flexDirection: "column", gap: "2rem" }}>
            <TreasureTableView
              table={demoTreasureTable}
              onRoll={handleRoll}
              onDelete={() => alert("Delete in demo mode")}
              rolledEntry={rolledEntry}
              rolledValue={rolledValue}
            />

            <div style={{ borderTop: "1px solid var(--border)", paddingTop: "2rem" }}>
              <h2 style={{ marginBottom: "1rem" }}>Create New Table</h2>
              <TreasureTableGenerator
                items={demoItems}
                onGenerate={handleGenerateTable}
              />
            </div>
          </div>
        </>
      )}
    </AppShell>
  );
}

export default function App() {
  return (
    <MsalProvider instance={msalInstance}>
      <CampaignProvider>
        <AppContent />
      </CampaignProvider>
    </MsalProvider>
  );
}
