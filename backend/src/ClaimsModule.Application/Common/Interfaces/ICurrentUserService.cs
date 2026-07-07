namespace ClaimsModule.Application.Common.Interfaces;

/// <summary>
/// Resolves the calling user's identity/role. Implemented by a hardcoded stub until Stage 7
/// wires up the mock JWT bearer auth service.
/// </summary>
public interface ICurrentUserService
{
    Guid OrganisationId { get; }
    Guid UserId { get; }
    string Role { get; }
}
