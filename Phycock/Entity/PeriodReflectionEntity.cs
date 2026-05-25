using Dev.CommonLibrary.Entity;
using Phycock.Entity.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Phycock.Entity
{
    /// <summary>
    /// 週次／月次レポートに対するユーザー所感。
    /// (UserId, PeriodType, PeriodStart) で一意。
    /// </summary>
    [Table("PeriodReflection")]
    public class PeriodReflectionEntity : PhycockEntityBase
    {
        /// <summary>記録対象ユーザーID（ApplicationUser.Id）。</summary>
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = "";

        /// <summary>集計期間種別。</summary>
        public PeriodType PeriodType { get; set; }

        /// <summary>期間開始日。週次=日曜、月次=月初（時刻 00:00）。</summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>自己評価。</summary>
        [MaxLength(1000)]
        public string? SelfEvaluation { get; set; }

        /// <summary>負担に感じたこと。</summary>
        [MaxLength(1000)]
        public string? Burden { get; set; }

        /// <summary>改善案。</summary>
        [MaxLength(1000)]
        public string? Improvement { get; set; }

        /// <summary>食欲の所感。</summary>
        [MaxLength(1000)]
        public string? Appetite { get; set; }

        /// <summary>睡眠の所感。</summary>
        [MaxLength(1000)]
        public string? Sleep { get; set; }
    }
}
