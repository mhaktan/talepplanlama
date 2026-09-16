using AutoMapper;
using talepplanlama.Entities;
using talepplanlama.ChangeRequests.Dto;

namespace talepplanlama.ChangeRequests
{
    public class ChangeRequestMapProfile : Profile
    {
        public ChangeRequestMapProfile()
        {
            CreateMap<ChangeRequest, ChangeRequestDto>()
                .ForMember(d => d.StatusName, o => o.MapFrom(s => s.Status.ToString()));
            CreateMap<CreateChangeRequestDto, ChangeRequest>();
            CreateMap<ChangeRequestDto, ChangeRequest>();
        }
    }
}
