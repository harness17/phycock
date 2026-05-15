namespace Phycock.Entity.Enums
{
    /// <summary>
    /// カレンダー表示色を enum 値に持たせるための属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class CalendarColorAttribute : Attribute
    {
        public CalendarColorAttribute(string backgroundColor, string borderColor, string textColor)
        {
            BackgroundColor = backgroundColor;
            BorderColor = borderColor;
            TextColor = textColor;
        }

        public string BackgroundColor { get; }

        public string BorderColor { get; }

        public string TextColor { get; }
    }
}
