using System;
using talepplanlama.Analytics.Dto;

namespace talepplanlama.ChangeRequests.Dto
{
    /// <summary>GetAll ile ayni filtreleri kabul eder, ustune GroupBy alir.</summary>
    public class ChangeRequestGroupedCountInput : PagedChangeRequestResultRequestDto
    {
        public string GroupBy { get; set; }
    }

    public class ChangeRequestStatsInput : PagedChangeRequestResultRequestDto
    {
        /// <summary>avg | sum | min | max | avgDayDiff</summary>
        public string Aggregate { get; set; }
        public string Field { get; set; }
        public string FromField { get; set; }
        public string ToField { get; set; }
    }
}
