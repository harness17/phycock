using Phycock.Entity.Enums;
using System.ComponentModel.DataAnnotations;

namespace Phycock.Models
{
    /// <summary>
    /// 睡眠記録の作成・編集フォーム ViewModel。
    /// </summary>
    public class SleepRecordFormViewModel
    {
        /// <summary>睡眠記録 ID。</summary>
        public long Id { get; set; }

        /// <summary>記録対象ユーザー ID。</summary>
        public string UserId { get; set; } = "";

        /// <summary>記録日。</summary>
        [Required(ErrorMessage = "記録日は必須です")]
        [DataType(DataType.Date)]
        [Display(Name = "記録日")]
        public DateTime RecordDate { get; set; } = DateTime.Today;

        /// <summary>睡眠開始日時。</summary>
        [Display(Name = "開始日時")]
        public DateTime StartDate { get; set; }

        /// <summary>睡眠終了日時。</summary>
        [Display(Name = "終了日時")]
        public DateTime? EndDate { get; set; }

        /// <summary>睡眠開始時刻。</summary>
        [Required(ErrorMessage = "開始時刻は必須です")]
        [DataType(DataType.Time)]
        [Display(Name = "開始時刻")]
        public TimeOnly? StartTime { get; set; }

        /// <summary>睡眠終了時刻。</summary>
        [DataType(DataType.Time)]
        [Display(Name = "終了時刻")]
        public TimeOnly? EndTime { get; set; }

        /// <summary>睡眠種別。</summary>
        [Display(Name = "睡眠種別")]
        public SleepType SleepType { get; set; } = SleepType.NightSleep;

        /// <summary>自由記入メモ。</summary>
        [MaxLength(500, ErrorMessage = "メモは500文字以内で入力してください")]
        [Display(Name = "メモ")]
        public string? Memo { get; set; }
    }

    /// <summary>
    /// 睡眠記録の一覧表示 ViewModel。
    /// </summary>
    public class SleepRecordListViewModel
    {
        /// <summary>睡眠記録 ID。</summary>
        public long Id { get; set; }

        /// <summary>記録対象ユーザー ID。</summary>
        public string UserId { get; set; } = "";

        /// <summary>記録日。</summary>
        public DateTime RecordDate { get; set; }

        /// <summary>睡眠開始日時。</summary>
        public DateTime StartDate { get; set; }

        /// <summary>睡眠終了日時。</summary>
        public DateTime? EndDate { get; set; }

        /// <summary>睡眠種別。</summary>
        public SleepType SleepType { get; set; }

        /// <summary>睡眠時間。</summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>自由記入メモ。</summary>
        public string? Memo { get; set; }
    }

    /// <summary>
    /// FullCalendar に渡す睡眠記録 JSON DTO。
    /// </summary>
    public class SleepRecordCalendarEventDto
    {
        /// <summary>イベント ID。</summary>
        public string Id { get; set; } = "";

        /// <summary>表示タイトル。</summary>
        public string Title { get; set; } = "";

        /// <summary>開始日時（ISO 8601）。</summary>
        public string Start { get; set; } = "";

        /// <summary>終了日時（ISO 8601）。</summary>
        public string? End { get; set; }

        /// <summary>表示色。</summary>
        public string Color { get; set; } = "";
    }
}
