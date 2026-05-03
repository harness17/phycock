using System.ComponentModel.DataAnnotations;

namespace Phycock.Entity.Enums
{
    /// <summary>
    /// 体調記録を登録したタイミング。
    /// </summary>
    public enum RecordTiming
    {
        /// <summary>起床時。</summary>
        [Display(Name = "起床時")]
        Morning = 0,

        /// <summary>訓練開始時。</summary>
        [Display(Name = "訓練開始時")]
        Noon = 1,

        /// <summary>訓練終了時。</summary>
        [Display(Name = "訓練終了時")]
        Evening = 2,

        /// <summary>就眠時。</summary>
        [Display(Name = "就眠時")]
        Night = 3
    }

    /// <summary>
    /// 身体的な体調レベル。
    /// </summary>
    public enum ConditionLevel
    {
        /// <summary>とても悪い。</summary>
        [Display(Name = "とても悪い")]
        VeryBad = 1,

        /// <summary>悪い。</summary>
        [Display(Name = "悪い")]
        Bad = 2,

        /// <summary>普通。</summary>
        [Display(Name = "普通")]
        Normal = 3,

        /// <summary>良い。</summary>
        [Display(Name = "良い")]
        Good = 4,

        /// <summary>とても良い。</summary>
        [Display(Name = "とても良い")]
        VeryGood = 5
    }

    /// <summary>
    /// 気分のレベル。
    /// </summary>
    public enum FeelingLevel
    {
        /// <summary>とても悪い。</summary>
        [Display(Name = "とても悪い")]
        VeryBad = 1,

        /// <summary>悪い。</summary>
        [Display(Name = "悪い")]
        Bad = 2,

        /// <summary>普通。</summary>
        [Display(Name = "普通")]
        Normal = 3,

        /// <summary>良い。</summary>
        [Display(Name = "良い")]
        Good = 4,

        /// <summary>とても良い。</summary>
        [Display(Name = "とても良い")]
        VeryGood = 5
    }
}
