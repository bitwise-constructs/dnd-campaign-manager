namespace DndCampaignManager.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? DisplayName { get; }
    bool IsAuthenticated { get; }
    bool IsDm(Guid campaignId);
    IEnumerable<string> Roles { get; }
}
