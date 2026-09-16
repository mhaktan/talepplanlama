using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using talepplanlama.Entities;
using talepplanlama.ChangeRequests.Dto;
using talepplanlama.Analytics.Dto;
using talepplanlama.StateMachine.Dto;
using talepplanlama.ImplementationLogs.Dto;
using talepplanlama.Approvals.Dto;
using talepplanlama.Authorization;
using talepplanlama.Flows;

namespace talepplanlama.ChangeRequests
{
    public class ChangeRequestAppService : AsyncCrudAppService<
        ChangeRequest,
        ChangeRequestDto,
        long,
        PagedChangeRequestResultRequestDto,
        CreateChangeRequestDto,
        ChangeRequestDto>,
        IChangeRequestAppService
    {
        private readonly IRepository<StatusChangeLog, long> _statusChangeLogRepo;
        private readonly IRepository<ApprovalRecord, Guid> _approvalRepo;
        private readonly IFlowEngine _flowEngine;

        public ChangeRequestAppService(IRepository<ChangeRequest, long> repository, IFlowEngine flowEngine, IRepository<StatusChangeLog, long> statusChangeLogRepo, IRepository<ApprovalRecord, Guid> approvalRepo)
            : base(repository)
        {
            _flowEngine = flowEngine;
            _statusChangeLogRepo = statusChangeLogRepo;
            _approvalRepo = approvalRepo;
            // Claim-based authorization (JwtPermissionChecker reads JWT "permission" claims)
            GetPermissionName = PermissionNames.ChangeRequest_Read;
            GetAllPermissionName = PermissionNames.ChangeRequest_Read;
            CreatePermissionName = PermissionNames.ChangeRequest_Create;
            UpdatePermissionName = PermissionNames.ChangeRequest_Update;
            DeletePermissionName = PermissionNames.ChangeRequest_Delete;
        }

        protected override IQueryable<ChangeRequest> CreateFilteredQuery(PagedChangeRequestResultRequestDto input)
        {
            return Repository.GetAll()
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x =>
                    x.Id.ToString().Contains(input.Keyword) ||
                    (x.Title != null && x.Title.Contains(input.Keyword)) ||
                    (x.RequestNumber != null && x.RequestNumber.Contains(input.Keyword)) ||
                    (x.Description != null && x.Description.Contains(input.Keyword)) ||
                    (x.Justification != null && x.Justification.Contains(input.Keyword)) ||
                    (x.FirstApproverRole != null && x.FirstApproverRole.Contains(input.Keyword)) ||
                    (x.SecondApproverRole != null && x.SecondApproverRole.Contains(input.Keyword)) ||
                    (x.RevisionNote != null && x.RevisionNote.Contains(input.Keyword)))
                .WhereIf(!input.Title.IsNullOrWhiteSpace(), x => x.Title != null && x.Title.Contains(input.Title))
                .WhereIf(!input.RequestNumber.IsNullOrWhiteSpace(), x => x.RequestNumber != null && x.RequestNumber.Contains(input.RequestNumber))
                .WhereIf(!input.Description.IsNullOrWhiteSpace(), x => x.Description != null && x.Description.Contains(input.Description))
                .WhereIf(!input.Justification.IsNullOrWhiteSpace(), x => x.Justification != null && x.Justification.Contains(input.Justification))
                .WhereIf(!input.FirstApproverRole.IsNullOrWhiteSpace(), x => x.FirstApproverRole != null && x.FirstApproverRole.Contains(input.FirstApproverRole))
                .WhereIf(!input.SecondApproverRole.IsNullOrWhiteSpace(), x => x.SecondApproverRole != null && x.SecondApproverRole.Contains(input.SecondApproverRole))
                .WhereIf(!input.RevisionNote.IsNullOrWhiteSpace(), x => x.RevisionNote != null && x.RevisionNote.Contains(input.RevisionNote))
                .WhereIf(input.EffectiveDate.HasValue, x => x.EffectiveDate == input.EffectiveDate.Value)
                .WhereIf(input.Status.HasValue, x => x.Status == (ChangeRequestStatus)input.Status.Value)
                .WhereIf(input.EffectiveDateFrom.HasValue, x => x.EffectiveDate >= input.EffectiveDateFrom.Value)
                .WhereIf(input.EffectiveDateTo.HasValue, x => x.EffectiveDate <= input.EffectiveDateTo.Value)
                .WhereIf(!input.StatusIn.IsNullOrWhiteSpace(), x => input.StatusIn
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => (ChangeRequestStatus)int.Parse(v.Trim()))
                    .Contains(x.Status))
                .WhereIf(input.StatusNot.HasValue, x => x.Status != (ChangeRequestStatus)input.StatusNot.Value)
                .WhereIf(input.RequestTypeId.HasValue, x => x.RequestTypeId == input.RequestTypeId.Value);
        }

        public override async Task<ChangeRequestDto> CreateAsync(CreateChangeRequestDto input)
        {
            var result = await base.CreateAsync(input);
            await _flowEngine.TriggerAsync("on-create", "ChangeRequest", result);

            // Frontend creates records with status pre-set without going through ChangeStatusAsync,
            // so mirror on-field-change here whenever the initial status isn't the default. Otherwise
            // status-driven flows (e.g. approval) never fire on plain Create.
            if (result.Status != (int)ChangeRequestStatus.Draft)
                await _flowEngine.TriggerAsync("on-field-change", "ChangeRequest", result);
            return result;
        }

        public override async Task<ChangeRequestDto> UpdateAsync(ChangeRequestDto input)
        {
            // State machine: validate status transition + log
            var existing = await Repository.GetAsync(input.Id);
            var statusChanged = (int)existing.Status != input.Status;
            if (statusChanged)
            {
                var fromStatus = existing.Status.ToString();
                var toStatus = ((ChangeRequestStatus)input.Status).ToString();
                ValidateStatusTransition(existing.Status, (ChangeRequestStatus)input.Status);

                // Log status change
                await _statusChangeLogRepo.InsertAsync(new StatusChangeLog
                {
                    EntityType = "ChangeRequest",
                    EntityId = input.Id.ToString(),
                    FromStatus = fromStatus,
                    ToStatus = toStatus,
                    Action = "Update",
                    ChangedByUserId = AbpSession.UserId
                });
            }

            var result = await base.UpdateAsync(input);
            await _flowEngine.TriggerAsync("on-update", "ChangeRequest", result);

            // Frontend updates status via plain UpdateAsync (not ChangeStatusAsync) — fire
            // on-field-change so status-driven flows pick up the transition.
            if (statusChanged)
                await _flowEngine.TriggerAsync("on-field-change", "ChangeRequest", result);
            return result;
        }

        public override async Task DeleteAsync(EntityDto<long> input)
        {
            await base.DeleteAsync(input);
            await _flowEngine.TriggerAsync("on-delete", "ChangeRequest", new { Id = input.Id });
        }

        // Onay adimini tamamlayan rol genelde Update degil ChangeStatus yetkisine sahip olur;
        // RequireAllPermissions=false ile ikisinden biri yeterli (geriye donuk uyumlu).
        [Abp.Authorization.AbpAuthorize(PermissionNames.ChangeRequest_ChangeStatus, PermissionNames.ChangeRequest_Update, RequireAllPermissions = false)]
        public async Task<ChangeRequestDto> ChangeStatusAsync(long id, ChangeStatusInput input)
        {
            var entity = await Repository.GetAsync(id);
            var currentStatus = entity.Status.ToString();

            // Find valid transition
            var transitions = new (string From, string To, string Action, bool Readonly)[]
            {
            ("Draft", "PendingFirstApproval", "Submit", false),
            ("PendingFirstApproval", "PendingSecondApproval", "Approve", false),
            ("PendingFirstApproval", "Revision", "Revise", false),
            ("PendingSecondApproval", "PendingRoutePlanning", "Approve", false),
            ("PendingSecondApproval", "Revision", "Revise", false),
            ("Revision", "PendingFirstApproval", "Resubmit", false),
            ("PendingRoutePlanning", "PendingSystem", "Complete", false),
            ("PendingSystem", "PendingOperations", "Complete", false),
            ("PendingOperations", "Completed", "Complete", false),
            ("*", "Cancelled", "Cancel", false)
            };

            var transition = transitions.FirstOrDefault(t =>
                (t.From == "*" || t.From == currentStatus) && t.Action == input.Action);

            if (transition == default)
                throw new Abp.UI.UserFriendlyException($"Invalid action '{input.Action}' from status '{currentStatus}'");

            // Onay ekranindan gelen serbest metni gecisin zorunlu alanlarina tasi
            if (input.ActionData == null) input.ActionData = new Dictionary<string, string>();
            var genericNote = input.ActionData.ContainsKey("comment") && !string.IsNullOrWhiteSpace(input.ActionData["comment"])
                ? input.ActionData["comment"]
                : (input.ActionData.ContainsKey("revisionNote") ? input.ActionData["revisionNote"] : null);
            if (!string.IsNullOrWhiteSpace(genericNote))
            {
                foreach (var rf in new[] { "revisionNote" })
                {
                    if (!input.ActionData.ContainsKey(rf) || string.IsNullOrWhiteSpace(input.ActionData[rf]))
                        input.ActionData[rf] = genericNote;
                }
            }
            // Validate required fields per transition
            if (input.Action == "Revise" && (input.ActionData == null || !input.ActionData.ContainsKey("revisionNote") || string.IsNullOrWhiteSpace(input.ActionData["revisionNote"])))
                throw new Abp.UI.UserFriendlyException("Revise requires: revisionNote");
            // Bu gecisler icin bagli kayit on kosulu yok

            var fromStatus = currentStatus;

            // Apply new status
            entity.Status = (ChangeRequestStatus)Enum.Parse(typeof(ChangeRequestStatus), transition.To);
            if (input.ActionData != null && input.ActionData.ContainsKey("revisionNote") && !string.IsNullOrWhiteSpace(input.ActionData["revisionNote"]))
                entity.RevisionNote = input.ActionData["revisionNote"];

            await Repository.UpdateAsync(entity);
            await CurrentUnitOfWork.SaveChangesAsync();

            // Cancel pending ApprovalRecords when the entity is cancelled — otherwise the records
            // sit forever in approvers' inboxes pointing to a cancelled request.
            if (input.Action == "Cancel")
            {
                var pending = _approvalRepo.GetAll()
                    .Where(a => a.EntityType == "ChangeRequest" && a.EntityId == id.ToString() && a.Status == "Pending")
                    .ToList();
                foreach (var pendingRec in pending)
                {
                    pendingRec.Status = "Cancelled";
                    pendingRec.ActionTaken = "Cancel";
                    pendingRec.ActionDate = DateTime.UtcNow;
                    pendingRec.Comment = "Entity cancelled by submitter.";
                    await _approvalRepo.UpdateAsync(pendingRec);
                }
            }

            // Log status change
            await _statusChangeLogRepo.InsertAsync(new Entities.StatusChangeLog
            {
                EntityType = "ChangeRequest",
                EntityId = id.ToString(),
                FromStatus = fromStatus,
                ToStatus = transition.To,
                Action = input.Action,
                Comment = input.ActionData != null && input.ActionData.ContainsKey("comment") ? input.ActionData["comment"] : null,
                ChangedByUserId = AbpSession.UserId
            });

            var result = MapToEntityDto(entity);

            // Trigger flow: on-status-change (always)
            await _flowEngine.TriggerAsync("on-field-change", "ChangeRequest", result);

            return result;
        }

        private void ValidateStatusTransition(ChangeRequestStatus from, ChangeRequestStatus to)
        {
            var allowed = new (string From, string To)[]
            {
                ("Draft", "PendingFirstApproval"),
                ("PendingFirstApproval", "PendingSecondApproval"),
                ("PendingFirstApproval", "Revision"),
                ("PendingSecondApproval", "PendingRoutePlanning"),
                ("PendingSecondApproval", "Revision"),
                ("Revision", "PendingFirstApproval"),
                ("PendingRoutePlanning", "PendingSystem"),
                ("PendingSystem", "PendingOperations"),
                ("PendingOperations", "Completed"),
                ("*", "Cancelled")
            };

            var isValid = allowed.Any(t =>
                (t.From == "*" || t.From == from.ToString()) &&
                t.To == to.ToString());

            if (!isValid)
                throw new Abp.UI.UserFriendlyException($"Invalid status transition from {from} to {to}");
        }
        [Abp.Authorization.AbpAuthorize(PermissionNames.ChangeRequest_Read)]
        public List<GroupCountDto> GetGroupedCount(ChangeRequestGroupedCountInput input)
        {
            // Whitelist — istemciden gelen alan adı doğrudan sorguya girmez.
            var allowed = new[] { "Status", "RequestTypeId" };
            if (input.GroupBy == null || !allowed.Contains(input.GroupBy))
            {
                throw new Abp.UI.UserFriendlyException(
                    $"Gruplanabilir alan degil: {input.GroupBy}. Izin verilenler: {string.Join(", ", allowed)}");
            }

            var query = CreateFilteredQuery(input);

            switch (input.GroupBy)
            {
                case "Status":
                    return query
                        .GroupBy(x => x.Status)
                        .Select(g => new GroupCountDto
                        {
                            Key = ((int)g.Key).ToString(),
                            Label = g.Key.ToString(),
                            Count = g.Count(),
                        })
                        .ToList();
                case "RequestTypeId":
                    return query
                        .GroupBy(x => new { Key = x.RequestTypeId, Label = x.RequestType == null ? null : x.RequestType.Name })
                        .Select(g => new GroupCountDto
                        {
                            Key = g.Key.Key.ToString(),
                            Label = g.Key.Label ?? "(bos)",
                            Count = g.Count(),
                        })
                        .ToList();
                default:
                    return new List<GroupCountDto>();
            }
        }

        /// <summary>
        /// Rapor verisi — kok kayit ve alt koleksiyonlar TEK yanitta.
        /// PDF sablonu template basina tek apiBinding kullaniyor.
        /// </summary>
        [Abp.Authorization.AbpAuthorize(PermissionNames.ChangeRequest_Read)]
        public async Task<ChangeRequestReportDto> GetReportData(long id)
        {
            var root = await Repository.GetAll()
                .Include(x => x.ImplementationLogs)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (root == null)
                throw new Abp.UI.UserFriendlyException($"Kayit bulunamadi: {id}");

            return new ChangeRequestReportDto
            {
                Data = ObjectMapper.Map<ChangeRequestDto>(root),
                ImplementationLogs = ObjectMapper.Map<List<ImplementationLogDto>>(
                    root.ImplementationLogs == null ? new List<ImplementationLog>() : root.ImplementationLogs.ToList()),
                // ApprovalRecord alan adlari: EntityType (isim) ve EntityId (STRING).
                // Once EntityName/long varsayilmisti — CS1061 + CS0019 veriyordu.
                ApprovalHistory = ObjectMapper.Map<List<ApprovalRecordDto>>(
                    _approvalRepo.GetAll()
                        .Where(a => a.EntityType == "ChangeRequest" && a.EntityId == id.ToString())
                        .OrderBy(a => a.StepIndex).ToList()),
            };
        }

    }
}
