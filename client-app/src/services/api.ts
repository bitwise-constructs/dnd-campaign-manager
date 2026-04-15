import { PublicClientApplication } from "@azure/msal-browser";
import { apiScopes } from "./authConfig";

const API_BASE = import.meta.env.VITE_API_BASE_URL || "https://localhost:7001/api";

let msalInstance: PublicClientApplication | null = null;

export function setMsalInstance(instance: PublicClientApplication) {
  msalInstance = instance;
}

async function getToken(): Promise<string> {
  if (!msalInstance) throw new Error("MSAL not initialized");

  const accounts = msalInstance.getAllAccounts();
  if (accounts.length === 0) throw new Error("No authenticated user");

  try {
    const response = await msalInstance.acquireTokenSilent({
      scopes: apiScopes,
      account: accounts[0],
    });
    return response.accessToken;
  } catch {
    const response = await msalInstance.acquireTokenPopup({
      scopes: apiScopes,
    });
    return response.accessToken;
  }
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = await getToken();

  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
      ...options.headers,
    },
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: response.statusText }));
    throw new ApiError(response.status, error);
  }

  if (response.status === 204) return undefined as T;
  return response.json();
}

export class ApiError extends Error {
  constructor(public status: number, public body: unknown) {
    super(`API error ${status}`);
  }
}

// --- Characters ---
export const charactersApi = {
  getAll: (campaignId: string) =>
    request<import("../types").CharacterDto[]>(`/campaigns/${campaignId}/characters`),

  get: (campaignId: string, id: string) =>
    request<import("../types").CharacterDto>(`/campaigns/${campaignId}/characters/${id}`),

  create: (campaignId: string, data: Omit<import("../types").CharacterDto, "id" | "campaignId" | "playerUserId" | "playerDisplayName">) =>
    request<{ id: string }>(`/campaigns/${campaignId}/characters`, {
      method: "POST",
      body: JSON.stringify(data),
    }),

  update: (campaignId: string, id: string, data: Partial<import("../types").CharacterDto>) =>
    request<void>(`/campaigns/${campaignId}/characters/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    }),

  delete: (campaignId: string, id: string) =>
    request<void>(`/campaigns/${campaignId}/characters/${id}`, { method: "DELETE" }),
};

// --- Magic Items ---
export const magicItemsApi = {
  getAll: (campaignId: string, rarity?: number, category?: number) => {
    const params = new URLSearchParams();
    if (rarity !== undefined) params.set("rarity", String(rarity));
    if (category !== undefined) params.set("category", String(category));
    const qs = params.toString();
    return request<import("../types").MagicItemDto[]>(
      `/campaigns/${campaignId}/magicitems${qs ? `?${qs}` : ""}`
    );
  },

  create: (campaignId: string, data: Omit<import("../types").MagicItemDto, "id" | "campaignId">) =>
    request<{ id: string }>(`/campaigns/${campaignId}/magicitems`, {
      method: "POST",
      body: JSON.stringify(data),
    }),

  update: (campaignId: string, id: string, data: Partial<import("../types").MagicItemDto>) =>
    request<void>(`/campaigns/${campaignId}/magicitems/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    }),

  delete: (campaignId: string, id: string) =>
    request<void>(`/campaigns/${campaignId}/magicitems/${id}`, { method: "DELETE" }),
};

// --- Wishlists ---
export const wishlistsApi = {
  getCampaignWishlists: (campaignId: string) =>
    request<import("../types").CampaignWishlistsDto>(
      `/campaigns/${campaignId}/wishlists`
    ),

  getCharacterWishlist: (campaignId: string, characterId: string) =>
    request<import("../types").WishlistItemDto[]>(
      `/campaigns/${campaignId}/wishlists/character/${characterId}`
    ),

  addToCharacterWishlist: (campaignId: string, data: {
    characterId: string;
    magicItemId?: string;
    customItemName?: string;
    customItemRarity?: string;
    priority: number;
    notes?: string;
  }) =>
    request<{ id: string }>(`/campaigns/${campaignId}/wishlists/character`, {
      method: "POST",
      body: JSON.stringify(data),
    }),

  updatePriority: (campaignId: string, id: string, priority: number) =>
    request<void>(`/campaigns/${campaignId}/wishlists/${id}/priority`, {
      method: "PUT",
      body: JSON.stringify(priority),
    }),

  remove: (campaignId: string, id: string) =>
    request<void>(`/campaigns/${campaignId}/wishlists/${id}`, { method: "DELETE" }),
};

