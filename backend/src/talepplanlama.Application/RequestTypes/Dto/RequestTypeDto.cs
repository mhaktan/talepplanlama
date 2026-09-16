using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;

namespace talepplanlama.RequestTypes.Dto
{
    [AutoMapFrom(typeof(Entities.RequestType))]
    public class RequestTypeDto : EntityDto<long>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime? LastModificationTime { get; set; }

    }
}