using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using talepplanlama.Analytics.Dto;
using talepplanlama.StateMachine.Dto;
using talepplanlama.ChangeRequests.Dto;

namespace talepplanlama.ChangeRequests
{
    public interface IChangeRequestAppService : IAsyncCrudAppService<
        ChangeRequestDto,
        long,
        PagedChangeRequestResultRequestDto,
        CreateChangeRequestDto,
        ChangeRequestDto>
    {
        Task<ChangeRequestDto> ChangeStatusAsync(long id, ChangeStatusInput input);
        List<GroupCountDto> GetGroupedCount(ChangeRequestGroupedCountInput input);
        Task<ChangeRequestReportDto> GetReportData(long id);
    }
}
