using AutoMapper;
using talepplanlama.Entities;
using talepplanlama.RequestTypes.Dto;

namespace talepplanlama.RequestTypes
{
    public class RequestTypeMapProfile : Profile
    {
        public RequestTypeMapProfile()
        {
            CreateMap<RequestType, RequestTypeDto>();
            CreateMap<CreateRequestTypeDto, RequestType>();
            CreateMap<RequestTypeDto, RequestType>();
        }
    }
}
