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
        Planned = 0,

        /// <summary>通所済み。</summary>
        [Display(Name = "通所済み")]
        Attended = 1,

        /// <summary>欠席。</summary>
        [Display(Name = "欠席")]
        Absent = 2,

        /// <summary>遅刻。</summary>
        [Display(Name = "遅刻")]
        Late = 3,

        /// <summary>早退。</summary>
        [Display(Name = "早退")]
        EarlyLeave = 4
    }

    /// <summary>
    /// 通所時の活動種別。
    /// </summary>
    public enum ActivityType
    {
        /// <summary>プログラム。</summary>
        [Display(Name = "プログラム")]
        Program = 0,

        /// <summary>個別訓練。</summary>
        [Display(Name = "個別訓練")]
        IndividualTraining = 1,

        /// <summary>部署活動。</summary>
        [Display(Name = "部署活動")]
        DepartmentActivity = 2,

        /// <summary>外出。</summary>
        [Display(Name = "外出")]
        GoOut = 3,

        /// <summary>その他。</summary>
        [Display(Name = "その他")]
        Other = 4
    }

    /// <summary>
    /// リタリコワークスのプログラム種別。
    /// </summary>
    public enum ProgramType
    {
        /// <summary>セルフワーク。</summary>
        [Display(Name = "自分らしく働く")]
        SelfWork = 0,

        /// <summary>健康管理。</summary>
        [Display(Name = "ヘルスケア")]
        HealthCare = 1,

        /// <summary>職場コミュニケーション。</summary>
        [Display(Name = "職場でのコミュニケーション")]
        WorkplaceCommunication = 2,

        /// <summary>就職活動。</summary>
        [Display(Name = "就職活動")]
        JobHunting = 3,

        /// <summary>応募・面接。</summary>
        [Display(Name = "応募・面接")]
        ApplicationInterview = 4,

        /// <summary>就労準備。</summary>
        [Display(Name = "就労前の準備")]
        PreWorkPreparation = 5,

        /// <summary>その他自由入力。</summary>
        [Display(Name = "その他")]
        OtherFreeInput = 6
    }
}
