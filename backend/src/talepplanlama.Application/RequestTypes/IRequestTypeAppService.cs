using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using talepplanlama.Analytics.Dto;
using talepplanlama.RequestTypes.Dto;

namespace talepplanlama.RequestTypes
{
    public interface IRequestTypeAppService : IAsyncCrudAppService<
        RequestTypeDto,
        long,
        PagedRequestTypeResultRequestDto,
        CreateRequestTypeDto,
        RequestTypeDto>
    {
        Task<RequestTypeReportDto> GetReportData(long id);
    }
}
