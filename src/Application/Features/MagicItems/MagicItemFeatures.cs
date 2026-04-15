using AutoMapper;
using AutoMapper.QueryableExtensions;
using DndCampaignManager.Application.Common.Interfaces;
using DndCampaignManager.Application.Common.Models;
using DndCampaignManager.Domain.Entities;
using DndCampaignManager.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DndCampaignManager.Application.Features.MagicItems.Queries;

public record GetMagicItemsQuery(Guid CampaignId, Rarity? Rarity = null, ItemCategory? Category = null)
    : IRequest<List<MagicItemDto>>;

public class GetMagicItemsQueryHandler : IRequestHandler<GetMagicItemsQuery, List<MagicItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IMapper _mapper;

    public GetMagicItemsQueryHandler(IApplicationDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<List<MagicItemDto>> Handle(GetMagicItemsQuery request, CancellationToken ct)
    {
        var query = _db.MagicItems.Where(m => m.CampaignId == request.CampaignId);

        if (request.Rarity.HasValue)
            query = query.Where(m => m.Rarity == request.Rarity.Value);

        if (request.Category.HasValue)
            query = query.Where(m => m.Category == request.Category.Value);

        return await query
            .OrderBy(m => m.Rarity).ThenBy(m => m.Name)
            .ProjectTo<MagicItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }
}

namespace DndCampaignManager.Application.Features.MagicItems.Commands;

public record CreateMagicItemCommand(
    string Name,
    string? Description,
    Rarity Rarity,
    ItemCategory Category,
    bool RequiresAttunement,
    string? AttunementRequirement,
    string? Source,
    Guid CampaignId
) : IRequest<Guid>;

public class CreateMagicItemCommandValidator : AbstractValidator<CreateMagicItemCommand>
{
    public CreateMagicItemCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CampaignId).NotEmpty();
    }
}

public class CreateMagicItemCommandHandler : IRequestHandler<CreateMagicItemCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateMagicItemCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateMagicItemCommand request, CancellationToken ct)
    {
        var item = new MagicItem
        {
            Name = request.Name,
            Description = request.Description,
            Rarity = request.Rarity,
            Category = request.Category,
            RequiresAttunement = request.RequiresAttunement,
            AttunementRequirement = request.AttunementRequirement,
            Source = request.Source,
            CampaignId = request.CampaignId
        };

        _db.MagicItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return item.Id;
    }
}

public record UpdateMagicItemCommand(
    Guid Id,
    string Name,
    string? Description,
    Rarity Rarity,
    ItemCategory Category,
    bool RequiresAttunement,
    string? AttunementRequirement,
    string? Source
) : IRequest;

public class UpdateMagicItemCommandHandler : IRequestHandler<UpdateMagicItemCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateMagicItemCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateMagicItemCommand request, CancellationToken ct)
    {
        var item = await _db.MagicItems.FindAsync(new object[] { request.Id }, ct)
            ?? throw new KeyNotFoundException($"Magic item {request.Id} not found");

        item.Name = request.Name;
        item.Description = request.Description;
        item.Rarity = request.Rarity;
        item.Category = request.Category;
        item.RequiresAttunement = request.RequiresAttunement;
        item.AttunementRequirement = request.AttunementRequirement;
        item.Source = request.Source;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}

public record DeleteMagicItemCommand(Guid Id) : IRequest;

public class DeleteMagicItemCommandHandler : IRequestHandler<DeleteMagicItemCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteMagicItemCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteMagicItemCommand request, CancellationToken ct)
    {
        var item = await _db.MagicItems.FindAsync(new object[] { request.Id }, ct)
            ?? throw new KeyNotFoundException($"Magic item {request.Id} not found");

        _db.MagicItems.Remove(item);
        await _db.SaveChangesAsync(ct);
    }
}
