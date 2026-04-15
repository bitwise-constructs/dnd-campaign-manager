using DndCampaignManager.Application.Common.Interfaces;
using DndCampaignManager.Application.Common.Models;
using DndCampaignManager.Domain.Entities;
using DndCampaignManager.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DndCampaignManager.Application.Features.Wishlists.Queries;

// --- Get wishlist for a specific character ---
public record GetCharacterWishlistQuery(Guid CharacterId) : IRequest<List<WishlistItemDto>>;

public class GetCharacterWishlistQueryHandler : IRequestHandler<GetCharacterWishlistQuery, List<WishlistItemDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCharacterWishlistQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<WishlistItemDto>> Handle(GetCharacterWishlistQuery request, CancellationToken ct)
    {
        return await _db.WishlistItems
            .Include(w => w.Character)
            .Include(w => w.MagicItem)
            .Where(w => w.CharacterId == request.CharacterId)
            .OrderBy(w => w.Priority)
            .Select(w => MapToDto(w))
            .ToListAsync(ct);
    }
}

// --- Get all wishlists for a campaign (DM view) ---
public record GetCampaignWishlistsQuery(Guid CampaignId)
    : IRequest<CampaignWishlistsDto>;

public record CampaignWishlistsDto(
    DmItemPoolDto DmPool,
    Dictionary<string, List<WishlistItemDto>> CharacterWishlists
);

public class GetCampaignWishlistsQueryHandler
    : IRequestHandler<GetCampaignWishlistsQuery, CampaignWishlistsDto>
{
    private readonly IApplicationDbContext _db;

    public GetCampaignWishlistsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CampaignWishlistsDto> Handle(
        GetCampaignWishlistsQuery request, CancellationToken ct)
    {
        // Get or create the DM pool
        var pool = await _db.DmItemPools
            .Include(p => p.Items).ThenInclude(i => i.MagicItem)
            .FirstOrDefaultAsync(p => p.CampaignId == request.CampaignId, ct);

        DmItemPoolDto poolDto;
        if (pool is null)
        {
            // Auto-create an empty pool
            pool = new DmItemPool { CampaignId = request.CampaignId };
            _db.DmItemPools.Add(pool);
            await _db.SaveChangesAsync(ct);
            poolDto = new DmItemPoolDto(pool.Id, request.CampaignId, new List<WishlistItemDto>());
        }
        else
        {
            poolDto = new DmItemPoolDto(
                pool.Id, request.CampaignId,
                pool.Items.OrderBy(i => i.Priority).Select(i => MapToDto(i)).ToList()
            );
        }

        // Get character wishlists
        var charItems = await _db.WishlistItems
            .Include(w => w.Character)
            .Include(w => w.MagicItem)
            .Where(w => w.CharacterId != null && w.Character!.CampaignId == request.CampaignId)
            .OrderBy(w => w.Character!.Name)
            .ThenBy(w => w.Priority)
            .ToListAsync(ct);

        var charWishlists = charItems
            .GroupBy(w => w.Character!.Name)
            .ToDictionary(g => g.Key, g => g.Select(w => MapToDto(w)).ToList());

        return new CampaignWishlistsDto(poolDto, charWishlists);
    }
}

// --- Get the DM item pool ---
public record GetDmItemPoolQuery(Guid CampaignId) : IRequest<DmItemPoolDto>;

public class GetDmItemPoolQueryHandler : IRequestHandler<GetDmItemPoolQuery, DmItemPoolDto>
{
    private readonly IApplicationDbContext _db;

    public GetDmItemPoolQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<DmItemPoolDto> Handle(GetDmItemPoolQuery request, CancellationToken ct)
    {
        var pool = await _db.DmItemPools
            .Include(p => p.Items).ThenInclude(i => i.MagicItem)
            .FirstOrDefaultAsync(p => p.CampaignId == request.CampaignId, ct);

        if (pool is null)
        {
            pool = new DmItemPool { CampaignId = request.CampaignId };
            _db.DmItemPools.Add(pool);
            await _db.SaveChangesAsync(ct);
        }

        return new DmItemPoolDto(
            pool.Id, request.CampaignId,
            pool.Items.OrderBy(i => i.Priority).Select(i => MapToDto(i)).ToList()
        );
    }
}

