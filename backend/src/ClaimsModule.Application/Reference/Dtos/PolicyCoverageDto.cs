namespace ClaimsModule.Application.Reference.Dtos;

/// <summary>Coverage types available on a policy - used during FNOL.</summary>
public record PolicyCoverageDto(Guid PolicyId, string PolicyNumber, List<string> CoverageTypes);
