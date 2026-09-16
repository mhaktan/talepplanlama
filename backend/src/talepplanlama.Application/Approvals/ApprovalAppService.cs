using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Net.Mail;
using Abp.Runtime.Session;
using Abp.UI;
using talepplanlama.Approvals.Dto;
using talepplanlama.Entities;
using talepplanlama.Flows;

namespace talepplanlama.Approvals
{
    public interface IApprovalAppService : IApplicationService
    {
        Task SubmitForApprovalAsync(SubmitApprovalInput input);
        Task<ApprovalRecordDto> ProcessApprovalAsync(ProcessApprovalInput input);
        Task<List<ApprovalRecordDto>> GetApprovalHistoryAsync(string entityType, string entityId);
        Task<List<StatusChangeLogDto>> GetStatusChangeLogsAsync(string entityType, string entityId);
        Task<List<PendingApprovalDto>> GetMyPendingApprovalsAsync();
    }

    // Entity-agnostik servis oldugu icin entity bazli permission verilemez; sinif duzeyinde
    // permission adi olmadan AbpAuthorize = "sadece authenticated kullanici" korumasi.
    // ProcessApprovalAsync ayrica kendi icinde assignee kontrolu yapar.
    [AbpAuthorize]
    public class ApprovalAppService : ApplicationService, IApprovalAppService
    {
        private readonly IRepository<ApprovalRecord, Guid> _approvalRepo;
        private readonly IRepository<StatusChangeLog, long> _statusChangeLogRepo;
        private readonly IRepository<AppUser, long> _userRepo;
        private readonly IRepository<AppRole, long> _roleRepo;
        private readonly IRepository<UserRole, long> _userRoleRepo;
        private readonly IEmailSender _emailSender;
        private readonly IFlowEngine _flowEngine;
        private readonly IIocResolver _iocResolver;

        public ApprovalAppService(
            IRepository<ApprovalRecord, Guid> approvalRepo,
            IRepository<StatusChangeLog, long> statusChangeLogRepo,
            IRepository<AppUser, long> userRepo,
            IRepository<AppRole, long> roleRepo,
            IRepository<UserRole, long> userRoleRepo,
            IEmailSender emailSender,
            IFlowEngine flowEngine,
            IIocResolver iocResolver)
        {
            _approvalRepo = approvalRepo;
            _statusChangeLogRepo = statusChangeLogRepo;
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _userRoleRepo = userRoleRepo;
            _emailSender = emailSender;
            _flowEngine = flowEngine;
            _iocResolver = iocResolver;
        }

        // Helper: returns user ids that belong to the named role (case-insensitive).
        private List<long> GetUserIdsInRole(string roleName)
        {
            if (string.IsNullOrEmpty(roleName)) return new List<long>();
            var role = _roleRepo.GetAll().FirstOrDefault(r => r.Name.ToLower() == roleName.ToLower());
            if (role == null) return new List<long>();
            return _userRoleRepo.GetAll().Where(ur => ur.RoleId == role.Id).Select(ur => ur.UserId).ToList();
        }

        // Helper: role names assigned to a user (used to expand "My Tasks" with role-broadcast records).
        /// <summary>
        /// Kullanicinin rol ADLARI. Navigation (ur.Role.Name) KULLANILMAZ: DbContext'te
        /// UserRole -> AppRole iliskisi eslenmemisse EF bos isim donduruyor ve rol bazli
        /// atanmis onaylar "My Tasks"ta hic gorunmuyor (sessiz hata, derleme kirilmiyor).
        /// Bunun yerine iki adimda, id uzerinden okunur.
        /// </summary>
        private List<string> GetUserRoleNames(long userId)
        {
            var roleIds = _userRoleRepo.GetAll()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToList();
            if (roleIds.Count == 0) return new List<string>();

            return _roleRepo.GetAll()
                .Where(r => roleIds.Contains(r.Id))
                .Select(r => r.Name)
                .Where(n => n != null)
                .ToList();
        }

