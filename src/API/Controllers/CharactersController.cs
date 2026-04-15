using DndCampaignManager.Application.Features.Characters.Commands;
using DndCampaignManager.Application.Features.Characters.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DndCampaignManager.API.Controllers;

[ApiController]
[Route("api/campaigns/{campaignId:guid}/[controller]")]
[Authorize]
public class CharactersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CharactersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid campaignId)
    {
        var result = await _mediator.Send(new GetCharactersQuery(campaignId));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid campaignId, Guid id)
    {
        var result = await _mediator.Send(new GetCharacterQuery(id));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid campaignId, [FromBody] CreateCharacterCommand command)
    {
        // Ensure campaignId from route is used
        var cmd = command with { CampaignId = campaignId };
        var id = await _mediator.Send(cmd);
        return CreatedAtAction(nameof(Get), new { campaignId, id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid campaignId, Guid id, [FromBody] UpdateCharacterCommand command)
    {
        var cmd = command with { Id = id };
        await _mediator.Send(cmd);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "DmOnly")]
    public async Task<IActionResult> Delete(Guid campaignId, Guid id)
    {
        await _mediator.Send(new DeleteCharacterCommand(id));
        return NoContent();
    }
}
