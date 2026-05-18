using System.ComponentModel.DataAnnotations;

namespace Phycock.Entity.Enums
{
    /// <summary>
    /// 通所予定の時間帯。
    /// </summary>
    public enum ScheduleSession
    {
        /// <summary>午前。</summary>
        [Display(Name = "午前")]
        AM = 0,

        /// <summary>午後。</summary>
        [Display(Name = "午後")]
        PM = 1,

        /// <summary>終日。</summary>
        [Display(Name = "終日")]
        AllDay = 2
    }

    /// <summary>
    /// 通所予定の実績状態。
    /// </summary>
    public enum ScheduleStatus
    {
        /// <summary>予定。</summary>
        [Display(Name = "予定")]
        [CalendarColor("#6C757D", "#6C757D", "#FFFFFF")]
        Planned = 0,

        /// <summary>通所済み。</summary>
        [Display(Name = "通所済み")]
        [CalendarColor("#198754", "#198754", "#FFFFFF")]
        Attended = 1,

        /// <summary>欠席。</summary>
        [Display(Name = "欠席")]
        [CalendarColor("#DC3545", "#DC3545", "#FFFFFF")]
        Absent = 2,

        /// <summary>遅刻。</summary>
        [Display(Name = "遅刻")]
        [CalendarColor("#FFC107", "#FFC107", "#212529")]
        Late = 3,

        /// <summary>早退。</summary>
        [Display(Name = "早退")]
        [CalendarColor("#FD7E14", "#FD7E14", "#212529")]
        EarlyLeave = 4
    }

    /// <summary>
    /// 通所時の活動種別。
    /// </summary>
    public enum ActivityType
    {
        /// <summary>プログラム。</summary>
        [Display(Name = "プログラム", Order = 1)]
        Program = 0,

        /// <summary>個別訓練。</summary>
        [Display(Name = "個別訓練", Order = 2)]
        [CalendarColor("#F4E5F8", "#8E44AD", "#4A235A")]
        IndividualTraining = 1,

        /// <summary>部署活動。</summary>
        [Display(Name = "部署活動", Order = 3)]
        [CalendarColor("#D8EAF7", "#2874A6", "#1B4F72")]
        DepartmentActivity = 2,

        /// <summary>実習。</summary>
        [Display(Name = "実習", Order = 4)]
        [CalendarColor("#FCE4D6", "#C65911", "#5A2600")]
        PracticalTraining = 7,

        /// <summary>外出。</summary>
        [Display(Name = "外出", Order = 5)]
        [CalendarColor("#D9EAD3", "#6AA84F", "#274E13")]
        GoOut = 3,

        /// <summary>面談。</summary>
        [Display(Name = "面談", Order = 6)]
        [CalendarColor("#FFF2CC", "#D6A000", "#5F4700")]
        Interview = 5,

        /// <summary>プライベート。</summary>
        [Display(Name = "プライベート", Order = 7)]
        [CalendarColor("#E7DEFA", "#8E6FC7", "#5A3D8A")]
        Private = 6,

        /// <summary>その他。</summary>
        [Display(Name = "その他", Order = 8)]
        [CalendarColor("#E7E6E6", "#A6A6A6", "#3C3C3C")]
        Other = 4
    }

    /// <summary>
    /// リタリコワークスのプログラム種別。
    /// </summary>
    public enum ProgramType
    {
        /// <summary>セルフワーク。</summary>
        [Display(Name = "自分らしく働く")]
        [CalendarColor("#F8CBAD", "#C55A11", "#4A2300")]
        SelfWork = 0,

        /// <summary>健康管理。</summary>
        [Display(Name = "ヘルスケア")]
        [CalendarColor("#E2F0D9", "#70AD47", "#254D1B")]
        HealthCare = 1,

        /// <summary>職場コミュニケーション。</summary>
        [Display(Name = "職場でのコミュニケーション")]
        [CalendarColor("#E4DFEC", "#8064A2", "#3D2A56")]
        WorkplaceCommunication = 2,

        /// <summary>就職活動。</summary>
        [Display(Name = "就職活動")]
        [CalendarColor("#DDEBF7", "#5B9BD5", "#1F4E79")]
        JobHunting = 3,

        /// <summary>応募・面接。</summary>
        [Display(Name = "応募・面接")]
        [CalendarColor("#F4CCCC", "#C00000", "#7F1D1D")]
        ApplicationInterview = 4,

        /// <summary>就労準備。</summary>
        [Display(Name = "就労前の準備")]
        [CalendarColor("#DDEBF7", "#2F75B5", "#1F4E79")]
        PreWorkPreparation = 5,

        /// <summary>その他自由入力。</summary>
        [Display(Name = "その他")]
        [CalendarColor("#FFFFFF", "#ADB5BD", "#343A40")]
        OtherFreeInput = 6
    }
}
