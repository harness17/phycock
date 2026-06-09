namespace Phycock.Models
{
    /// <summary>ヒートマップ日別データ。</summary>
    public class HeatmapDayDto
    {
        /// <summary>日付（yyyy-MM-dd 形式）。</summary>
        public string Date { get; set; } = "";

        /// <summary>体調レベル（ConditionLevel の int 値）。その日の最低値。</summary>
        public int Level { get; set; }
    }
}
