using AutoMapper;
using Phycock.Entity;
using Phycock.Models;

namespace Phycock.AutoMapperProfiles
{
    /// <summary>
    /// 通所予定の AutoMapper プロファイル。
    /// </summary>
    public class ScheduleEntryProfile : Profile
    {
        /// <summary>
        /// 通所予定のマッピングを定義する。
        /// </summary>
        public ScheduleEntryProfile()
        {
            CreateMap<ScheduleEntryEntity, ScheduleEntryFormViewModel>().ReverseMap();
            CreateMap<ScheduleEntryEntity, ScheduleEntryJsonDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
                .ForMember(dest => dest.Title, opt => opt.Ignore())
                .ForMember(dest => dest.Start, opt => opt.Ignore())
                .ForMember(dest => dest.End, opt => opt.Ignore())
                .ForMember(dest => dest.Color, opt => opt.Ignore())
                .ForMember(dest => dest.ExtendedProps, opt => opt.Ignore());
        }
    }
}
