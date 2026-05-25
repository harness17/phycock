namespace Phycock.Entity.Enums
{
    /// <summary>所感の集計期間種別。</summary>
    public enum PeriodType
    {
        /// <summary>週次（PeriodStart は週開始日＝日曜）。</summary>
        Weekly = 0,
        /// <summary>月次（PeriodStart は月初 yyyy-MM-01）。</summary>
        Monthly = 1
    }
}
