namespace Phycock.Common
{
    /// <summary>
    /// 睡眠時間の評価基準。
    /// </summary>
    public static class SleepStandards
    {
        public const double AdequateLowerHours = 7;
        public const double AdequateUpperHours = 9;

        public static SleepLevel Classify(double totalHours)
        {
            if (totalHours <= 0) return SleepLevel.None;
            if (totalHours < 6) return SleepLevel.Insufficient;
            if (totalHours < AdequateLowerHours) return SleepLevel.SlightlyShort;
            if (totalHours <= AdequateUpperHours) return SleepLevel.Adequate;
            return SleepLevel.Excessive;
        }
    }

    public enum SleepLevel
    {
        None,
        Insufficient,
        SlightlyShort,
        Adequate,
        Excessive
    }
}
