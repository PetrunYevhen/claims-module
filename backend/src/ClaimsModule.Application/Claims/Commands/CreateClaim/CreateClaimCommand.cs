using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Domain.Enums;
using MediatR;

namespace ClaimsModule.Application.Claims.Commands.CreateClaim;

public record CreatePartyRequest(
    PartyRole PartyRole,
    PartyType PartyType,
    string? FirstName,
    string? LastName,
    string? CompanyName,
    string? Email,
    string? Phone);

public record CreateRiskObjectRequest(
    AssetType AssetType,
    string AssetDescription,
    string? DamageDescription,
    bool IsPrimary,
    string? AssetReference);

/// <summary>FRS 5.2 Step 3: the optional initial reserve, created in the same transaction as the claim.</summary>
public record InitialReserveRequest(
    ReserveComponentType Component,
    decimal Amount,
    string? ChangeReason);

public record CreateClaimCommand : IRequest<ClaimDetailDto>
{
    public Guid? PolicyId { get; init; }
    public DateTimeOffset LossDate { get; init; }
    public string LossDescription { get; init; } = null!;
    public string? LossLocation { get; init; }
    public string CauseOfLossCode { get; init; } = null!;
    public decimal? EstimatedLossAmount { get; init; }
    public string? PoliceReportNumber { get; init; }
    public List<CreatePartyRequest> Parties { get; init; } = [];
    public List<CreateRiskObjectRequest> RiskObjects { get; init; } = [];
    public InitialReserveRequest? InitialReserve { get; init; }
}
