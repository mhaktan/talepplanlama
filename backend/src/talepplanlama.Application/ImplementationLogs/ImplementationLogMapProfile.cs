using AutoMapper;
using talepplanlama.Entities;
using talepplanlama.ImplementationLogs.Dto;

namespace talepplanlama.ImplementationLogs
{
    public class ImplementationLogMapProfile : Profile
    {
        public ImplementationLogMapProfile()
        {
            CreateMap<ImplementationLog, ImplementationLogDto>();
            CreateMap<CreateImplementationLogDto, ImplementationLog>();
            CreateMap<ImplementationLogDto, ImplementationLog>();
        }
    }
}
