using System.Security.Claims;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Common;

namespace ClaimsModule.API.Auth;

/// <summary>
/// Reads the current user's identity from the validated JWT set by ASP.NET Core's auth middleware.
/// Hangfire background jobs (PostGLReserveChangeJob, SlaMonitoringJob) run outside any HTTP request,
/// so there is no HttpContext/principal there - this degrades to a fixed "system" identity in that
/// case rather than throwing, since those jobs still need to write audit log entries.
/// </summary>
public class ClaimsPrincipalCurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private static readonly Guid SystemUserId = new("00000000-0000-0000-0000-000000000001");

    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public Guid OrganisationId => User is null
        ? SeedConstants.OrganisationId
        : Guid.Parse(User.FindFirstValue("org") ?? throw new InvalidOperationException("Token is missing the 'org' claim."));

    public Guid UserId => User is null
        ? SystemUserId
        : Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Token is missing the user id claim."));

    public string Role => User?.FindFirstValue(ClaimTypes.Role) ?? "system";
}
