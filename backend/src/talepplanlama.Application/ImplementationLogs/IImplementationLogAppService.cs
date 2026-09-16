using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using talepplanlama.Analytics.Dto;
using talepplanlama.ImplementationLogs.Dto;

namespace talepplanlama.ImplementationLogs
{
    public interface IImplementationLogAppService : IAsyncCrudAppService<
        ImplementationLogDto,
        long,
        PagedImplementationLogResultRequestDto,
        CreateImplementationLogDto,
        ImplementationLogDto>
    {
        List<GroupCountDto> GetGroupedCount(ImplementationLogGroupedCountInput input);
    }
}
