using System.Security.Claims;
using DndCampaignManager.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DndCampaignManager.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IApplicationDbContext _db;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IApplicationDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    // Entra ID uses 'oid' claim for object ID, or 'sub' as fallback
    public string? UserId => User?.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
        ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? DisplayName => User?.FindFirstValue("name")
        ?? User?.FindFirstValue(ClaimTypes.Name);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<string> Roles => User?.FindAll(ClaimTypes.Role).Select(c => c.Value)
        ?? Enumerable.Empty<string>();

    public bool IsDm(Guid campaignId)
    {
        if (UserId == null) return false;
        // Check if the current user is the DM of the given campaign
        return _db.Campaigns.Any(c => c.Id == campaignId && c.DmUserId == UserId);
    }
}
