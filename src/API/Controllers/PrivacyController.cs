using DndCampaignManager.Application.Features.Privacy;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DndCampaignManager.API.Controllers;

[ApiController]
[Route("api/campaigns/{campaignId:guid}/characters/{characterId:guid}/privacy")]
[Authorize]
public class PrivacyController : ControllerBase
{
    private readonly IMediator _mediator;

    public PrivacyController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Get(Guid campaignId, Guid characterId)
    {
        var result = await _mediator.Send(new GetPrivacySettingsQuery(characterId));
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        Guid campaignId, Guid characterId,
        [FromBody] UpdatePrivacySettingsCommand command)
    {
        var cmd = command with { CharacterId = characterId };
        var result = await _mediator.Send(cmd);
        return Ok(result);
    }
}
