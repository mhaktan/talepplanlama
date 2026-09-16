using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace talepplanlama.Entities
{
    // State Machine: status — Draft → PendingFirstApproval → PendingSecondApproval → Revision → PendingRoutePlanning → PendingSystem → PendingOperations → Completed → Cancelled
    // Initial: Draft | Transitions: Draft→PendingFirstApproval[Submit], PendingFirstApproval→PendingSecondApproval[Approve], PendingFirstApproval→Revision[Revise], PendingSecondApproval→PendingRoutePlanning[Approve], PendingSecondApproval→Revision[Revise], Revision→PendingFirstApproval[Resubmit], PendingRoutePlanning→PendingSystem[Complete], PendingSystem→PendingOperations[Complete], PendingOperations→Completed[Complete], *→Cancelled[Cancel]
    [Table("ChangeRequests")]
    public class ChangeRequest : FullAuditedEntity<long>
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

        public ChangeRequestStatus Status { get; set; }

        [Required]
        public string FirstApproverRole { get; set; }

        [Required]
        public string SecondApproverRole { get; set; }

        [MaxLength(1000)]
        public string RevisionNote { get; set; }

        public long RequestTypeId { get; set; }

        [ForeignKey(nameof(RequestTypeId))]
        public virtual RequestType RequestType { get; set; }

        public virtual ICollection<ImplementationLog> ImplementationLogs { get; set; }

    }
}