using DndCampaignManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DndCampaignManager.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Campaign> Campaigns { get; }
    DbSet<Character> Characters { get; }
    DbSet<CharacterPrivacySettings> CharacterPrivacySettings { get; }
    DbSet<DmItemPool> DmItemPools { get; }
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<MagicItem> MagicItems { get; }
    DbSet<WishlistItem> WishlistItems { get; }
    DbSet<TreasureTable> TreasureTables { get; }
    DbSet<TreasureTableEntry> TreasureTableEntries { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
