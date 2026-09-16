using System;
using Abp.Application.Services.Dto;

namespace talepplanlama.ChangeRequests.Dto
{
    public class PagedChangeRequestResultRequestDto : PagedAndSortedResultRequestDto
    {
        public string Keyword { get; set; }
        public long? RequestTypeId { get; set; }
        public string Title { get; set; }
        public string RequestNumber { get; set; }
        public string Description { get; set; }
        public string Justification { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public int? Status { get; set; }
        public string FirstApproverRole { get; set; }
        public string SecondApproverRole { get; set; }
        public string RevisionNote { get; set; }
        public DateTime? EffectiveDateFrom { get; set; }
        public DateTime? EffectiveDateTo { get; set; }
        /// <summary>Virgülle ayrılmış enum indeksleri — ör. "0,2"</summary>
        public string StatusIn { get; set; }
        public int? StatusNot { get; set; }
    }
}
