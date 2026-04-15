using DndCampaignManager.Application.Common.Interfaces;
using DndCampaignManager.Application.Common.Models;
using DndCampaignManager.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DndCampaignManager.Application.Features.DndBeyondSync;

// =============================================================================
// LINK — Associate a DDB character ID with a local character, then immediately sync
// =============================================================================

public record LinkDndBeyondCommand(
    Guid CharacterId,
    long DndBeyondCharacterId
) : IRequest<DndBeyondSyncResultDto>;

public class LinkDndBeyondCommandValidator : AbstractValidator<LinkDndBeyondCommand>
{
    public LinkDndBeyondCommandValidator()
    {
        RuleFor(x => x.CharacterId).NotEmpty();
        RuleFor(x => x.DndBeyondCharacterId).GreaterThan(0)
            .WithMessage("Enter the numeric character ID from your D&D Beyond URL");
    }
}

public class LinkDndBeyondCommandHandler : IRequestHandler<LinkDndBeyondCommand, DndBeyondSyncResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDndBeyondImportService _importService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<LinkDndBeyondCommandHandler> _logger;

    public LinkDndBeyondCommandHandler(
        IApplicationDbContext db,
        IDndBeyondImportService importService,
        ICurrentUserService currentUser,
        ILogger<LinkDndBeyondCommandHandler> logger)
    {
        _db = db;
        _importService = importService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<DndBeyondSyncResultDto> Handle(LinkDndBeyondCommand request, CancellationToken ct)
    {
        var character = await _db.Characters.FindAsync(new object[] { request.CharacterId }, ct)
            ?? throw new KeyNotFoundException($"Character {request.CharacterId} not found");

        // Only the owning player (or DM) can link
        if (character.PlayerUserId != _currentUser.UserId && !_currentUser.IsDm(character.CampaignId))
            throw new UnauthorizedAccessException("You can only link your own character to D&D Beyond");

        // Check no other character in the campaign is already linked to this DDB ID
        var duplicate = await _db.Characters.AnyAsync(
            c => c.CampaignId == character.CampaignId
                 && c.DndBeyondCharacterId == request.DndBeyondCharacterId
                 && c.Id != request.CharacterId, ct);

        if (duplicate)
            throw new InvalidOperationException(
                "Another character in this campaign is already linked to that D&D Beyond character");

        // Store the link
        character.DndBeyondCharacterId = request.DndBeyondCharacterId;
        character.DndBeyondSyncStatus = DndBeyondSyncStatus.Syncing;
        await _db.SaveChangesAsync(ct);

        // Immediately attempt a sync
        return await SyncCharacterFromDdb(character, ct);
    }

    private async Task<DndBeyondSyncResultDto> SyncCharacterFromDdb(
        Domain.Entities.Character character, CancellationToken ct)
    {
        var result = await _importService.FetchByCharacterIdAsync(character.DndBeyondCharacterId!.Value, ct);

        if (result.Success && result.Data is not null)
        {
            ApplyDdbData(character, result.Data);
            SyncInventory(character, result.Data, _db);
            character.DndBeyondRawJson = result.RawJson;
            character.DndBeyondSyncStatus = DndBeyondSyncStatus.Synced;
            character.DndBeyondLastSyncedAt = DateTime.UtcNow;
            character.DndBeyondLastSyncError = null;
            character.DndBeyondUrl = result.Data.DndBeyondUrl;
            character.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "DDB sync succeeded for character {CharId} (DDB #{DdbId})",
                character.Id, character.DndBeyondCharacterId);

            return new DndBeyondSyncResultDto(true, null, character.DndBeyondLastSyncedAt);
        }
        else
        {
            character.DndBeyondSyncStatus = DndBeyondSyncStatus.SyncFailed;
            character.DndBeyondLastSyncError = result.ErrorMessage ?? "Unknown error";
            character.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            _logger.LogWarning(
                "DDB sync failed for character {CharId} (DDB #{DdbId}): {Error}",
                character.Id, character.DndBeyondCharacterId, result.ErrorMessage);

            return new DndBeyondSyncResultDto(false, result.ErrorMessage, character.DndBeyondLastSyncedAt);
        }
    }

    internal static void ApplyDdbData(Domain.Entities.Character character, DndBeyondCharacterData data)
    {
        character.Name = data.Name;
        character.Level = data.Level;
        character.Race = data.Race;
        character.ImageUrl = data.AvatarUrl ?? character.ImageUrl;
        character.HitPoints = data.HitPoints;
        character.ArmorClass = data.ArmorClass;
        character.Strength = data.Strength;
        character.Dexterity = data.Dexterity;
        character.Constitution = data.Constitution;
        character.Intelligence = data.Intelligence;
        character.Wisdom = data.Wisdom;
        character.Charisma = data.Charisma;

        // Traits — only overwrite if DDB provided them (they may be private/missing)
        if (data.PersonalityTraits is not null) character.PersonalityTraits = data.PersonalityTraits;
        if (data.Ideals is not null) character.Ideals = data.Ideals;
        if (data.Bonds is not null) character.Bonds = data.Bonds;
        if (data.Flaws is not null) character.Flaws = data.Flaws;

        // Map DDB class name to our enum (best-effort)
        if (data.ClassName is not null)
        {
            character.Class = ParseClassBestEffort(data.ClassName);
        }
    }

    /// <summary>
    /// Sync inventory from DDB data. Replaces DDB-sourced items entirely;
    /// manually-added items (DndBeyondItemId == null) are preserved.
    /// </summary>
    internal static void SyncInventory(
        Domain.Entities.Character character,
        DndBeyondCharacterData data,
        IApplicationDbContext db)
    {
        // Remove old DDB-sourced inventory items
        var ddbItems = character.Inventory
            .Where(i => i.DndBeyondItemId != null)
            .ToList();
        foreach (var old in ddbItems)
        {
            db.InventoryItems.Remove(old);
            character.Inventory.Remove(old);
        }

        // Add fresh inventory from DDB
        foreach (var item in data.Inventory)
        {
            character.Inventory.Add(new Domain.Entities.InventoryItem
            {
                Name = item.Name,
                Description = item.Description,
                Quantity = item.Quantity,
                Weight = item.Weight,
                IsEquipped = item.IsEquipped,
                IsAttuned = item.IsAttuned,
                IsMagic = item.IsMagic,
                Rarity = item.Rarity,
                ItemType = item.ItemType,
                DndBeyondItemId = item.DndBeyondItemId,
                CharacterId = character.Id
            });
        }
    }

    internal static CharacterClass ParseClassBestEffort(string className)
    {
        // DDB class names can be multiclass like "Fighter / Wizard" — take the first
        var primary = className.Split('/')[0].Trim().Replace(" ", "");

        if (Enum.TryParse<CharacterClass>(primary, ignoreCase: true, out var parsed))
            return parsed;

        // Handle edge cases
        return primary.ToLowerInvariant() switch
        {
            "blood hunter" or "bloodhunter" => CharacterClass.BloodHunter,
            _ => CharacterClass.Fighter // Safe fallback
        };
    }
}

