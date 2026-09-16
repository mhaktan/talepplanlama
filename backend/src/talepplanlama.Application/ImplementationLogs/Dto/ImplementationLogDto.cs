using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;

namespace talepplanlama.ImplementationLogs.Dto
{
    [AutoMapFrom(typeof(Entities.ImplementationLog))]
    public class ImplementationLogDto : EntityDto<long>
    {
        public int Phase { get; set; }

        public string Notes { get; set; }

        public DateTime? CompletedAt { get; set; }

        public long ChangeRequestId { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime? LastModificationTime { get; set; }

    }
}