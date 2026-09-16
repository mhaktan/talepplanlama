using System;
using Abp.Application.Services.Dto;

namespace talepplanlama.RequestTypes.Dto
{
    public class PagedRequestTypeResultRequestDto : PagedAndSortedResultRequestDto
    {
        public string Keyword { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
