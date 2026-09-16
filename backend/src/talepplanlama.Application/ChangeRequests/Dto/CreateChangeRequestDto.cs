using System;
using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;

namespace talepplanlama.ChangeRequests.Dto
{
    [AutoMapTo(typeof(Entities.ChangeRequest))]
    public class CreateChangeRequestDto
    {
        [Required]
        [MaxLength(300)]
        public string Title { get; set; }

        [MaxLength(50)]
        public string RequestNumber { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; }

        [MaxLength(1000)]
        public string Justification { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public int Status { get; set; }

        [Required]
        public string FirstApproverRole { get; set; }

        [Required]
        public string SecondApproverRole { get; set; }

        [MaxLength(1000)]
        public string RevisionNote { get; set; }

        public long RequestTypeId { get; set; }

    }
}