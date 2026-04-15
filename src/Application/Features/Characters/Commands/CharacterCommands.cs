using DndCampaignManager.Application.Common.Interfaces;
using DndCampaignManager.Domain.Entities;
using DndCampaignManager.Domain.Enums;
using FluentValidation;
using MediatR;

namespace DndCampaignManager.Application.Features.Characters.Commands;

// --- Create Character ---
public record CreateCharacterCommand(
    string Name,
    CharacterClass Class,
    int Level,
    string? Race,
    string? ImageUrl,
    Guid CampaignId
) : IRequest<Guid>;

public class CreateCharacterCommandValidator : AbstractValidator<CreateCharacterCommand>
{
    public CreateCharacterCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Level).InclusiveBetween(1, 20);
        RuleFor(x => x.CampaignId).NotEmpty();
    }
}

public class CreateCharacterCommandHandler : IRequestHandler<CreateCharacterCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateCharacterCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateCharacterCommand request, CancellationToken ct)
    {
        var character = new Character
        {
            Name = request.Name,
            PlayerUserId = _currentUser.UserId!,
            PlayerDisplayName = _currentUser.DisplayName,
            Class = request.Class,
            Level = request.Level,
            Race = request.Race,
            ImageUrl = request.ImageUrl,
            CampaignId = request.CampaignId
        };

        _db.Characters.Add(character);
        await _db.SaveChangesAsync(ct);

        return character.Id;
    }
}

// --- Update Character ---
public record UpdateCharacterCommand(
    Guid Id,
    string Name,
    CharacterClass Class,
    int Level,
    string? Race,
    string? ImageUrl
) : IRequest;

public class UpdateCharacterCommandValidator : AbstractValidator<UpdateCharacterCommand>
{
    public UpdateCharacterCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Level).InclusiveBetween(1, 20);
    }
}

public class UpdateCharacterCommandHandler : IRequestHandler<UpdateCharacterCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateCharacterCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateCharacterCommand request, CancellationToken ct)
    {
        var character = await _db.Characters.FindAsync(new object[] { request.Id }, ct)
            ?? throw new KeyNotFoundException($"Character {request.Id} not found");

        // Only the owning player or a DM can update
        if (character.PlayerUserId != _currentUser.UserId)
            throw new UnauthorizedAccessException("You can only edit your own character");

        character.Name = request.Name;
        character.Class = request.Class;
        character.Level = request.Level;
        character.Race = request.Race;
        character.ImageUrl = request.ImageUrl;
        character.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}

// --- Delete Character ---
public record DeleteCharacterCommand(Guid Id) : IRequest;

public class DeleteCharacterCommandHandler : IRequestHandler<DeleteCharacterCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteCharacterCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DeleteCharacterCommand request, CancellationToken ct)
    {
        var character = await _db.Characters.FindAsync(new object[] { request.Id }, ct)
            ?? throw new KeyNotFoundException($"Character {request.Id} not found");

        _db.Characters.Remove(character);
        await _db.SaveChangesAsync(ct);
    }
}