// --- DM Item Pool ---
export const dmPoolApi = {
  get: (campaignId: string) =>
    request<import("../types").DmItemPoolDto>(`/campaigns/${campaignId}/dm-pool`),

  add: (campaignId: string, data: {
    magicItemId?: string;
    customItemName?: string;
    customItemRarity?: string;
    notes?: string;
    weight: number;
  }) =>
    request<{ id: string }>(`/campaigns/${campaignId}/dm-pool`, {
      method: "POST",
      body: JSON.stringify({ campaignId, ...data }),
    }),

  updateWeight: (campaignId: string, id: string, weight: number) =>
    request<void>(`/campaigns/${campaignId}/dm-pool/${id}/weight`, {
      method: "PUT",
      body: JSON.stringify(weight),
    }),

  remove: (campaignId: string, id: string) =>
    request<void>(`/campaigns/${campaignId}/dm-pool/${id}`, { method: "DELETE" }),

  pickTopN: (campaignId: string, selectedItemIds: string[], count: number, useWeights: boolean) =>
    request<import("../types").PickResultDto>(`/campaigns/${campaignId}/dm-pool/pick`, {
      method: "POST",
      body: JSON.stringify({ selectedItemIds, count, useWeights }),
    }),
};

// --- Magic Item Search ---
export const magicItemSearchApi = {
  search: (campaignId: string, query: string, maxResults = 15) =>
    request<import("../types").MagicItemSearchResult[]>(
      `/campaigns/${campaignId}/magic-item-search?q=${encodeURIComponent(query)}&maxResults=${maxResults}`
    ),
};

// --- Treasure Tables ---
export const treasureTablesApi = {
  getAll: (campaignId: string) =>
    request<import("../types").TreasureTableDto[]>(`/campaigns/${campaignId}/treasuretables`),

  get: (campaignId: string, id: string) =>
    request<import("../types").TreasureTableDto>(`/campaigns/${campaignId}/treasuretables/${id}`),

  generate: (campaignId: string, data: {
    name: string;
    description?: string;
    selectedItems: import("../types").SelectedItemForTable[];
  }) =>
    request<{ id: string }>(`/campaigns/${campaignId}/treasuretables/generate`, {
      method: "POST",
      body: JSON.stringify(data),
    }),

  roll: (campaignId: string, id: string, forcedRoll?: number) => {
    const qs = forcedRoll !== undefined ? `?forcedRoll=${forcedRoll}` : "";
    return request<import("../types").TreasureTableEntryDto>(
      `/campaigns/${campaignId}/treasuretables/${id}/roll${qs}`,
      { method: "POST" }
    );
  },

  delete: (campaignId: string, id: string) =>
    request<void>(`/campaigns/${campaignId}/treasuretables/${id}`, { method: "DELETE" }),
};

// --- D&D Beyond Integration ---
export const dndBeyondApi = {
  link: (campaignId: string, characterId: string, dndBeyondCharacterId: number) =>
    request<import("../types").DndBeyondSyncResult>(
      `/campaigns/${campaignId}/characters/${characterId}/dndbeyond/link`,
      { method: "POST", body: JSON.stringify({ dndBeyondCharacterId }) }
    ),

  sync: (campaignId: string, characterId: string) =>
    request<import("../types").DndBeyondSyncResult>(
      `/campaigns/${campaignId}/characters/${characterId}/dndbeyond/sync`,
      { method: "POST" }
    ),

  uploadJson: (campaignId: string, characterId: string, rawJson: string) =>
    request<import("../types").DndBeyondSyncResult>(
      `/campaigns/${campaignId}/characters/${characterId}/dndbeyond/upload-json`,
      { method: "POST", body: JSON.stringify({ rawJson }) }
    ),

  unlink: (campaignId: string, characterId: string) =>
    request<void>(
      `/campaigns/${campaignId}/characters/${characterId}/dndbeyond/link`,
      { method: "DELETE" }
    ),

  syncAll: (campaignId: string) =>
    request<import("../types").DndBeyondBatchSyncResult[]>(
      `/campaigns/${campaignId}/dndbeyond/sync-all`,
      { method: "POST" }
    ),
};

// --- Privacy Settings ---
export const privacyApi = {
  get: (campaignId: string, characterId: string) =>
    request<import("../types").CharacterPrivacySettingsDto>(
      `/campaigns/${campaignId}/characters/${characterId}/privacy`
    ),

  update: (campaignId: string, characterId: string, settings: import("../types").CharacterPrivacySettingsDto) =>
    request<import("../types").CharacterPrivacySettingsDto>(
      `/campaigns/${campaignId}/characters/${characterId}/privacy`,
      { method: "PUT", body: JSON.stringify({ characterId, ...settings }) }
    ),
};
