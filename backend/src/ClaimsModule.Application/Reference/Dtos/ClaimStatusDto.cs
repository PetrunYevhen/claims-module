namespace ClaimsModule.Application.Reference.Dtos;

public record ClaimStatusDto(string Status, IReadOnlyList<string> ValidNextStatuses);
