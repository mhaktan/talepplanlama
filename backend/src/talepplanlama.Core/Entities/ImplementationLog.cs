using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace talepplanlama.Entities
{
    [Table("ImplementationLogs")]
    public class ImplementationLog : FullAuditedEntity<long>
    {
        public ImplementationLogPhase Phase { get; set; }

        [MaxLength(2000)]
        public string Notes { get; set; }

        public DateTime? CompletedAt { get; set; }

        public long ChangeRequestId { get; set; }

        [ForeignKey(nameof(ChangeRequestId))]
        public virtual ChangeRequest ChangeRequest { get; set; }

    }
}