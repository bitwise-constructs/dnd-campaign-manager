using DndCampaignManager.Application.Features.TreasureTables.Commands;
using DndCampaignManager.Application.Features.TreasureTables.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DndCampaignManager.API.Controllers;

[ApiController]
[Route("api/campaigns/{campaignId:guid}/[controller]")]
[Authorize]
public class TreasureTablesController : ControllerBase
{
    private readonly IMediator _mediator;

    public TreasureTablesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid campaignId)
    {
        var result = await _mediator.Send(new GetTreasureTablesQuery(campaignId));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid campaignId, Guid id)
    {
        var result = await _mediator.Send(new GetTreasureTableQuery(id));
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Generate a treasure table from selected magic items with weights.
    /// DM selects items via checkboxes on the admin page, assigns weights, and this builds the roll table.
    /// </summary>
    [HttpPost("generate")]
    [Authorize(Policy = "DmOnly")]
    public async Task<IActionResult> Generate(Guid campaignId, [FromBody] GenerateTreasureTableCommand command)
    {
        var cmd = command with { CampaignId = campaignId };
        var id = await _mediator.Send(cmd);
        return Created($"api/campaigns/{campaignId}/treasuretables/{id}", new { id });
    }

    /// <summary>
    /// Roll on a treasure table to get a random item
    /// </summary>
    [HttpPost("{id:guid}/roll")]
    public async Task<IActionResult> Roll(Guid campaignId, Guid id, [FromQuery] int? forcedRoll = null)
    {
        var result = await _mediator.Send(new RollTreasureTableQuery(id, forcedRoll));
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "DmOnly")]
    public async Task<IActionResult> Delete(Guid campaignId, Guid id)
    {
        await _mediator.Send(new DeleteTreasureTableCommand(id));
        return NoContent();
    }
}
