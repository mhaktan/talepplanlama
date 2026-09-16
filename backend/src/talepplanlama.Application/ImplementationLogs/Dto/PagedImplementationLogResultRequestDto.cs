using System;
using Abp.Application.Services.Dto;

namespace talepplanlama.ImplementationLogs.Dto
{
    public class PagedImplementationLogResultRequestDto : PagedAndSortedResultRequestDto
    {
        public string Keyword { get; set; }
        public long? ChangeRequestId { get; set; }
        public int? Phase { get; set; }
        public string Notes { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CompletedAtFrom { get; set; }
        public DateTime? CompletedAtTo { get; set; }
        /// <summary>Virgülle ayrılmış enum indeksleri — ör. "0,2"</summary>
        public string PhaseIn { get; set; }
        public int? PhaseNot { get; set; }
    }
}
