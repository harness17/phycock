using Phycock.Entity.Enums;
using System.ComponentModel.DataAnnotations;

namespace Phycock.Models
{
    /// <summary>
    /// 通所予定の作成・編集フォーム ViewModel。
    /// </summary>
    public class ScheduleEntryFormViewModel
    {
        /// <summary>通所予定 ID。</summary>
        public long Id { get; set; }

        /// <summary>予定対象ユーザー ID。</summary>
        public string UserId { get; set; } = "";

        /// <summary>通所予定日。</summary>
        [Required(ErrorMessage = "日付は必須です")]
        [Display(Name = "日付")]
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        /// <summary>通所時間帯。</summary>
        [Display(Name = "時間帯")]
        public ScheduleSession Session { get; set; } = ScheduleSession.AM;

        /// <summary>在宅利用かどうか。</summary>
        [Display(Name = "在宅利用")]
        public bool IsAtHome { get; set; }

        /// <summary>予定または実績状態。</summary>
        [Display(Name = "状態")]
        public ScheduleStatus Status { get; set; } = ScheduleStatus.Planned;

        /// <summary>活動種別。</summary>
        [Display(Name = "活動種別")]
        public ActivityType ActivityType { get; set; } = ActivityType.Program;

        /// <summary>プログラム種別。</summary>
        [Display(Name = "プログラム種別")]
        public ProgramType? ProgramType { get; set; }

        /// <summary>活動内容の補足。</summary>
        [MaxLength(200, ErrorMessage = "活動内容は200文字以内で入力してください")]
        [Display(Name = "活動内容")]
        public string? ActivityNote { get; set; }

        /// <summary>開始時刻。</summary>
        [Display(Name = "開始時刻")]
        public TimeOnly? StartTime { get; set; }

        /// <summary>終了時刻。</summary>
        [Display(Name = "終了時刻")]
        public TimeOnly? EndTime { get; set; }

        /// <summary>自由記入メモ。</summary>
        [MaxLength(1000, ErrorMessage = "メモは1000文字以内で入力してください")]
        [Display(Name = "メモ")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// FullCalendar に渡す通所予定 JSON DTO。
    /// </summary>
    public class ScheduleEntryJsonDto
    {
        /// <summary>イベント ID。</summary>
        public string Id { get; set; } = "";

        /// <summary>表示タイトル。</summary>
        public string Title { get; set; } = "";

        /// <summary>開始日時または日付（ISO 8601）。</summary>
        public string Start { get; set; } = "";

        /// <summary>終了日時または日付（ISO 8601）。</summary>
        public string? End { get; set; }

        /// <summary>表示色。</summary>
        public string Color { get; set; } = "";

        /// <summary>背景色。</summary>
        public string BackgroundColor { get; set; } = "";

        /// <summary>枠線色。</summary>
        public string BorderColor { get; set; } = "";

        /// <summary>文字色。</summary>
        public string TextColor { get; set; } = "";

        /// <summary>追加属性。</summary>
        public ScheduleEntryExtendedProps ExtendedProps { get; set; } = new();
    }

    /// <summary>
    /// FullCalendar 用の通所予定追加属性。
    /// </summary>
    public class ScheduleEntryExtendedProps
    {
        /// <summary>時間帯（AM/PM/終日）。</summary>
        public string Session { get; set; } = "";

        /// <summary>在宅利用かどうか。</summary>
        public bool IsAtHome { get; set; }

        /// <summary>状態。</summary>
        public string Status { get; set; } = "";

        /// <summary>活動種別。</summary>
        public string ActivityType { get; set; } = "";

        /// <summary>活動内容の補足。</summary>
        public string? ActivityNote { get; set; }
    }

    /// <summary>
    /// 通所予定詳細表示 DTO。
    /// </summary>
    public class ScheduleEntryDetailDto
    {
        /// <summary>通所予定 ID。</summary>
        public long Id { get; set; }

        /// <summary>予定対象ユーザー ID。</summary>
        public string UserId { get; set; } = "";

        /// <summary>通所予定日。</summary>
        public string Date { get; set; } = "";

        /// <summary>時間帯。</summary>
        public ScheduleSession Session { get; set; }

        /// <summary>在宅利用かどうか。</summary>
        public bool IsAtHome { get; set; }

        /// <summary>状態。</summary>
        public ScheduleStatus Status { get; set; }

        /// <summary>活動種別。</summary>
        public ActivityType ActivityType { get; set; }

        /// <summary>プログラム種別。</summary>
        public ProgramType? ProgramType { get; set; }

        /// <summary>活動内容の補足。</summary>
        public string? ActivityNote { get; set; }

        /// <summary>開始時刻。</summary>
        public string? StartTime { get; set; }

        /// <summary>終了時刻。</summary>
        public string? EndTime { get; set; }

        /// <summary>自由記入メモ。</summary>
        public string? Notes { get; set; }
    }
}
