using DndCampaignManager.Application.Common.Interfaces;
using DndCampaignManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DndCampaignManager.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<CharacterPrivacySettings> CharacterPrivacySettings => Set<CharacterPrivacySettings>();
    public DbSet<DmItemPool> DmItemPools => Set<DmItemPool>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<MagicItem> MagicItems => Set<MagicItem>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<TreasureTable> TreasureTables => Set<TreasureTable>();
    public DbSet<TreasureTableEntry> TreasureTableEntries => Set<TreasureTableEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.DmUserId).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.DmUserId);
        });

        modelBuilder.Entity<Character>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PlayerUserId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PlayerDisplayName).HasMaxLength(200);
            entity.Property(e => e.Race).HasMaxLength(50);
            entity.HasIndex(e => e.PlayerUserId);

            // D&D Beyond integration
            entity.Property(e => e.DndBeyondUrl).HasMaxLength(500);
            entity.Property(e => e.DndBeyondLastSyncError).HasMaxLength(1000);
            entity.HasIndex(e => new { e.CampaignId, e.DndBeyondCharacterId })
                .IsUnique()
                .HasFilter("[DndBeyondCharacterId] IS NOT NULL");

            // Raw JSON can be large — store as nvarchar(max)
            entity.Property(e => e.DndBeyondRawJson).HasColumnType("nvarchar(max)");

            // Roleplay fields
            entity.Property(e => e.PersonalityTraits).HasMaxLength(2000);
            entity.Property(e => e.Ideals).HasMaxLength(1000);
            entity.Property(e => e.Bonds).HasMaxLength(1000);
            entity.Property(e => e.Flaws).HasMaxLength(1000);
            entity.Property(e => e.Backstory).HasColumnType("nvarchar(max)");

            entity.HasOne(e => e.Campaign)
                .WithMany(c => c.Characters)
                .HasForeignKey(e => e.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.PrivacySettings)
                .WithOne(p => p.Character)
                .HasForeignKey<CharacterPrivacySettings>(p => p.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CharacterPrivacySettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CharacterId).IsUnique();
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Rarity).HasMaxLength(50);
            entity.Property(e => e.ItemType).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.Character)
                .WithMany(c => c.Inventory)
                .HasForeignKey(e => e.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MagicItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Source).HasMaxLength(100);
            entity.Property(e => e.AttunementRequirement).HasMaxLength(200);

            entity.HasOne(e => e.Campaign)
                .WithMany(c => c.MagicItems)
                .HasForeignKey(e => e.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WishlistItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomItemName).HasMaxLength(200);
            entity.Property(e => e.CustomItemRarity).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            // Unique: same magic item can't appear twice on the same character's wishlist
            // (custom items aren't constrained this way since MagicItemId is null)
            entity.HasIndex(e => new { e.CharacterId, e.MagicItemId })
                .IsUnique()
                .HasFilter("[CharacterId] IS NOT NULL AND [MagicItemId] IS NOT NULL");

            entity.HasOne(e => e.Character)
                .WithMany(c => c.Wishlist)
                .HasForeignKey(e => e.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.MagicItem)
                .WithMany(m => m.WishlistEntries)
                .HasForeignKey(e => e.MagicItemId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.DmPool)
                .WithMany(p => p.Items)
                .HasForeignKey(e => e.DmPoolId)
                .OnDelete(DeleteBehavior.Cascade);

            // Computed column excluded from mapping
            entity.Ignore(e => e.DisplayName);
            entity.Ignore(e => e.IsCustom);
        });

        modelBuilder.Entity<DmItemPool>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CampaignId).IsUnique(); // One pool per campaign

            entity.HasOne(e => e.Campaign)
                .WithOne(c => c.DmItemPool)
                .HasForeignKey<DmItemPool>(e => e.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TreasureTable>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();

            entity.HasOne(e => e.Campaign)
                .WithMany(c => c.TreasureTables)
                .HasForeignKey(e => e.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TreasureTableEntry>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.TreasureTable)
                .WithMany(t => t.Entries)
                .HasForeignKey(e => e.TreasureTableId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.MagicItem)
                .WithMany(m => m.TreasureTableEntries)
                .HasForeignKey(e => e.MagicItemId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade conflict
        });
    }
}
