using DndCampaignManager.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DndCampaignManager.API.Controllers;

[ApiController]
[Route("api/campaigns/{campaignId:guid}/magic-item-search")]
[Authorize]
public class MagicItemSearchController : ControllerBase
{
    private readonly IMagicItemSearchService _searchService;

    public MagicItemSearchController(IMagicItemSearchService searchService)
        => _searchService = searchService;

    /// <summary>
    /// Search for magic items across local campaign collection, Open5e, and the SRD API.
    /// Used by both players (adding to wishlists) and DMs (adding to pool/collection).
    /// Debounce on the frontend at ~300ms.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search(
        Guid campaignId,
        [FromQuery] string q,
        [FromQuery] int maxResults = 15)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(Array.Empty<object>());

        var results = await _searchService.SearchAsync(q, campaignId, maxResults);
        return Ok(results);
    }
}
