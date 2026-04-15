using AutoMapper;
using AutoMapper.QueryableExtensions;
using DndCampaignManager.Application.Common.Interfaces;
using DndCampaignManager.Application.Common.Models;
using DndCampaignManager.Domain.Entities;
using DndCampaignManager.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DndCampaignManager.Application.Features.TreasureTables.Queries;

public record GetTreasureTablesQuery(Guid CampaignId) : IRequest<List<TreasureTableDto>>;

public class GetTreasureTablesQueryHandler : IRequestHandler<GetTreasureTablesQuery, List<TreasureTableDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IMapper _mapper;

    public GetTreasureTablesQueryHandler(IApplicationDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<List<TreasureTableDto>> Handle(GetTreasureTablesQuery request, CancellationToken ct)
    {
        return await _db.TreasureTables
            .Include(t => t.Entries).ThenInclude(e => e.MagicItem)
            .Where(t => t.CampaignId == request.CampaignId)
            .OrderByDescending(t => t.CreatedAt)
            .ProjectTo<TreasureTableDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }
}

public record GetTreasureTableQuery(Guid Id) : IRequest<TreasureTableDto?>;

public class GetTreasureTableQueryHandler : IRequestHandler<GetTreasureTableQuery, TreasureTableDto?>
{
    private readonly IApplicationDbContext _db;
    private readonly IMapper _mapper;

    public GetTreasureTableQueryHandler(IApplicationDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<TreasureTableDto?> Handle(GetTreasureTableQuery request, CancellationToken ct)
    {
        return await _db.TreasureTables
            .Include(t => t.Entries).ThenInclude(e => e.MagicItem)
            .Where(t => t.Id == request.Id)
            .ProjectTo<TreasureTableDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);
    }
}

namespace DndCampaignManager.Application.Features.TreasureTables.Commands;

/// <summary>
/// Generates a treasure table from a selection of magic items.
/// The DM checks off items from the master list and/or wishlists,
/// and this command builds the table with weighted roll ranges.
/// </summary>
public record GenerateTreasureTableCommand(
    string Name,
    string? Description,
    Guid CampaignId,
    List<SelectedItemForTable> SelectedItems
) : IRequest<Guid>;

public record SelectedItemForTable(
    Guid MagicItemId,
    int Weight // Higher weight = more likely to appear when rolling
);

public class GenerateTreasureTableCommandValidator : AbstractValidator<GenerateTreasureTableCommand>
{
    public GenerateTreasureTableCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.SelectedItems).NotEmpty()
            .WithMessage("Select at least one item for the treasure table");
        RuleForEach(x => x.SelectedItems).ChildRules(item =>
        {
            item.RuleFor(x => x.MagicItemId).NotEmpty();
            item.RuleFor(x => x.Weight).GreaterThan(0);
        });
    }
}

public class GenerateTreasureTableCommandHandler : IRequestHandler<GenerateTreasureTableCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public GenerateTreasureTableCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(GenerateTreasureTableCommand request, CancellationToken ct)
    {
        // Verify all selected items exist in this campaign
        var itemIds = request.SelectedItems.Select(s => s.MagicItemId).ToList();
        var existingItems = await _db.MagicItems
            .Where(m => m.CampaignId == request.CampaignId && itemIds.Contains(m.Id))
            .Select(m => m.Id)
            .ToListAsync(ct);

        var missing = itemIds.Except(existingItems).ToList();
        if (missing.Any())
            throw new KeyNotFoundException($"Magic items not found in campaign: {string.Join(", ", missing)}");

        var table = new TreasureTable
        {
            Name = request.Name,
            Description = request.Description,
            CampaignId = request.CampaignId
        };

        // Calculate roll ranges from weights
        int totalWeight = request.SelectedItems.Sum(s => s.Weight);
        int currentMin = 1;
        int maxRoll = Math.Max(totalWeight, 100); // Use d100 minimum

        double scale = (double)maxRoll / totalWeight;

        foreach (var selected in request.SelectedItems.OrderBy(s => s.MagicItemId))
        {
            int rangeSize = Math.Max(1, (int)Math.Round(selected.Weight * scale));
            int entryMax = Math.Min(currentMin + rangeSize - 1, maxRoll);

            table.Entries.Add(new TreasureTableEntry
            {
                MagicItemId = selected.MagicItemId,
                Weight = selected.Weight,
                MinRoll = currentMin,
                MaxRoll = entryMax
            });

            currentMin = entryMax + 1;
        }

        // Adjust last entry to fill remaining range
        if (table.Entries.Any())
        {
            var last = table.Entries.Last();
            last.MaxRoll = maxRoll;
        }

        _db.TreasureTables.Add(table);
        await _db.SaveChangesAsync(ct);
        return table.Id;
    }
}

// --- Roll on a treasure table ---
public record RollTreasureTableQuery(Guid TreasureTableId, int? ForcedRoll = null)
    : IRequest<TreasureTableEntryDto>;

public class RollTreasureTableQueryHandler : IRequestHandler<RollTreasureTableQuery, TreasureTableEntryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IMapper _mapper;
    private static readonly Random _random = new();

    public RollTreasureTableQueryHandler(IApplicationDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<TreasureTableEntryDto> Handle(RollTreasureTableQuery request, CancellationToken ct)
    {
        var table = await _db.TreasureTables
            .Include(t => t.Entries).ThenInclude(e => e.MagicItem)
            .FirstOrDefaultAsync(t => t.Id == request.TreasureTableId, ct)
            ?? throw new KeyNotFoundException("Treasure table not found");

        if (!table.Entries.Any())
            throw new InvalidOperationException("Treasure table is empty");

        int maxRoll = table.Entries.Max(e => e.MaxRoll ?? 100);
        int roll = request.ForcedRoll ?? _random.Next(1, maxRoll + 1);

        var entry = table.Entries
            .FirstOrDefault(e => roll >= (e.MinRoll ?? 1) && roll <= (e.MaxRoll ?? maxRoll))
            ?? table.Entries.Last(); // Fallback

        return _mapper.Map<TreasureTableEntryDto>(entry);
    }
}

public record DeleteTreasureTableCommand(Guid Id) : IRequest;

public class DeleteTreasureTableCommandHandler : IRequestHandler<DeleteTreasureTableCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteTreasureTableCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteTreasureTableCommand request, CancellationToken ct)
    {
        var table = await _db.TreasureTables
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new KeyNotFoundException("Treasure table not found");

        _db.TreasureTableEntries.RemoveRange(table.Entries);
        _db.TreasureTables.Remove(table);
        await _db.SaveChangesAsync(ct);
    }
}
