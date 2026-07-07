using AutoMapper;
using ClaimsModule.Application.Reserves.Dtos;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Reserves.Mapping;

public class ReservesMappingProfile : Profile
{
    public ReservesMappingProfile()
    {
        CreateMap<ReserveHistory, ReserveHistoryDto>();

        CreateMap<ClaimReserveComponent, ReserveComponentDto>()
            .ForMember(d => d.PendingAmount, o => o.MapFrom(s =>
                s.History.Where(h => h.ApprovalStatus == ReserveApprovalStatus.PendingApproval).Sum(h => h.Amount)));
    }
}
