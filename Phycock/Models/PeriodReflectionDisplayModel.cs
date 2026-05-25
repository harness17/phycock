using Phycock.Models;

namespace Phycock.Views.Statistics
{
    /// <summary>
    /// `_PeriodReflectionDisplay.cshtml` 用モデル。所感本体に加えて、
    /// 表示タイトル（「週次の所感」等）と追加CSSクラスを指定する。
    /// </summary>
    public class PeriodReflectionDisplayModel
    {
        /// <summary>セクション見出し文字列。</summary>
        public string Title { get; set; } = "";

        /// <summary>所感本体。</summary>
        public PeriodReflectionViewModel Vm { get; set; } = new();

        /// <summary>セクション外側に追加するクラス（PDF用 page-break-before 等）。</summary>
        public string ExtraClass { get; set; } = "";
    }
}
