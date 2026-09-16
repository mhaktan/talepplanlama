using System;
using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;

namespace talepplanlama.RequestTypes.Dto
{
    [AutoMapTo(typeof(Entities.RequestType))]
    public class CreateRequestTypeDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

    }
}