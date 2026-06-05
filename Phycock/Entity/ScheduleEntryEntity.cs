using Dev.CommonLibrary.Entity;
using Phycock.Entity.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Phycock.Entity
{
    /// <summary>
    /// 通所予定エンティティ。
    /// </summary>
    [Table("ScheduleEntry")]
    public class ScheduleEntryEntity : PhycockEntityBase
    {
        /// <summary>予定対象ユーザーID（ApplicationUser.Id）。</summary>
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = "";

        /// <summary>通所予定日。</summary>
        public DateOnly Date { get; set; }

        /// <summary>通所時間帯。</summary>
        public ScheduleSession Session { get; set; }

        /// <summary>在宅利用かどうか。</summary>
        public bool IsAtHome { get; set; }

        /// <summary>予定または実績状態。</summary>
        public ScheduleStatus Status { get; set; }

        /// <summary>活動種別。</summary>
        public ActivityType ActivityType { get; set; }

        /// <summary>プログラム種別。活動種別がプログラム以外の場合は null。</summary>
        public ProgramType? ProgramType { get; set; }

        /// <summary>活動内容の補足。</summary>
        [MaxLength(200)]
        public string? ActivityNote { get; set; }

        /// <summary>開始時刻。</summary>
        public TimeOnly? StartTime { get; set; }

        /// <summary>終了時刻。</summary>
        public TimeOnly? EndTime { get; set; }

        /// <summary>自由記入メモ。</summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
