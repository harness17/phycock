using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Linq.Expressions;
using System.Text.Encodings.Web;

namespace Dev.CommonLibrary.Extensions.Helper
{
    /// <summary>
    /// ASP.NET Core MVC 用の HTML ヘルパー拡張。
    /// </summary>
    public static class HtmlExtensions
    {
        /// <summary>
        /// 改行を br に変換して HTML として返す。
        /// </summary>
        public static IHtmlContent FormatNewLines(this IHtmlHelper helper, string? text)
        {
            if (string.IsNullOrEmpty(text)) return HtmlString.Empty;

            var encoded = HtmlEncoder.Default.Encode(text)
                .Replace("\r\n", "<br />")
                .Replace("\n", "<br />");
            return new HtmlString(encoded);
        }

        /// <summary>
        /// Enum の DescriptionAttribute 表示名を label として出力する。
        /// </summary>
        public static IHtmlContent DisplayForEnum<TModel, TProperty>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression)
        {
            var value = expression.Compile().Invoke(htmlHelper.ViewData.Model);
            if (value == null) return HtmlString.Empty;

            var text = value.GetType().IsEnum
                ? EnumUtility.GetEnumDisplay(value.GetType(), value.ToString()!)
                : value.ToString() ?? "";

            var label = new TagBuilder("label");
            label.InnerHtml.Append(text);
            return label;
        }

        /// <summary>
        /// 指定プロパティをフィールドプレフィックスにして Partial を描画する。
        /// </summary>
        public static IHtmlContent PartialFor<TModel, TProperty>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression,
            string partialViewName)
        {
            var prefix = ExpressionHelper.GetExpressionText(expression);
            var model = expression.Compile().Invoke(htmlHelper.ViewData.Model);
            return PartialFor(htmlHelper, prefix, partialViewName, model);
        }

        /// <summary>
        /// 指定プレフィックスを使って Partial を描画する。
        /// </summary>
        public static IHtmlContent PartialFor<TModel>(
            this IHtmlHelper<TModel> htmlHelper,
            string prefix,
            string partialViewName)
        {
            return PartialFor(htmlHelper, prefix, partialViewName, htmlHelper.ViewData.Model);
        }

        private static IHtmlContent PartialFor<TModel>(
            IHtmlHelper<TModel> htmlHelper,
            string prefix,
            string partialViewName,
            object? model)
        {
            var viewData = new ViewDataDictionary(htmlHelper.ViewData);
            viewData.TemplateInfo.HtmlFieldPrefix = string.IsNullOrWhiteSpace(htmlHelper.ViewData.TemplateInfo.HtmlFieldPrefix)
                ? prefix
                : $"{htmlHelper.ViewData.TemplateInfo.HtmlFieldPrefix}.{prefix}";

            // ASP.NET Core はデフォルトで SynchronizationContext を持たないため
            // .GetAwaiter().GetResult() によるデッドロックは発生しない。
            // IHtmlContent 戻り値の制約上 async 化不可。
            return htmlHelper.PartialAsync(partialViewName, model, viewData).GetAwaiter().GetResult();
        }

    }
}
