using DndCampaignManager.Application.Features.Wishlists.Commands;
using DndCampaignManager.Application.Features.Wishlists.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DndCampaignManager.API.Controllers;

[ApiController]
[Route("api/campaigns/{campaignId:guid}/wishlists")]
[Authorize]
public class WishlistsController : ControllerBase
{
    private readonly IMediator _mediator;

    public WishlistsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get all wishlists for a campaign: DM pool + character wishlists (DM view).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCampaignWishlists(Guid campaignId)
    {
        var result = await _mediator.Send(new GetCampaignWishlistsQuery(campaignId));
        return Ok(result);
    }

    /// <summary>
    /// Get wishlist for a specific character.
    /// </summary>
    [HttpGet("character/{characterId:guid}")]
    public async Task<IActionResult> GetCharacterWishlist(Guid campaignId, Guid characterId)
    {
        var result = await _mediator.Send(new GetCharacterWishlistQuery(characterId));
        return Ok(result);
    }

    /// <summary>
    /// Add an item to a character's wishlist. Can be a linked magic item or a custom plain-text entry.
    /// </summary>
    [HttpPost("character")]
    public async Task<IActionResult> AddToCharacterWishlist(
        Guid campaignId, [FromBody] AddToWishlistCommand command)
    {
        var id = await _mediator.Send(command);
        return Created($"api/campaigns/{campaignId}/wishlists/{id}", new { id });
    }

    /// <summary>
    /// Update priority of a wishlist item.
    /// </summary>
    [HttpPut("{id:guid}/priority")]
    public async Task<IActionResult> UpdatePriority(Guid campaignId, Guid id, [FromBody] int priority)
    {
        await _mediator.Send(new UpdateWishlistPriorityCommand(id, priority));
        return NoContent();
    }

    /// <summary>
    /// Remove a wishlist item.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid campaignId, Guid id)
    {
        await _mediator.Send(new RemoveWishlistItemCommand(id));
        return NoContent();
    }
}

/// <summary>
/// DM item pool and treasure table generation endpoints.
/// </summary>
[ApiController]
[Route("api/campaigns/{campaignId:guid}/dm-pool")]
[Authorize(Policy = "DmOnly")]
public class DmItemPoolController : ControllerBase
{
    private readonly IMediator _mediator;

    public DmItemPoolController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get the DM's general item pool for this campaign.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPool(Guid campaignId)
    {
        var result = await _mediator.Send(new GetDmItemPoolQuery(campaignId));
        return Ok(result);
    }

    /// <summary>
    /// Add an item to the DM pool. Can be a linked magic item or a custom plain-text entry.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddToPool(Guid campaignId, [FromBody] AddToDmPoolCommand command)
    {
        var cmd = command with { CampaignId = campaignId };
        var id = await _mediator.Send(cmd);
        return Created($"api/campaigns/{campaignId}/dm-pool/{id}", new { id });
    }

    /// <summary>
    /// Update the weight of any wishlist item (DM pool or character wishlist).
    /// </summary>
    [HttpPut("{id:guid}/weight")]
    public async Task<IActionResult> UpdateWeight(Guid campaignId, Guid id, [FromBody] int weight)
    {
        await _mediator.Send(new UpdateWishlistItemWeightCommand(id, weight));
        return NoContent();
    }

    /// <summary>
    /// Remove an item from the DM pool.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RemoveFromPool(Guid campaignId, Guid id)
    {
        await _mediator.Send(new RemoveWishlistItemCommand(id));
        return NoContent();
    }

    /// <summary>
    /// Pick top N items from the selected set using weighted random.
    /// The DM checks items from the pool and/or character wishlists,
    /// then this endpoint rolls against them.
    /// </summary>
    [HttpPost("pick")]
    public async Task<IActionResult> PickTopN(
        Guid campaignId, [FromBody] PickTopNRequest request)
    {
        var result = await _mediator.Send(new PickTopNCommand(
            request.SelectedItemIds, request.Count, request.UseWeights));
        return Ok(result);
    }
}

public record PickTopNRequest(List<Guid> SelectedItemIds, int Count, bool UseWeights);
