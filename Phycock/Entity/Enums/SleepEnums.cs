using System.ComponentModel.DataAnnotations;

namespace Phycock.Entity.Enums
{
    /// <summary>
    /// 睡眠記録の種別。
    /// </summary>
    public enum SleepType
    {
        /// <summary>本睡眠。</summary>
        [Display(Name = "本睡眠")]
        NightSleep = 0,

        /// <summary>日中の仮眠。</summary>
        [Display(Name = "仮眠")]
        DaytimeNap = 1,

        /// <summary>医療機関・支援施設などでの休息。</summary>
        [Display(Name = "施設での休息")]
        MedicalFacilityRest = 2,

        /// <summary>その他。</summary>
        [Display(Name = "その他")]
        Other = 3
    }
}