// =============================================================================
// COMMANDS
// =============================================================================

namespace DndCampaignManager.Application.Features.Wishlists.Commands;

// --- Add item to a character's wishlist (linked or custom) ---
public record AddToWishlistCommand(
    Guid CharacterId,
    Guid? MagicItemId,
    string? CustomItemName,
    string? CustomItemRarity,
    int Priority,
    string? Notes
) : IRequest<Guid>;

public class AddToWishlistCommandValidator : AbstractValidator<AddToWishlistCommand>
{
    public AddToWishlistCommandValidator()
    {
        RuleFor(x => x.CharacterId).NotEmpty();
        RuleFor(x => x.Priority).GreaterThan(0);
        RuleFor(x => x)
            .Must(x => x.MagicItemId.HasValue || !string.IsNullOrWhiteSpace(x.CustomItemName))
            .WithMessage("Either select a magic item or enter a custom item name");
    }
}

public class AddToWishlistCommandHandler : IRequestHandler<AddToWishlistCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddToWishlistCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(AddToWishlistCommand request, CancellationToken ct)
    {
        var character = await _db.Characters.FindAsync(new object[] { request.CharacterId }, ct)
            ?? throw new KeyNotFoundException("Character not found");

        if (character.PlayerUserId != _currentUser.UserId && !_currentUser.IsDm(character.CampaignId))
            throw new UnauthorizedAccessException("You can only modify your own character's wishlist");

        // Duplicate check for linked items
        if (request.MagicItemId.HasValue)
        {
            var exists = await _db.WishlistItems.AnyAsync(
                w => w.CharacterId == request.CharacterId && w.MagicItemId == request.MagicItemId, ct);
            if (exists)
                throw new InvalidOperationException("This item is already on the wishlist");
        }

        var item = new WishlistItem
        {
            CharacterId = request.CharacterId,
            MagicItemId = request.MagicItemId,
            CustomItemName = request.MagicItemId.HasValue ? null : request.CustomItemName,
            CustomItemRarity = request.MagicItemId.HasValue ? null : request.CustomItemRarity,
            Priority = request.Priority,
            Notes = request.Notes,
            Weight = 1
        };

        _db.WishlistItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return item.Id;
    }
}

// --- Add item to the DM's general pool (linked or custom) ---
public record AddToDmPoolCommand(
    Guid CampaignId,
    Guid? MagicItemId,
    string? CustomItemName,
    string? CustomItemRarity,
    string? Notes,
    int Weight
) : IRequest<Guid>;

public class AddToDmPoolCommandValidator : AbstractValidator<AddToDmPoolCommand>
{
    public AddToDmPoolCommandValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.Weight).GreaterThan(0);
        RuleFor(x => x)
            .Must(x => x.MagicItemId.HasValue || !string.IsNullOrWhiteSpace(x.CustomItemName))
            .WithMessage("Either select a magic item or enter a custom item name");
    }
}

public class AddToDmPoolCommandHandler : IRequestHandler<AddToDmPoolCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public AddToDmPoolCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(AddToDmPoolCommand request, CancellationToken ct)
    {
        var pool = await _db.DmItemPools
            .FirstOrDefaultAsync(p => p.CampaignId == request.CampaignId, ct);

        if (pool is null)
        {
            pool = new DmItemPool { CampaignId = request.CampaignId };
            _db.DmItemPools.Add(pool);
            await _db.SaveChangesAsync(ct);
        }

        var nextPriority = pool.Items.Any() ? pool.Items.Max(i => i.Priority) + 1 : 1;

        var item = new WishlistItem
        {
            DmPoolId = pool.Id,
            MagicItemId = request.MagicItemId,
            CustomItemName = request.MagicItemId.HasValue ? null : request.CustomItemName,
            CustomItemRarity = request.MagicItemId.HasValue ? null : request.CustomItemRarity,
            Notes = request.Notes,
            Weight = request.Weight,
            Priority = nextPriority
        };

        _db.WishlistItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return item.Id;
    }
}

// --- Update weight on any wishlist item (DM only) ---
public record UpdateWishlistItemWeightCommand(Guid Id, int Weight) : IRequest;

