namespace ClaimsModule.Application.Common.Interfaces;

/// <summary>Atomic, per-year claim number generation - CLM-{YYYY}-{7-digit-sequence} (BR-C-04).</summary>
public interface IClaimNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
