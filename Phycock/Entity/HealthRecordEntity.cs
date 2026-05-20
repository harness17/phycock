using Dev.CommonLibrary.Entity;
using Phycock.Entity.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Phycock.Entity
{
    /// <summary>
    /// 体調記録エンティティ。
    /// </summary>
    [Table("HealthRecord")]
    public class HealthRecordEntity : PhycockEntityBase
    {
        /// <summary>記録対象ユーザーID（ApplicationUser.Id）。</summary>
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = "";

        /// <summary>記録日。</summary>
        public DateTime RecordDate { get; set; }

        /// <summary>記録タイミング。</summary>
        public RecordTiming RecordTiming { get; set; }

        /// <summary>任意時刻。RecordTiming が Custom の場合のみ使用する。</summary>
        public TimeOnly? RecordTime { get; set; }

        /// <summary>症状のビットフラグ。</summary>
        public long SymptomFlags { get; set; }

        /// <summary>身体的な体調レベル。</summary>
        public ConditionLevel Condition { get; set; }

        /// <summary>気分のレベル。</summary>
        public FeelingLevel Feeling { get; set; }

        /// <summary>自由記入メモ。</summary>
        [MaxLength(1000)]
        public string? Memo { get; set; }
    }
}
