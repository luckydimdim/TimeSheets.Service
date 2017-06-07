using AutoMapper;
using Cmas.BusinessLayers.TimeSheets.Entities;
using Cmas.Services.TimeSheets.Dtos.Responses;

namespace Cmas.Services.TimeSheets
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<TimeSheet, DetailedTimeSheetResponse>();
            CreateMap<TimeSheet, SimpleTimeSheetResponse>();
            CreateMap<Attachment, AttachmentResponse>();
        }
    }

}
