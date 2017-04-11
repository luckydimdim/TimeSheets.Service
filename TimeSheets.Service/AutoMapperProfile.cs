using AutoMapper;
using Cmas.BusinessLayers.TimeSheets.Entities;
using Cmas.Services.TimeSheets.Dtos;

namespace Cmas.Services.Requests
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<TimeSheet, DetailedTimeSheetDto>();
            CreateMap<TimeSheet, SimpleTimeSheetDto>();
        }
    }

}
