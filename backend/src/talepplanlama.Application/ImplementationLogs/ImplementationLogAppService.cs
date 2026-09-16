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
using talepplanlama.ImplementationLogs.Dto;
using talepplanlama.Analytics.Dto;
using talepplanlama.Authorization;
using talepplanlama.Flows;

namespace talepplanlama.ImplementationLogs
{
    public class ImplementationLogAppService : AsyncCrudAppService<
        ImplementationLog,
        ImplementationLogDto,
        long,
        PagedImplementationLogResultRequestDto,
        CreateImplementationLogDto,
        ImplementationLogDto>,
        IImplementationLogAppService
    {
        private readonly IFlowEngine _flowEngine;

        public ImplementationLogAppService(IRepository<ImplementationLog, long> repository, IFlowEngine flowEngine)
            : base(repository)
        {
            _flowEngine = flowEngine;
            // Claim-based authorization (JwtPermissionChecker reads JWT "permission" claims)
            GetPermissionName = PermissionNames.ImplementationLog_Read;
            GetAllPermissionName = PermissionNames.ImplementationLog_Read;
            CreatePermissionName = PermissionNames.ImplementationLog_Create;
            UpdatePermissionName = PermissionNames.ImplementationLog_Update;
            DeletePermissionName = PermissionNames.ImplementationLog_Delete;
        }

        protected override IQueryable<ImplementationLog> CreateFilteredQuery(PagedImplementationLogResultRequestDto input)
        {
            return Repository.GetAll()
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x =>
                    x.Id.ToString().Contains(input.Keyword) ||
                    (x.Notes != null && x.Notes.Contains(input.Keyword)))
                .WhereIf(!input.Notes.IsNullOrWhiteSpace(), x => x.Notes != null && x.Notes.Contains(input.Notes))
                .WhereIf(input.Phase.HasValue, x => x.Phase == (ImplementationLogPhase)input.Phase.Value)
                .WhereIf(input.CompletedAt.HasValue, x => x.CompletedAt == input.CompletedAt.Value)
                .WhereIf(input.CompletedAtFrom.HasValue, x => x.CompletedAt >= input.CompletedAtFrom.Value)
                .WhereIf(input.CompletedAtTo.HasValue, x => x.CompletedAt <= input.CompletedAtTo.Value)
                .WhereIf(!input.PhaseIn.IsNullOrWhiteSpace(), x => input.PhaseIn
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => (ImplementationLogPhase)int.Parse(v.Trim()))
                    .Contains(x.Phase))
                .WhereIf(input.PhaseNot.HasValue, x => x.Phase != (ImplementationLogPhase)input.PhaseNot.Value)
                .WhereIf(input.ChangeRequestId.HasValue, x => x.ChangeRequestId == input.ChangeRequestId.Value);
        }

        public override async Task<ImplementationLogDto> CreateAsync(CreateImplementationLogDto input)
        {
            var result = await base.CreateAsync(input);
            await _flowEngine.TriggerAsync("on-create", "ImplementationLog", result);
            return result;
        }

        public override async Task<ImplementationLogDto> UpdateAsync(ImplementationLogDto input)
        {
            var result = await base.UpdateAsync(input);
            await _flowEngine.TriggerAsync("on-update", "ImplementationLog", result);
            return result;
        }

        public override async Task DeleteAsync(EntityDto<long> input)
        {
            await base.DeleteAsync(input);
            await _flowEngine.TriggerAsync("on-delete", "ImplementationLog", new { Id = input.Id });
        }
        [Abp.Authorization.AbpAuthorize(PermissionNames.ImplementationLog_Read)]
        public List<GroupCountDto> GetGroupedCount(ImplementationLogGroupedCountInput input)
        {
            // Whitelist — istemciden gelen alan adı doğrudan sorguya girmez.
            var allowed = new[] { "Phase", "ChangeRequestId" };
            if (input.GroupBy == null || !allowed.Contains(input.GroupBy))
            {
                throw new Abp.UI.UserFriendlyException(
                    $"Gruplanabilir alan degil: {input.GroupBy}. Izin verilenler: {string.Join(", ", allowed)}");
            }

            var query = CreateFilteredQuery(input);

            switch (input.GroupBy)
            {
                case "Phase":
                    return query
                        .GroupBy(x => x.Phase)
                        .Select(g => new GroupCountDto
                        {
                            Key = ((int)g.Key).ToString(),
                            Label = g.Key.ToString(),
                            Count = g.Count(),
                        })
                        .ToList();
                case "ChangeRequestId":
                    return query
                        .GroupBy(x => new { Key = x.ChangeRequestId, Label = x.ChangeRequest == null ? null : x.ChangeRequest.Title })
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

    }
}
