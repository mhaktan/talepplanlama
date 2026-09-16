using AutoMapper;
using talepplanlama.Approvals.Dto;
using talepplanlama.Entities;

namespace talepplanlama.Approvals
{
    public class ApprovalMapProfile : Profile
    {
        public ApprovalMapProfile()
        {
            CreateMap<ApprovalRecord, ApprovalRecordDto>();
            CreateMap<StatusChangeLog, StatusChangeLogDto>();
        }
    }
}
