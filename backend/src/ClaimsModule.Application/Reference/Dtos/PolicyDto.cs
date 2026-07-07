namespace ClaimsModule.Application.Reference.Dtos;

public class PolicyDto
{
    public Guid PolicyId { get; set; }
    public string PolicyNumber { get; set; } = null!;
    public string ClientName { get; set; } = null!;
    public DateOnly EffectiveDate { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public string Status { get; set; } = null!;
    public string CoverageTypes { get; set; } = null!;
}