// =============================================================================
// SYNC — Re-fetch data for an already-linked character
// =============================================================================

public record SyncDndBeyondCommand(Guid CharacterId) : IRequest<DndBeyondSyncResultDto>;

public class SyncDndBeyondCommandHandler : IRequestHandler<SyncDndBeyondCommand, DndBeyondSyncResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDndBeyondImportService _importService;
    private readonly ILogger<SyncDndBeyondCommandHandler> _logger;

    public SyncDndBeyondCommandHandler(
        IApplicationDbContext db,
        IDndBeyondImportService importService,
        ILogger<SyncDndBeyondCommandHandler> logger)
    {
        _db = db;
        _importService = importService;
        _logger = logger;
    }

    public async Task<DndBeyondSyncResultDto> Handle(SyncDndBeyondCommand request, CancellationToken ct)
    {
        var character = await _db.Characters
            .Include(c => c.Inventory)
            .FirstOrDefaultAsync(c => c.Id == request.CharacterId, ct)
            ?? throw new KeyNotFoundException($"Character {request.CharacterId} not found");

        if (character.DndBeyondCharacterId is null)
            throw new InvalidOperationException("This character is not linked to D&D Beyond");

        character.DndBeyondSyncStatus = DndBeyondSyncStatus.Syncing;
        await _db.SaveChangesAsync(ct);

        var result = await _importService.FetchByCharacterIdAsync(character.DndBeyondCharacterId.Value, ct);

        if (result.Success && result.Data is not null)
        {
            LinkDndBeyondCommandHandler.ApplyDdbData(character, result.Data);
            LinkDndBeyondCommandHandler.SyncInventory(character, result.Data, _db);
            character.DndBeyondRawJson = result.RawJson;
            character.DndBeyondSyncStatus = DndBeyondSyncStatus.Synced;
            character.DndBeyondLastSyncedAt = DateTime.UtcNow;
            character.DndBeyondLastSyncError = null;
            character.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return new DndBeyondSyncResultDto(true, null, character.DndBeyondLastSyncedAt);
        }
        else
        {
            // Keep all existing data — just mark as stale
            character.DndBeyondSyncStatus = DndBeyondSyncStatus.SyncFailed;
            character.DndBeyondLastSyncError = result.ErrorMessage ?? "D&D Beyond endpoint unreachable";
            character.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            _logger.LogWarning(
                "DDB re-sync failed for character {CharId}: {Error}. Retaining cached data from {LastSync}.",
                character.Id, result.ErrorMessage, character.DndBeyondLastSyncedAt);

            return new DndBeyondSyncResultDto(false, result.ErrorMessage, character.DndBeyondLastSyncedAt);
        }
    }
}

