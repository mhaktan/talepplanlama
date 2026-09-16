using System;
using System.Collections.Generic;
using talepplanlama.ChangeRequests.Dto;

namespace talepplanlama.RequestTypes.Dto
{
    /// <summary>
    /// Rapor verisi — kok kayit ve alt koleksiyonlar tek yanitta.
    /// PDF sablonu tek apiBinding kullandigi icin nested donuyoruz.
    /// </summary>
    public class RequestTypeReportDto
    {
        public RequestTypeDto Data { get; set; }
        public List<ChangeRequestDto> ChangeRequests { get; set; }
    }
}
