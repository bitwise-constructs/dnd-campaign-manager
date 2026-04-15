using DndCampaignManager.Application.Common.Interfaces;
using DndCampaignManager.Application.Common.Models;
using DndCampaignManager.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DndCampaignManager.Application.Features.Privacy;

public record UpdatePrivacySettingsCommand(
    Guid CharacterId,
    bool ShowAbilityScores,
    bool ShowHitPoints,
    bool ShowArmorClass,
    bool ShowInventory,
    bool ShowPersonalityTraits,
    bool ShowIdeals,
    bool ShowBonds,
    bool ShowFlaws,
    bool ShowWishlist,
    bool ShowBackstory,
    bool ShowAll
) : IRequest<CharacterPrivacySettingsDto>;

public class UpdatePrivacySettingsCommandHandler
    : IRequestHandler<UpdatePrivacySettingsCommand, CharacterPrivacySettingsDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdatePrivacySettingsCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CharacterPrivacySettingsDto> Handle(
        UpdatePrivacySettingsCommand request, CancellationToken ct)
    {
        var character = await _db.Characters
            .Include(c => c.PrivacySettings)
            .FirstOrDefaultAsync(c => c.Id == request.CharacterId, ct)
            ?? throw new KeyNotFoundException($"Character {request.CharacterId} not found");

        // Only the owning player can change their own privacy settings
        if (character.PlayerUserId != _currentUser.UserId)
            throw new UnauthorizedAccessException("You can only change privacy settings for your own character");

        var settings = character.PrivacySettings;

        if (settings is null)
        {
            settings = new CharacterPrivacySettings { CharacterId = character.Id };
            _db.CharacterPrivacySettings.Add(settings);
            character.PrivacySettings = settings;
        }

        settings.ShowAbilityScores = request.ShowAbilityScores;
        settings.ShowHitPoints = request.ShowHitPoints;
        settings.ShowArmorClass = request.ShowArmorClass;
        settings.ShowInventory = request.ShowInventory;
        settings.ShowPersonalityTraits = request.ShowPersonalityTraits;
        settings.ShowIdeals = request.ShowIdeals;
        settings.ShowBonds = request.ShowBonds;
        settings.ShowFlaws = request.ShowFlaws;
        settings.ShowWishlist = request.ShowWishlist;
        settings.ShowBackstory = request.ShowBackstory;
        settings.ShowAll = request.ShowAll;
        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new CharacterPrivacySettingsDto(
            settings.ShowAbilityScores, settings.ShowHitPoints, settings.ShowArmorClass,
            settings.ShowInventory, settings.ShowPersonalityTraits, settings.ShowIdeals,
            settings.ShowBonds, settings.ShowFlaws, settings.ShowWishlist,
            settings.ShowBackstory, settings.ShowAll
        );
    }
}

public record GetPrivacySettingsQuery(Guid CharacterId) : IRequest<CharacterPrivacySettingsDto>;

public class GetPrivacySettingsQueryHandler
    : IRequestHandler<GetPrivacySettingsQuery, CharacterPrivacySettingsDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetPrivacySettingsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CharacterPrivacySettingsDto> Handle(
        GetPrivacySettingsQuery request, CancellationToken ct)
    {
        var character = await _db.Characters
            .Include(c => c.PrivacySettings)
            .FirstOrDefaultAsync(c => c.Id == request.CharacterId, ct)
            ?? throw new KeyNotFoundException($"Character {request.CharacterId} not found");

        if (character.PlayerUserId != _currentUser.UserId)
            throw new UnauthorizedAccessException("You can only view privacy settings for your own character");

        var p = character.PrivacySettings;
        if (p is null)
        {
            // Return defaults (everything private)
            return new CharacterPrivacySettingsDto(
                false, false, false, false, false, false, false, false, false, false, false);
        }

        return new CharacterPrivacySettingsDto(
            p.ShowAbilityScores, p.ShowHitPoints, p.ShowArmorClass,
            p.ShowInventory, p.ShowPersonalityTraits, p.ShowIdeals,
            p.ShowBonds, p.ShowFlaws, p.ShowWishlist, p.ShowBackstory, p.ShowAll
        );
    }
}
