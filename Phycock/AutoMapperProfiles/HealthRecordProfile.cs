using AutoMapper;
using Phycock.Entity;
using Phycock.Models;

namespace Phycock.AutoMapperProfiles
{
    /// <summary>
    /// 体調記録の AutoMapper プロファイル。
    /// </summary>
    public class HealthRecordProfile : Profile
    {
        /// <summary>
        /// 体調記録のマッピングを定義する。
        /// </summary>
        public HealthRecordProfile()
        {
            CreateMap<HealthRecordEntity, HealthRecordFormViewModel>()
                .ForMember(dest => dest.SelectedSymptoms, opt => opt.Ignore());
            CreateMap<HealthRecordFormViewModel, HealthRecordEntity>()
                .ForMember(dest => dest.SymptomFlags, opt => opt.Ignore());
            CreateMap<HealthRecordEntity, HealthRecordJsonDto>()
                .ForMember(dest => dest.RecordDate, opt => opt.MapFrom(src => src.RecordDate.ToString("yyyy/MM/dd")))
                .ForMember(dest => dest.RecordTiming, opt => opt.MapFrom(src => src.RecordTiming.ToString()))
                .ForMember(dest => dest.Symptoms, opt => opt.Ignore())
                .ForMember(dest => dest.Condition, opt => opt.Ignore())
                .ForMember(dest => dest.Feeling, opt => opt.Ignore());
        }
    }
}