// =============================================================================
// UPLOAD JSON — Fallback when the API endpoint is down
// =============================================================================

public record UploadDndBeyondJsonCommand(
    Guid CharacterId,
    string RawJson
) : IRequest<DndBeyondSyncResultDto>;

public class UploadDndBeyondJsonCommandValidator : AbstractValidator<UploadDndBeyondJsonCommand>
{
    public UploadDndBeyondJsonCommandValidator()
    {
        RuleFor(x => x.CharacterId).NotEmpty();
        RuleFor(x => x.RawJson).NotEmpty()
            .WithMessage("Paste your D&D Beyond character JSON");
    }
}

public class UploadDndBeyondJsonCommandHandler : IRequestHandler<UploadDndBeyondJsonCommand, DndBeyondSyncResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDndBeyondImportService _importService;
    private readonly ICurrentUserService _currentUser;

    public UploadDndBeyondJsonCommandHandler(
        IApplicationDbContext db,
        IDndBeyondImportService importService,
        ICurrentUserService currentUser)
    {
        _db = db;
        _importService = importService;
        _currentUser = currentUser;
    }

    public async Task<DndBeyondSyncResultDto> Handle(UploadDndBeyondJsonCommand request, CancellationToken ct)
    {
        var character = await _db.Characters
            .Include(c => c.Inventory)
            .FirstOrDefaultAsync(c => c.Id == request.CharacterId, ct)
            ?? throw new KeyNotFoundException($"Character {request.CharacterId} not found");

        if (character.PlayerUserId != _currentUser.UserId && !_currentUser.IsDm(character.CampaignId))
            throw new UnauthorizedAccessException("You can only update your own character");

        var result = _importService.ParseFromJson(request.RawJson);

        if (!result.Success || result.Data is null)
            return new DndBeyondSyncResultDto(false, result.ErrorMessage ?? "Invalid JSON format", null);

        LinkDndBeyondCommandHandler.ApplyDdbData(character, result.Data);
        LinkDndBeyondCommandHandler.SyncInventory(character, result.Data, _db);
        character.DndBeyondCharacterId = result.Data.DndBeyondCharacterId;
        character.DndBeyondRawJson = request.RawJson;
        character.DndBeyondSyncStatus = DndBeyondSyncStatus.Synced;
        character.DndBeyondLastSyncedAt = DateTime.UtcNow;
        character.DndBeyondLastSyncError = null;
        character.DndBeyondUrl = result.Data.DndBeyondUrl;
        character.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new DndBeyondSyncResultDto(true, null, character.DndBeyondLastSyncedAt);
    }
}

// =============================================================================
// UNLINK — Remove DDB association, keep local character data
// =============================================================================

