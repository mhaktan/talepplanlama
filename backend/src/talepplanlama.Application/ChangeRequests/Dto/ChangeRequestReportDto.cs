using System;
using System.Collections.Generic;
using talepplanlama.ImplementationLogs.Dto;
using talepplanlama.Approvals.Dto;

namespace talepplanlama.ChangeRequests.Dto
{
    /// <summary>
    /// Rapor verisi — kok kayit ve alt koleksiyonlar tek yanitta.
    /// PDF sablonu tek apiBinding kullandigi icin nested donuyoruz.
    /// </summary>
    public class ChangeRequestReportDto
    {
        public ChangeRequestDto Data { get; set; }
        public List<ImplementationLogDto> ImplementationLogs { get; set; }
        public List<ApprovalRecordDto> ApprovalHistory { get; set; }
    }
}
