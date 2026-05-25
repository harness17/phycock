using Phycock.Entity.Enums;
using System.ComponentModel.DataAnnotations;

namespace Phycock.Models
{
    /// <summary>
    /// 期間所感の表示・編集 ViewModel。
    /// </summary>
    public class PeriodReflectionViewModel
    {
        /// <summary>レコードID。新規時は 0。</summary>
        public long Id { get; set; }

        /// <summary>対象ユーザーID。サーバー側で解決。</summary>
        public string UserId { get; set; } = "";

        /// <summary>期間種別。</summary>
        public PeriodType PeriodType { get; set; }

        /// <summary>期間開始日（週次=日曜、月次=月初）。</summary>
        public DateTime PeriodStart { get; set; }

        [Display(Name = "自己評価")]
        [StringLength(1000, ErrorMessage = "自己評価は1000文字以内で入力してください")]
        public string? SelfEvaluation { get; set; }

        [Display(Name = "負担")]
        [StringLength(1000, ErrorMessage = "負担は1000文字以内で入力してください")]
        public string? Burden { get; set; }

        [Display(Name = "改善案")]
        [StringLength(1000, ErrorMessage = "改善案は1000文字以内で入力してください")]
        public string? Improvement { get; set; }

        [Display(Name = "食欲")]
        [StringLength(1000, ErrorMessage = "食欲は1000文字以内で入力してください")]
        public string? Appetite { get; set; }

        [Display(Name = "睡眠")]
        [StringLength(1000, ErrorMessage = "睡眠は1000文字以内で入力してください")]
        public string? Sleep { get; set; }

        /// <summary>5項目すべてが空かどうか。</summary>
        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(SelfEvaluation)
            && string.IsNullOrWhiteSpace(Burden)
            && string.IsNullOrWhiteSpace(Improvement)
            && string.IsNullOrWhiteSpace(Appetite)
            && string.IsNullOrWhiteSpace(Sleep);
    }
}
