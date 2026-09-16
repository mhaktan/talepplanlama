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
using talepplanlama.RequestTypes.Dto;
using talepplanlama.Analytics.Dto;
using talepplanlama.ChangeRequests.Dto;
using talepplanlama.Approvals.Dto;
using talepplanlama.Authorization;
using talepplanlama.Flows;

namespace talepplanlama.RequestTypes
{
    public class RequestTypeAppService : AsyncCrudAppService<
        RequestType,
        RequestTypeDto,
        long,
        PagedRequestTypeResultRequestDto,
        CreateRequestTypeDto,
        RequestTypeDto>,
        IRequestTypeAppService
    {
        private readonly IFlowEngine _flowEngine;

        public RequestTypeAppService(IRepository<RequestType, long> repository, IFlowEngine flowEngine)
            : base(repository)
        {
            _flowEngine = flowEngine;
            // Claim-based authorization (JwtPermissionChecker reads JWT "permission" claims)
            GetPermissionName = PermissionNames.RequestType_Read;
            GetAllPermissionName = PermissionNames.RequestType_Read;
            CreatePermissionName = PermissionNames.RequestType_Create;
            UpdatePermissionName = PermissionNames.RequestType_Update;
            DeletePermissionName = PermissionNames.RequestType_Delete;
        }

        protected override IQueryable<RequestType> CreateFilteredQuery(PagedRequestTypeResultRequestDto input)
        {
            return Repository.GetAll()
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x =>
                    x.Id.ToString().Contains(input.Keyword) ||
                    (x.Name != null && x.Name.Contains(input.Keyword)) ||
                    (x.Description != null && x.Description.Contains(input.Keyword)))
                .WhereIf(!input.Name.IsNullOrWhiteSpace(), x => x.Name != null && x.Name.Contains(input.Name))
                .WhereIf(!input.Description.IsNullOrWhiteSpace(), x => x.Description != null && x.Description.Contains(input.Description));
        }

        public override async Task<RequestTypeDto> CreateAsync(CreateRequestTypeDto input)
        {
            var result = await base.CreateAsync(input);
            await _flowEngine.TriggerAsync("on-create", "RequestType", result);
            return result;
        }

        public override async Task<RequestTypeDto> UpdateAsync(RequestTypeDto input)
        {
            var result = await base.UpdateAsync(input);
            await _flowEngine.TriggerAsync("on-update", "RequestType", result);
            return result;
        }

        public override async Task DeleteAsync(EntityDto<long> input)
        {
            await base.DeleteAsync(input);
            await _flowEngine.TriggerAsync("on-delete", "RequestType", new { Id = input.Id });
        }
        /// <summary>
        /// Rapor verisi — kok kayit ve alt koleksiyonlar TEK yanitta.
        /// PDF sablonu template basina tek apiBinding kullaniyor.
        /// </summary>
        [Abp.Authorization.AbpAuthorize(PermissionNames.RequestType_Read)]
        public async Task<RequestTypeReportDto> GetReportData(long id)
        {
            var root = await Repository.GetAll()
                .Include(x => x.ChangeRequests)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (root == null)
                throw new Abp.UI.UserFriendlyException($"Kayit bulunamadi: {id}");

            return new RequestTypeReportDto
            {
                Data = ObjectMapper.Map<RequestTypeDto>(root),
                ChangeRequests = ObjectMapper.Map<List<ChangeRequestDto>>(
                    root.ChangeRequests == null ? new List<ChangeRequest>() : root.ChangeRequests.ToList()),
            };
        }

    }
}
