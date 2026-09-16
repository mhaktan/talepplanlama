using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;

namespace talepplanlama.ChangeRequests.Dto
{
    [AutoMapFrom(typeof(Entities.ChangeRequest))]
    public class ChangeRequestDto : EntityDto<long>
    {
        public string Title { get; set; }

        public string RequestNumber { get; set; }

        public string Description { get; set; }

        public string Justification { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public int Status { get; set; }

        public string FirstApproverRole { get; set; }

        public string SecondApproverRole { get; set; }

        public string RevisionNote { get; set; }

        /// <summary>
        /// String form of the status — used by flow conditions (triggerData.statusName equals "PendingX").
        /// </summary>
        public string StatusName { get; set; }

        public long RequestTypeId { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime? LastModificationTime { get; set; }

    }
}