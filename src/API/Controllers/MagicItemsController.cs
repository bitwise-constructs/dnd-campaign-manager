using DndCampaignManager.Application.Features.MagicItems.Commands;
using DndCampaignManager.Application.Features.MagicItems.Queries;
using DndCampaignManager.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DndCampaignManager.API.Controllers;

[ApiController]
[Route("api/campaigns/{campaignId:guid}/[controller]")]
[Authorize]
public class MagicItemsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MagicItemsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        Guid campaignId,
        [FromQuery] Rarity? rarity = null,
        [FromQuery] ItemCategory? category = null)
    {
        var result = await _mediator.Send(new GetMagicItemsQuery(campaignId, rarity, category));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "DmOnly")]
    public async Task<IActionResult> Create(Guid campaignId, [FromBody] CreateMagicItemCommand command)
    {
        var cmd = command with { CampaignId = campaignId };
        var id = await _mediator.Send(cmd);
        return Created($"api/campaigns/{campaignId}/magicitems/{id}", new { id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "DmOnly")]
    public async Task<IActionResult> Update(Guid campaignId, Guid id, [FromBody] UpdateMagicItemCommand command)
    {
        var cmd = command with { Id = id };
        await _mediator.Send(cmd);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "DmOnly")]
    public async Task<IActionResult> Delete(Guid campaignId, Guid id)
    {
        await _mediator.Send(new DeleteMagicItemCommand(id));
        return NoContent();
    }
}
