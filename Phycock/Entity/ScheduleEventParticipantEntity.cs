using Dev.CommonLibrary.Entity;
using Phycock.Common;
using Phycock.Entity.Enums;
using System.ComponentModel.DataAnnotations;

namespace Phycock.Entity
{
    /// <summary>
    /// スケジュールイベント参加者エンティティ。
    /// 更新頻度・性質が本体と異なるため履歴テーブルなし。
    /// </summary>
    public class ScheduleEventParticipantEntity : PhycockEntityBase
    {
        /// <summary>対象イベント ID（FK → ScheduleEventEntity）</summary>
        public long EventId { get; set; }

        /// <summary>参加者 UserId（FK → ApplicationUser）</summary>
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = "";

        /// <summary>参加ステータス</summary>
        public ParticipantStatus Status { get; set; } = ParticipantStatus.Invited;
    }
}
