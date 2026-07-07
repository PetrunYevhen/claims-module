using AutoMapper;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Reference.Dtos;
using ClaimsModule.Domain.Entities;

namespace ClaimsModule.Application.Claims.Mapping;

public class ClaimsMappingProfile : Profile
{
    public ClaimsMappingProfile()
    {
        CreateMap<Claim, ClaimSummaryDto>()
            .ForMember(d => d.LossDate, o => o.MapFrom(s => s.LossEvent != null ? s.LossEvent.LossDate : (DateTimeOffset?)null))
            .ForMember(d => d.CauseOfLossCode, o => o.MapFrom(s => s.LossEvent != null ? s.LossEvent.CauseOfLossCode : null))
            .ForMember(d => d.TotalReserves, o => o.MapFrom(s => s.ReserveComponents.Sum(rc => rc.CurrentAmount)));

        CreateMap<Claim, ClaimDetailDto>()
            .ForMember(d => d.LossEvent, o => o.Ignore())
            .ForMember(d => d.Parties, o => o.Ignore())
            .ForMember(d => d.RiskObjects, o => o.Ignore())
            .ForMember(d => d.ValidationIssues, o => o.Ignore())
            .ForMember(d => d.ValidNextStatuses, o => o.Ignore());

        CreateMap<ClaimAuditLog, AuditLogEntryDto>();

        CreateMap<ClaimDocument, ClaimDocumentDto>()
            .ForMember(d => d.DownloadUrl, o => o.Ignore());

        CreateMap<LossEvent, LossEventDto>();
        CreateMap<ClaimParty, ClaimPartyDto>();
        CreateMap<ClaimRiskObject, ClaimRiskObjectDto>();

        CreateMap<CauseOfLossCode, CauseOfLossCodeDto>()
            .ForMember(d => d.PerilCategory, o => o.MapFrom(s => s.PerilCategory.ToString()));

        CreateMap<Policy, PolicyDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));
    }
}
