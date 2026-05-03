using Phycock.Entity.Enums;
using System.ComponentModel.DataAnnotations;

namespace Phycock.Models
{
    /// <summary>
    /// 体調記録の作成・編集フォーム ViewModel。
    /// </summary>
    public class HealthRecordFormViewModel
    {
        /// <summary>体調記録 ID。</summary>
        public long Id { get; set; }

        /// <summary>記録対象ユーザー ID。</summary>
        public string UserId { get; set; } = "";

        /// <summary>記録日。</summary>
        [Required(ErrorMessage = "記録日は必須です")]
        [DataType(DataType.Date)]
        [Display(Name = "記録日")]
        public DateTime RecordDate { get; set; } = DateTime.Today;

        /// <summary>記録タイミング。</summary>
        [Display(Name = "記録タイミング")]
        public RecordTiming RecordTiming { get; set; } = RecordTiming.Morning;

        /// <summary>選択された症状。</summary>
        [Display(Name = "症状")]
        public List<SymptomType> SelectedSymptoms { get; set; } = new();

        /// <summary>身体的な体調レベル。</summary>
        [Range(1, 5, ErrorMessage = "体調は1〜5の範囲で選択してください")]
        [Display(Name = "体調")]
        public ConditionLevel Condition { get; set; } = ConditionLevel.Normal;

        /// <summary>気分のレベル。</summary>
        [Range(1, 5, ErrorMessage = "気分は1〜5の範囲で選択してください")]
        [Display(Name = "気分")]
        public FeelingLevel Feeling { get; set; } = FeelingLevel.Normal;

        /// <summary>自由記入メモ。</summary>
        [MaxLength(1000, ErrorMessage = "メモは1000文字以内で入力してください")]
        [Display(Name = "メモ")]
        public string? Memo { get; set; }
    }

    /// <summary>
    /// 体調記録の一覧表示 ViewModel。
    /// </summary>
    public class HealthRecordListViewModel
    {
        /// <summary>体調記録 ID。</summary>
        public long Id { get; set; }

        /// <summary>記録対象ユーザー ID。</summary>
        public string UserId { get; set; } = "";

        /// <summary>記録日。</summary>
        public DateTime RecordDate { get; set; }

        /// <summary>記録タイミング。</summary>
        public RecordTiming RecordTiming { get; set; }

        /// <summary>症状表示文字列。</summary>
        public string Symptoms { get; set; } = "";

        /// <summary>症状のビットフラグ。</summary>
        public long SymptomFlags { get; set; }

        /// <summary>身体的な体調レベル。</summary>
        public ConditionLevel Condition { get; set; }

        /// <summary>気分のレベル。</summary>
        public FeelingLevel Feeling { get; set; }

        /// <summary>自由記入メモ。</summary>
        public string? Memo { get; set; }
    }

    /// <summary>
    /// 体調記録の Ajax 詳細 DTO。
    /// </summary>
    public class HealthRecordJsonDto
    {
        /// <summary>体調記録 ID。</summary>
        public long Id { get; set; }

        /// <summary>記録日。</summary>
        public string RecordDate { get; set; } = "";

        /// <summary>記録タイミング。</summary>
        public string RecordTiming { get; set; } = "";

        /// <summary>症状一覧。</summary>
        public List<string> Symptoms { get; set; } = new();

        /// <summary>身体的な体調レベル。</summary>
        public string Condition { get; set; } = "";

        /// <summary>気分のレベル。</summary>
        public string Feeling { get; set; } = "";

        /// <summary>自由記入メモ。</summary>
        public string? Memo { get; set; }
    }
}
