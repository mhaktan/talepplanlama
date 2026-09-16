using System;
using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;

namespace talepplanlama.ImplementationLogs.Dto
{
    [AutoMapTo(typeof(Entities.ImplementationLog))]
    public class CreateImplementationLogDto
    {
        public int Phase { get; set; }

        [MaxLength(2000)]
        public string Notes { get; set; }

        public DateTime? CompletedAt { get; set; }

        public long ChangeRequestId { get; set; }

    }
}