public record UnlinkDndBeyondCommand(Guid CharacterId) : IRequest;

public class UnlinkDndBeyondCommandHandler : IRequestHandler<UnlinkDndBeyondCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UnlinkDndBeyondCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UnlinkDndBeyondCommand request, CancellationToken ct)
    {
        var character = await _db.Characters.FindAsync(new object[] { request.CharacterId }, ct)
            ?? throw new KeyNotFoundException($"Character {request.CharacterId} not found");

        if (character.PlayerUserId != _currentUser.UserId && !_currentUser.IsDm(character.CampaignId))
            throw new UnauthorizedAccessException("You can only unlink your own character");

        // Clear DDB fields but keep all the synced stats — they're still valid local data
        character.DndBeyondCharacterId = null;
        character.DndBeyondUrl = null;
        character.DndBeyondSyncStatus = DndBeyondSyncStatus.Unlinked;
        character.DndBeyondLastSyncedAt = null;
        character.DndBeyondLastSyncError = null;
        character.DndBeyondRawJson = null;
        character.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}

// =============================================================================
// SYNC ALL — DM batch-syncs every linked character in the campaign
// =============================================================================

public record SyncAllDndBeyondCommand(Guid CampaignId) : IRequest<List<DndBeyondBatchSyncResultDto>>;

public class SyncAllDndBeyondCommandHandler
    : IRequestHandler<SyncAllDndBeyondCommand, List<DndBeyondBatchSyncResultDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDndBeyondImportService _importService;
    private readonly ILogger<SyncAllDndBeyondCommandHandler> _logger;

    public SyncAllDndBeyondCommandHandler(
        IApplicationDbContext db,
        IDndBeyondImportService importService,
        ILogger<SyncAllDndBeyondCommandHandler> logger)
    {
        _db = db;
        _importService = importService;
        _logger = logger;
    }

    public async Task<List<DndBeyondBatchSyncResultDto>> Handle(
        SyncAllDndBeyondCommand request, CancellationToken ct)
    {
        var linkedCharacters = await _db.Characters
            .Include(c => c.Inventory)
            .Where(c => c.CampaignId == request.CampaignId && c.DndBeyondCharacterId != null)
            .ToListAsync(ct);

        var results = new List<DndBeyondBatchSyncResultDto>();

        foreach (var character in linkedCharacters)
        {
            var result = await _importService.FetchByCharacterIdAsync(character.DndBeyondCharacterId!.Value, ct);

            if (result.Success && result.Data is not null)
            {
                LinkDndBeyondCommandHandler.ApplyDdbData(character, result.Data);
                LinkDndBeyondCommandHandler.SyncInventory(character, result.Data, _db);
                character.DndBeyondRawJson = result.RawJson;
                character.DndBeyondSyncStatus = DndBeyondSyncStatus.Synced;
                character.DndBeyondLastSyncedAt = DateTime.UtcNow;
                character.DndBeyondLastSyncError = null;

                results.Add(new DndBeyondBatchSyncResultDto(character.Id, character.Name, true, null));
            }
            else
            {
                character.DndBeyondSyncStatus = DndBeyondSyncStatus.SyncFailed;
                character.DndBeyondLastSyncError = result.ErrorMessage;

                results.Add(new DndBeyondBatchSyncResultDto(
                    character.Id, character.Name, false, result.ErrorMessage));
            }

            character.UpdatedAt = DateTime.UtcNow;

            // Brief delay between requests to avoid DDB rate limiting
            await Task.Delay(500, ct);
        }

        await _db.SaveChangesAsync(ct);

        var succeeded = results.Count(r => r.Success);
        _logger.LogInformation(
            "DDB batch sync for campaign {CampaignId}: {Ok}/{Total} succeeded",
            request.CampaignId, succeeded, results.Count);

        return results;
    }
}

// =============================================================================
// DTOs
// =============================================================================

public record DndBeyondSyncResultDto(
    bool Success,
    string? ErrorMessage,
    DateTime? LastSyncedAt
);

public record DndBeyondBatchSyncResultDto(
    Guid CharacterId,
    string CharacterName,
    bool Success,
    string? ErrorMessage
);
