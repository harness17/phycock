using AutoMapper;
using Phycock.Entity;
using Phycock.Models;

namespace Phycock.AutoMapperProfiles
{
    /// <summary>
    /// 睡眠記録の AutoMapper プロファイル。
    /// </summary>
    public class SleepRecordProfile : Profile
    {
        /// <summary>
        /// 睡眠記録のマッピングを定義する。
        /// </summary>
        public SleepRecordProfile()
        {
            CreateMap<SleepRecordEntity, SleepRecordFormViewModel>().ReverseMap();
        }
    }
}
