using Dev.CommonLibrary.Entity;
using Phycock.Entity.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Phycock.Entity
{
    /// <summary>
    /// 睡眠記録エンティティ。
    /// </summary>
    [Table("SleepRecord")]
    public class SleepRecordEntity : PhycockEntityBase
    {
        /// <summary>記録対象ユーザーID（ApplicationUser.Id）。</summary>
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = "";

        /// <summary>記録日。</summary>
        public DateTime RecordDate { get; set; }

        /// <summary>睡眠開始日時。</summary>
        public DateTime StartDate { get; set; }

        /// <summary>睡眠終了日時。未完了の睡眠は null。</summary>
        public DateTime? EndDate { get; set; }

        /// <summary>睡眠種別。</summary>
        public SleepType SleepType { get; set; }

        /// <summary>自由記入メモ。</summary>
        [MaxLength(500)]
        public string? Memo { get; set; }
    }
}