public class UpdateWishlistItemWeightCommandHandler : IRequestHandler<UpdateWishlistItemWeightCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateWishlistItemWeightCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateWishlistItemWeightCommand request, CancellationToken ct)
    {
        var item = await _db.WishlistItems.FindAsync(new object[] { request.Id }, ct)
            ?? throw new KeyNotFoundException("Wishlist item not found");

        item.Weight = Math.Max(1, request.Weight);
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}

// --- Remove wishlist item ---
public record RemoveWishlistItemCommand(Guid Id) : IRequest;

public class RemoveWishlistItemCommandHandler : IRequestHandler<RemoveWishlistItemCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RemoveWishlistItemCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(RemoveWishlistItemCommand request, CancellationToken ct)
    {
        var item = await _db.WishlistItems
            .Include(w => w.Character)
            .FirstOrDefaultAsync(w => w.Id == request.Id, ct)
            ?? throw new KeyNotFoundException("Wishlist item not found");

        // DM pool items: only DM can remove. Character items: owner or DM.
        if (item.CharacterId.HasValue && item.Character != null)
        {
            if (item.Character.PlayerUserId != _currentUser.UserId
                && !_currentUser.IsDm(item.Character.CampaignId))
                throw new UnauthorizedAccessException("You can only modify your own character's wishlist");
        }

        _db.WishlistItems.Remove(item);
        await _db.SaveChangesAsync(ct);
    }
}

// --- Update priority ---
public record UpdateWishlistPriorityCommand(Guid Id, int Priority) : IRequest;

public class UpdateWishlistPriorityCommandHandler : IRequestHandler<UpdateWishlistPriorityCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateWishlistPriorityCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateWishlistPriorityCommand request, CancellationToken ct)
    {
        var item = await _db.WishlistItems.FindAsync(new object[] { request.Id }, ct)
            ?? throw new KeyNotFoundException("Wishlist item not found");

        item.Priority = request.Priority;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}

// --- Pick top N (weighted random selection from checked items) ---
public record PickTopNCommand(
    List<Guid> SelectedItemIds,
    int Count,
    bool UseWeights
) : IRequest<PickResultDto>;

public class PickTopNCommandHandler : IRequestHandler<PickTopNCommand, PickResultDto>
{
    private readonly IApplicationDbContext _db;
    private static readonly Random _random = new();

    public PickTopNCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<PickResultDto> Handle(PickTopNCommand request, CancellationToken ct)
    {
        var items = await _db.WishlistItems
            .Include(w => w.MagicItem)
            .Include(w => w.Character)
            .Where(w => request.SelectedItemIds.Contains(w.Id))
            .ToListAsync(ct);

        if (!items.Any())
            throw new InvalidOperationException("No items selected for picking");

        var picks = new List<WishlistItemDto>();

        for (int i = 0; i < request.Count; i++)
        {
            WishlistItem picked;

            if (request.UseWeights)
            {
                int totalWeight = items.Sum(it => it.Weight);
                int roll = _random.Next(1, totalWeight + 1);
                int cumulative = 0;

                picked = items.Last(); // fallback
                foreach (var item in items)
                {
                    cumulative += item.Weight;
                    if (roll <= cumulative)
                    {
                        picked = item;
                        break;
                    }
                }
            }
            else
            {
                picked = items[_random.Next(items.Count)];
            }

            picks.Add(MapToDto(picked));
        }

        return new PickResultDto(picks, request.UseWeights);
    }
}

// =============================================================================
// Shared mapping helper
// =============================================================================

file static class WishlistMapping { }

static file WishlistItemDto MapToDto(WishlistItem w) => new(
    Id: w.Id,
    Priority: w.Priority,
    Notes: w.Notes,
    Weight: w.Weight,
    CharacterId: w.CharacterId,
    CharacterName: w.Character?.Name,
    MagicItemId: w.MagicItemId,
    MagicItemName: w.MagicItem?.Name,
    MagicItemRarity: w.MagicItem?.Rarity,
    CustomItemName: w.CustomItemName,
    CustomItemRarity: w.CustomItemRarity,
    IsCustom: w.MagicItemId == null,
    DisplayName: w.MagicItem?.Name ?? w.CustomItemName ?? "Unknown item"
);
