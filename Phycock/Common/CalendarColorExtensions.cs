using System.Reflection;
using Phycock.Entity.Enums;

namespace Phycock.Common
{
    /// <summary>
    /// enum メンバーに付与された <see cref="CalendarColorAttribute"/> を読み取るヘルパー。
    /// カレンダー・統計など複数画面で同じ色定義を参照するための一元化用。
    /// </summary>
    public static class CalendarColorExtensions
    {
        private static readonly CalendarColorValue Default = new("#E7E6E6", "#A6A6A6", "#3C3C3C");

        /// <summary>enum 値の CalendarColor 属性を取得する。未設定時は既定色を返す。</summary>
        public static CalendarColorValue GetCalendarColor(this Enum value)
        {
            var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
            var attr = member?.GetCustomAttribute<CalendarColorAttribute>();
            return attr is null
                ? Default
                : new CalendarColorValue(attr.BackgroundColor, attr.BorderColor, attr.TextColor);
        }
    }

    /// <summary>カレンダー表示色（背景・枠・文字）。</summary>
    public sealed record CalendarColorValue(string Background, string Border, string Text);
}
