using DndCampaignManager.Application.Features.DndBeyondSync;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DndCampaignManager.API.Controllers;

[ApiController]
[Route("api/campaigns/{campaignId:guid}/characters/{characterId:guid}/dndbeyond")]
[Authorize]
public class DndBeyondController : ControllerBase
{
    private readonly IMediator _mediator;

    public DndBeyondController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Link a D&D Beyond character to a local character by DDB character ID.
    /// The DDB ID is the number in the URL: dndbeyond.com/characters/12345678
    /// Immediately attempts a sync after linking.
    /// </summary>
    [HttpPost("link")]
    public async Task<IActionResult> Link(
        Guid campaignId, Guid characterId,
        [FromBody] LinkDndBeyondRequest request)
    {
        var result = await _mediator.Send(new LinkDndBeyondCommand(characterId, request.DndBeyondCharacterId));
        return Ok(result);
    }

    /// <summary>
    /// Re-sync character data from D&D Beyond.
    /// If the endpoint is unreachable, cached data is retained and the response indicates failure.
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> Sync(Guid campaignId, Guid characterId)
    {
        var result = await _mediator.Send(new SyncDndBeyondCommand(characterId));
        return Ok(result);
    }

    /// <summary>
    /// Upload raw D&D Beyond character JSON as a fallback when the API endpoint is down.
    /// Players can get this from: character-service.dndbeyond.com/character/v5/character/{id}
    /// </summary>
    [HttpPost("upload-json")]
    public async Task<IActionResult> UploadJson(
        Guid campaignId, Guid characterId,
        [FromBody] UploadJsonRequest request)
    {
        var result = await _mediator.Send(new UploadDndBeyondJsonCommand(characterId, request.RawJson));
        return Ok(result);
    }

    /// <summary>
    /// Unlink a character from D&D Beyond. Retains all previously synced stats.
    /// </summary>
    [HttpDelete("link")]
    public async Task<IActionResult> Unlink(Guid campaignId, Guid characterId)
    {
        await _mediator.Send(new UnlinkDndBeyondCommand(characterId));
        return NoContent();
    }
}

/// <summary>
/// Batch sync all linked characters in a campaign (DM only).
/// </summary>
[ApiController]
[Route("api/campaigns/{campaignId:guid}/dndbeyond")]
[Authorize(Policy = "DmOnly")]
public class DndBeyondBatchController : ControllerBase
{
    private readonly IMediator _mediator;

    public DndBeyondBatchController(IMediator mediator) => _mediator = mediator;

    [HttpPost("sync-all")]
    public async Task<IActionResult> SyncAll(Guid campaignId)
    {
        var results = await _mediator.Send(new SyncAllDndBeyondCommand(campaignId));
        return Ok(results);
    }
}

// --- Request DTOs (kept minimal — the Command has validation) ---
public record LinkDndBeyondRequest(long DndBeyondCharacterId);
public record UploadJsonRequest(string RawJson);