        public async Task SubmitForApprovalAsync(SubmitApprovalInput input)
        {
            // Cancel any existing pending approvals for this entity
            var existing = _approvalRepo.GetAll()
                .Where(a => a.EntityType == input.EntityType && a.EntityId == input.EntityId && a.Status == "Pending")
                .ToList();
            foreach (var record in existing)
            {
                record.Status = "Cancelled";
                await _approvalRepo.UpdateAsync(record);
            }

            // Exactly one of AssigneeUserId / AssigneeRole must be set
            var hasUser = input.AssigneeUserId.HasValue && input.AssigneeUserId.Value > 0;
            var hasRole = !string.IsNullOrWhiteSpace(input.AssigneeRole);
            if (!hasUser && !hasRole)
                throw new UserFriendlyException("Approval submission requires either AssigneeUserId or AssigneeRole.");

            // Create first approval step
            var approvalRecord = new ApprovalRecord
            {
                EntityType = input.EntityType,
                EntityId = input.EntityId,
                FlowId = input.FlowId,
                NodeId = input.NodeId,
                StepIndex = 0,
                StepName = string.IsNullOrEmpty(input.StepName) ? "Step 1" : input.StepName,
                AssigneeUserId = hasUser ? input.AssigneeUserId : null,
                AssigneeRole = hasRole ? input.AssigneeRole : null,
                Status = "Pending"
            };

            await _approvalRepo.InsertAsync(approvalRecord);

            // Send notification email — failure here MUST NOT block record creation.
            // Role-based assignment fans the email out to every member of the role.
            if (!string.IsNullOrEmpty(input.EmailSubject))
            {
                var recipients = new List<AppUser>();
                try
                {
                    if (hasUser)
                    {
                        var u = await _userRepo.FirstOrDefaultAsync(input.AssigneeUserId.Value);
                        if (u != null) recipients.Add(u);
                    }
                    else if (hasRole)
                    {
                        var userIds = GetUserIdsInRole(input.AssigneeRole);
                        if (userIds.Count > 0)
                            recipients = _userRepo.GetAll().Where(u => userIds.Contains(u.Id)).ToList();
                    }

                    var body = string.IsNullOrEmpty(input.EmailBody)
                        ? $"<p>You have a pending approval: {input.EntityType} #{input.EntityId}</p>"
                        : input.EmailBody;

                    foreach (var r in recipients)
                    {
                        if (!string.IsNullOrEmpty(r.EmailAddress))
                            await _emailSender.SendAsync(r.EmailAddress, input.EmailSubject, body, true);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Approval notification email failed: {ex.Message}");
                }
            }
        }

        public async Task<ApprovalRecordDto> ProcessApprovalAsync(ProcessApprovalInput input)
        {
            var record = await _approvalRepo.GetAsync(input.ApprovalRecordId);

            if (record.Status != "Pending")
                throw new UserFriendlyException("This approval has already been processed.");

            // Authorization: caller must be the specifically assigned user OR a member of the
            // assigned role. Broadcast-style role assignments are first-to-act.
            var currentUserId = AbpSession.GetUserId();
            var canProcess = (record.AssigneeUserId.HasValue && record.AssigneeUserId.Value == currentUserId)
                             || (!string.IsNullOrEmpty(record.AssigneeRole)
                                 && GetUserRoleNames(currentUserId).Any(r => r.Equals(record.AssigneeRole, StringComparison.OrdinalIgnoreCase)));
            if (!canProcess)
                throw new UserFriendlyException("You are not assigned to this approval step.");

            record.ActionTaken = input.Action;
            record.Comment = input.Comment;
            record.ActionDate = DateTime.UtcNow;

            // Look up the approval node config so we can drive next-step + assignee from the flow
            // definition rather than relying on the client to send NextAssigneeUserId.
            var flow = _flowEngine.GetFlowById(record.FlowId);
            // NodeId eskiden her uretimde degisiyordu; eski kayitlar hicbir dugume
            // denk gelmeyince adim listesi BOS okunuyor, onay "son adim" sanilip
            // kalan adimlar sessizce atlaniyordu. Bulunamazsa akisin onay dugumune dus.
            var node = flow?.Nodes?.FirstOrDefault(n => n.Id == record.NodeId)
                       ?? flow?.Nodes?.FirstOrDefault(n => n.Approval?.Steps != null && n.Approval.Steps.Any());
            if (node != null && node.Id != record.NodeId)
            {
                Logger.Warn($"ApprovalRecord {record.Id} NodeId '{record.NodeId}' bulunamadi; '{node.Id}' dugumune duseldi.");
                record.NodeId = node.Id;
            }
            var steps = node?.Approval?.Steps ?? new List<FlowApprovalStep>();
            if (steps.Count == 0)
                throw new UserFriendlyException("Approval steps could not be resolved for this record (flow definition missing).");

            if (input.Action == "Approve")
            {
                var nextIdx = record.StepIndex + 1;
                var hasNextStep = nextIdx < steps.Count;

                FlowApprovalStep nextStep = null;
                long nextUserId = 0;
                string nextRole = null;

                if (hasNextStep)
                {
                    nextStep = steps[nextIdx];
                    var resolved = await ResolveAssigneeFromEntityAsync(record.EntityType, record.EntityId, nextStep);
                    nextUserId = resolved.userId;
                    nextRole = resolved.roleName;
                    if (nextUserId <= 0 && string.IsNullOrEmpty(nextRole))
                        throw new UserFriendlyException(
                            $"Next step '{nextStep.Name}' assignee could not be resolved (assigneeType={nextStep.AssigneeType}, value={nextStep.AssigneeValue}).");

                    record.NextAssigneeUserId = nextUserId > 0 ? (long?)nextUserId : null;
                }

                record.Status = "Approved";
                await _approvalRepo.UpdateAsync(record);

                // Once ilerlet: entity'nin state machine'i (ChangeStatusAsync) gecis haritasinin sahibi.
                // Sonraki adimin kaydi ancak status degisimi basarili olduktan sonra acilir — aksi halde
                // status degisimi patlarsa onaycinin gelen kutusunda sahte bir "Pending" kayit kalabilir.
                await TryChangeEntityStatusAsync(record.EntityType, record.EntityId, "Approve", input.Comment);

                if (hasNextStep)
                {
                    await _approvalRepo.InsertAsync(new ApprovalRecord
                    {
                        EntityType = record.EntityType,
                        EntityId = record.EntityId,
                        FlowId = record.FlowId,
                        NodeId = record.NodeId,
                        StepIndex = nextIdx,
                        StepName = nextStep.Name ?? $"Step {nextIdx + 1}",
                        AssigneeUserId = nextUserId > 0 ? (long?)nextUserId : null,
                        AssigneeRole = !string.IsNullOrEmpty(nextRole) ? nextRole : null,
                        Status = "Pending"
                    });
                }
            }
            else if (input.Action == "Revise")
            {
                record.Status = "Revised";
                await _approvalRepo.UpdateAsync(record);

                // Red her zaman entity'yi state machine'in "Revise" gecisine gonderir (tek davranis).
                // Hedef durum state machine tarafindan belirlenir; o durumla filtrelenen liste
                // olusturucunun revize gelen kutusu olur.
                await TryChangeEntityStatusAsync(record.EntityType, record.EntityId, "Revise", input.Comment);
            }

            return ObjectMapper.Map<ApprovalRecordDto>(record);
        }

        // Resolve the assignee for an approval step. Returns (userId, roleName).
        //   - assigneeType="fixed": numeric user id baked into the flow definition
        //   - assigneeType="role":  role name; broadcast to all members
        //   - assigneeType="field": look up the named property on the entity. The value may be
        //                           numeric (user id) or a string (role name) — auto-detected.
        private async Task<(long userId, string roleName)> ResolveAssigneeFromEntityAsync(string entityType, string entityIdStr, FlowApprovalStep step)
        {
            if (step == null) return (0, null);
            if (step.AssigneeType == "fixed" && long.TryParse(step.AssigneeValue, out var fixedId))
                return (fixedId, null);
            if (step.AssigneeType == "role" && !string.IsNullOrWhiteSpace(step.AssigneeValue))
                return (0, step.AssigneeValue);
            if (step.AssigneeType != "field" || string.IsNullOrWhiteSpace(step.AssigneeValue))
                return (0, null);
            if (!long.TryParse(entityIdStr, out var entityId)) return (0, null);

            var entityClrType = ResolveEntityClrType(entityType);
            if (entityClrType == null) return (0, null);

            // IRepository<TEntity, long> via reflection — keeps ApprovalAppService entity-agnostic.
            var repoIface = typeof(IRepository<,>).MakeGenericType(entityClrType, typeof(long));
            using (var disposable = _iocResolver.ResolveAsDisposable(repoIface))
            {
                var repo = disposable.Object;
                var firstOrDefault = repo.GetType().GetMethod("FirstOrDefaultAsync", new[] { typeof(long) });
                if (firstOrDefault == null) return (0, null);
                var task = (Task)firstOrDefault.Invoke(repo, new object[] { entityId });
                await task.ConfigureAwait(false);
                var entity = task.GetType().GetProperty("Result")?.GetValue(task);
                if (entity == null) return (0, null);

                var prop = entity.GetType().GetProperties()
                    .FirstOrDefault(p => string.Equals(p.Name, step.AssigneeValue, StringComparison.OrdinalIgnoreCase));
                var raw = prop?.GetValue(entity);
                if (raw == null) return (0, null);
                var s = raw.ToString();
                if (long.TryParse(s, out var assignee)) return (assignee, null);
                if (!string.IsNullOrWhiteSpace(s)) return (0, s);
                return (0, null);
            }
        }

        // Call I{EntityType}AppService.ChangeStatusAsync(id, ChangeStatusInput) by reflection.
        // Approval-driven transitions piggy-back on the entity's existing transition map so we don't
        // duplicate state-machine logic in two places.
        private async Task TryChangeEntityStatusAsync(string entityType, string entityIdStr, string action, string comment)
        {
            if (!long.TryParse(entityIdStr, out var entityId))
                throw new UserFriendlyException($"Approval record has an unusable entity id '{entityIdStr}'.");
            try
            {
                var rootNs = GetType().Namespace?.Split('.')[0] ?? "talepplanlama";
                var pluralNs = $"{rootNs}.{entityType}s";
                var ifaceTypeName = $"{pluralNs}.I{entityType}AppService, {rootNs}.Application";
                // Once ad tahmini, tutmazsa yuklu assembly'lerde ara. Tek bir namespace/
                // cogul-ek tahminine bagli kalmak tum onay akisini sessizce durduruyordu.
                var iface = System.Type.GetType(ifaceTypeName) ?? FindTypeByName($"I{entityType}AppService", true);
                if (iface == null)
                {
                    Logger.Error($"No app service interface for entity '{entityType}' (looked for {ifaceTypeName})");
                    throw new UserFriendlyException($"Status could not be updated: app service for '{entityType}' was not found.");
                }

                var method = iface.GetMethod("ChangeStatusAsync");
                if (method == null)
                {
                    Logger.Error($"{iface.Name} has no ChangeStatusAsync method.");
                    throw new UserFriendlyException($"Status could not be updated: '{entityType}' has no state machine.");
                }

                // ChangeStatusInput PAYLASILAN namespace'te ({ns}.StateMachine.Dto) uretiliyor.
                // Onceden entity bazli namespace'te aranıyordu; tip bulunamayinca bu metod
                // sessizce donuyor ve ONAY TAMAMLANSA BILE entity durumu ilerlemiyordu.
                // Eski projeler icin entity bazli ad da yedek olarak deneniyor.
                var changeStatusInputType =
                    System.Type.GetType($"{rootNs}.StateMachine.Dto.ChangeStatusInput, {rootNs}.Application")
                    ?? System.Type.GetType($"{pluralNs}.Dto.ChangeStatusInput, {rootNs}.Application")
                    ?? FindTypeByName("ChangeStatusInput", false);
                if (changeStatusInputType == null)
                {
                    Logger.Error($"ChangeStatusInput type missing for '{entityType}'");
                    throw new UserFriendlyException("Status could not be updated: ChangeStatusInput type was not found.");
                }

                var changeInput = Activator.CreateInstance(changeStatusInputType);
                changeStatusInputType.GetProperty("Action")?.SetValue(changeInput, action);
                var actionData = new Dictionary<string, string>
                {
                    ["comment"] = comment ?? string.Empty,
                    ["revisionNote"] = comment ?? string.Empty,
                };
                changeStatusInputType.GetProperty("ActionData")?.SetValue(changeInput, actionData);

                using (var disposable = _iocResolver.ResolveAsDisposable(iface))
                {
                    var task = (Task)method.Invoke(disposable.Object, new[] { entityId, changeInput });
                    await task.ConfigureAwait(false);
                }
            }
            catch (TargetInvocationException tie)
            {
                Logger.Error($"ChangeStatusAsync invoke failed for {entityType}#{entityIdStr}: {tie.InnerException?.Message}", tie.InnerException);
                if (tie.InnerException is UserFriendlyException ufe) throw ufe;
                throw new UserFriendlyException("Status update failed: " + (tie.InnerException?.Message ?? tie.Message));
            }
        }

        private System.Type ResolveEntityClrType(string entityType)
        {
            var rootNs = GetType().Namespace?.Split('.')[0] ?? "talepplanlama";
            return System.Type.GetType($"{rootNs}.Entities.{entityType}, {rootNs}.Core")
                ?? FindTypeByName(entityType, false);
        }

        // Yuklu assembly'lerde ada gore tip arar (namespace tahmini tutmadiginda yedek).
        // Sonuc onbellege alinir; GetTypes() her cagride pahalidir.
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Type> _typeCache =
            new System.Collections.Concurrent.ConcurrentDictionary<string, System.Type>();

        private static System.Type FindTypeByName(string typeName, bool interfaceOnly)
        {
            return _typeCache.GetOrAdd($"{typeName}|{interfaceOnly}", _ =>
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.IsDynamic) continue;
                    System.Type[] types;
                    try { types = asm.GetTypes(); }
                    catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }
                    catch { continue; }

                    var hit = types.FirstOrDefault(t =>
                        t.Name == typeName && (!interfaceOnly || t.IsInterface));
                    if (hit != null) return hit;
                }
                return null;
            });
        }

        public async Task<List<ApprovalRecordDto>> GetApprovalHistoryAsync(string entityType, string entityId)
        {
            var records = _approvalRepo.GetAll()
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderBy(a => a.CreationTime)
                .ToList();

            return ObjectMapper.Map<List<ApprovalRecordDto>>(records);
        }

        public async Task<List<StatusChangeLogDto>> GetStatusChangeLogsAsync(string entityType, string entityId)
        {
            var records = _statusChangeLogRepo.GetAll()
                .Where(r => r.EntityType == entityType && r.EntityId == entityId)
                .OrderBy(r => r.CreationTime)
                .ToList();

            return ObjectMapper.Map<List<StatusChangeLogDto>>(records);
        }

        public async Task<List<PendingApprovalDto>> GetMyPendingApprovalsAsync()
        {
            var userId = AbpSession.GetUserId();
            var userRoles = GetUserRoleNames(userId);
            // Pending = user is the named assignee OR the record is broadcast to a role this user belongs to.
            var records = _approvalRepo.GetAll()
                .Where(a => a.Status == "Pending"
                            && ((a.AssigneeUserId.HasValue && a.AssigneeUserId.Value == userId)
                                || (a.AssigneeRole != null && userRoles.Contains(a.AssigneeRole))))
                .OrderByDescending(a => a.CreationTime)
                .ToList();

            return records.Select(r => new PendingApprovalDto
            {
                ApprovalRecordId = r.Id,
                EntityType = r.EntityType,
                EntityId = r.EntityId,
                StepName = r.StepName,
                CreationTime = r.CreationTime,
                AvailableActions = new List<string> { "Approve", "Revise" }
            }).ToList();
        }
    }
}
