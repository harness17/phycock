using System.ComponentModel.DataAnnotations;

namespace Phycock.Entity.Enums
{
    /// <summary>
    /// 体調記録で選択できる症状。
    /// </summary>
    [Flags]
    public enum SymptomType : long
    {
        /// <summary>未選択。</summary>
        [Display(Name = "なし")]
        None = 0L,

        /// <summary>頭痛。</summary>
        [Display(Name = "頭痛")]
        Headache = 1L,

        /// <summary>腹痛。</summary>
        [Display(Name = "腹痛")]
        Stomachache = 2L,

        /// <summary>吐き気。</summary>
        [Display(Name = "吐き気")]
        Nausea = 4L,

        /// <summary>めまい。</summary>
        [Display(Name = "めまい")]
        Dizziness = 8L,

        /// <summary>発熱。</summary>
        [Display(Name = "発熱")]
        Fever = 16L,

        /// <summary>食欲不振。</summary>
        [Display(Name = "食欲不振")]
        LossOfAppetite = 32L,

        /// <summary>動悸。</summary>
        [Display(Name = "動悸")]
        Palpitation = 64L,

        /// <summary>落ち込み。</summary>
        [Display(Name = "落ち込み")]
        Depression = 128L,

        /// <summary>不安。</summary>
        [Display(Name = "不安")]
        Anxiety = 256L,

        /// <summary>いらいら。</summary>
        [Display(Name = "いらいら")]
        Irritability = 512L,

        /// <summary>意欲低下。</summary>
        [Display(Name = "意欲低下")]
        LackOfMotivation = 1024L,

        /// <summary>集中力低下。</summary>
        [Display(Name = "集中力低下")]
        LackOfConcentration = 2048L,

        /// <summary>焦り。</summary>
        [Display(Name = "焦り")]
        Impatience = 4096L,

        /// <summary>眠気。</summary>
        [Display(Name = "眠気")]
        Drowsiness = 8192L,

        /// <summary>疲労。</summary>
        [Display(Name = "疲労")]
        Fatigue = 16384L,

        /// <summary>体が重い。</summary>
        [Display(Name = "体が重い")]
        HeavyBody = 32768L,

        /// <summary>筋緊張。</summary>
        [Display(Name = "筋緊張")]
        MuscleTension = 65536L,

        /// <summary>感覚過敏。</summary>
        [Display(Name = "感覚過敏")]
        SensoryOverload = 131072L
    }
}
