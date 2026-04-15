using DndCampaignManager.Application.Common.Interfaces;
using DndCampaignManager.Application.Common.Models;
using DndCampaignManager.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DndCampaignManager.Application.Features.Characters.Queries;

// --- Get all characters for a campaign (privacy-aware) ---
public record GetCharactersQuery(Guid CampaignId) : IRequest<List<CharacterDto>>;

public class GetCharactersQueryHandler : IRequestHandler<GetCharactersQuery, List<CharacterDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCharactersQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<CharacterDto>> Handle(GetCharactersQuery request, CancellationToken ct)
    {
        var characters = await _db.Characters
            .Include(c => c.PrivacySettings)
            .Include(c => c.Inventory)
            .Where(c => c.CampaignId == request.CampaignId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        var isDm = _currentUser.IsDm(request.CampaignId);

        return characters.Select(c => ToPrivacyAwareDto(c, _currentUser.UserId, isDm)).ToList();
    }
}

// --- Get single character (privacy-aware) ---
public record GetCharacterQuery(Guid Id) : IRequest<CharacterDto?>;

public class GetCharacterQueryHandler : IRequestHandler<GetCharacterQuery, CharacterDto?>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCharacterQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CharacterDto?> Handle(GetCharacterQuery request, CancellationToken ct)
    {
        var character = await _db.Characters
            .Include(c => c.PrivacySettings)
            .Include(c => c.Inventory)
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

        if (character is null) return null;

        var isDm = _currentUser.IsDm(character.CampaignId);
        return ToPrivacyAwareDto(character, _currentUser.UserId, isDm);
    }
}

// =============================================================================
// Privacy filtering logic — shared by all character queries
// =============================================================================

file static class PrivacyFilter
{
    // Called from both query handlers via the file-scoped method below
}

static file CharacterDto ToPrivacyAwareDto(Character c, string? viewerUserId, bool viewerIsDm)
{
    var isOwner = c.PlayerUserId == viewerUserId;
    var canSeeAll = isOwner || viewerIsDm;
    var p = c.PrivacySettings;
    var shareAll = p?.ShowAll ?? false;

    // Helper: returns the value if the viewer can see it, null otherwise
    T? Gate<T>(T? value, bool fieldVisible) where T : struct
        => canSeeAll || shareAll || fieldVisible ? value : null;

    string? GateStr(string? value, bool fieldVisible)
        => canSeeAll || shareAll || fieldVisible ? value : null;

    var showStats = p?.ShowAbilityScores ?? false;
    var showHp = p?.ShowHitPoints ?? false;
    var showAc = p?.ShowArmorClass ?? false;
    var showInv = p?.ShowInventory ?? false;
    var showTraits = p?.ShowPersonalityTraits ?? false;
    var showIdeals = p?.ShowIdeals ?? false;
    var showBonds = p?.ShowBonds ?? false;
    var showFlaws = p?.ShowFlaws ?? false;
    var showBackstory = p?.ShowBackstory ?? false;

    List<InventoryItemDto>? inventory = null;
    if (canSeeAll || shareAll || showInv)
    {
        inventory = c.Inventory.Select(i => new InventoryItemDto(
            i.Id, i.Name, i.Description, i.Quantity, i.Weight,
            i.IsEquipped, i.IsAttuned, i.IsMagic, i.Rarity, i.ItemType, i.Notes
        )).ToList();
    }

    // Only the owner sees their own privacy settings (so they can edit them)
    CharacterPrivacySettingsDto? privacyDto = null;
    if (isOwner && p is not null)
    {
        privacyDto = new CharacterPrivacySettingsDto(
            p.ShowAbilityScores, p.ShowHitPoints, p.ShowArmorClass,
            p.ShowInventory, p.ShowPersonalityTraits, p.ShowIdeals,
            p.ShowBonds, p.ShowFlaws, p.ShowWishlist, p.ShowBackstory, p.ShowAll
        );
    }

    return new CharacterDto(
        Id: c.Id,
        Name: c.Name,
        PlayerUserId: c.PlayerUserId,
        PlayerDisplayName: c.PlayerDisplayName,
        Class: c.Class,
        Level: c.Level,
        Race: c.Race,
        ImageUrl: c.ImageUrl,
        CampaignId: c.CampaignId,
        IsOwner: isOwner,
        // DDB fields (always visible — they're metadata, not character secrets)
        DndBeyondCharacterId: c.DndBeyondCharacterId,
        DndBeyondUrl: c.DndBeyondUrl,
        DndBeyondSyncStatus: c.DndBeyondSyncStatus,
        DndBeyondLastSyncedAt: c.DndBeyondLastSyncedAt,
        DndBeyondLastSyncError: c.DndBeyondLastSyncError,
        // Privacy-gated fields
        HitPoints: Gate(c.HitPoints, showHp),
        ArmorClass: Gate(c.ArmorClass, showAc),
        Strength: Gate(c.Strength, showStats),
        Dexterity: Gate(c.Dexterity, showStats),
        Constitution: Gate(c.Constitution, showStats),
        Intelligence: Gate(c.Intelligence, showStats),
        Wisdom: Gate(c.Wisdom, showStats),
        Charisma: Gate(c.Charisma, showStats),
        PersonalityTraits: GateStr(c.PersonalityTraits, showTraits),
        Ideals: GateStr(c.Ideals, showIdeals),
        Bonds: GateStr(c.Bonds, showBonds),
        Flaws: GateStr(c.Flaws, showFlaws),
        Backstory: GateStr(c.Backstory, showBackstory),
        Inventory: inventory,
        PrivacySettings: privacyDto
    );
}
