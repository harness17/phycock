using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Phycock.Common
{
    /// <summary>
    /// Enum 表示名の拡張メソッド。
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// DisplayAttribute.Name があれば返し、未設定なら enum 名を返す。
        /// </summary>
        public static string GetDisplayName(this Enum value)
        {
            var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
            var display = member?.GetCustomAttribute<DisplayAttribute>();
            return display?.Name ?? value.ToString();
        }
    }
}
