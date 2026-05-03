using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Linq.Expressions;

namespace Dev.CommonLibrary.Extensions.Helper
{
    /// <summary>
    /// ラジオボタンリスト用 HTML ヘルパー拡張。
    /// </summary>
    public static class HtmlExtensionsForRadioButton
    {
        /// <summary>
        /// Enum 値をラジオボタン群として出力する。
        /// </summary>
        public static IHtmlContent RadioButtonForEnum<TModel, TProperty>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression,
            string prefix = "",
            string clickevent = "",
            bool orderDecFlag = false)
        {
            var names = Enum.GetNames(typeof(TProperty)).AsEnumerable();
            if (orderDecFlag) names = names.Reverse();

            var items = names.Select(name => new SelectListItem
            {
                Value = name,
                Text = EnumUtility.GetDescription(typeof(TProperty), name),
            });

            return htmlHelper.RadioButtonForSelectList(expression, items, prefix, clickevent);
        }

        /// <summary>
        /// SelectListItem をラジオボタン群として出力する。
        /// </summary>
        public static IHtmlContent RadioButtonForSelectList<TModel, TProperty>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression,
            IEnumerable<SelectListItem> selectList,
            string prefix = "",
            string clickevent = "")
        {
            var expressionText = GetExpressionText(expression);
            var fullName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(expressionText);
            var currentValue = expression.Compile().Invoke(htmlHelper.ViewData.Model)?.ToString();
            var builder = new HtmlContentBuilder();
            var index = 0;

            foreach (var item in selectList)
            {
                var id = $"{prefix}{fullName}_{index++}";
                var radio = new TagBuilder("input");
                radio.TagRenderMode = TagRenderMode.SelfClosing;
                radio.Attributes["type"] = "radio";
                radio.Attributes["name"] = fullName;
                radio.Attributes["id"] = id;
                radio.Attributes["value"] = item.Value;
                if (!string.IsNullOrEmpty(clickevent)) radio.Attributes["onclick"] = clickevent;
                if (string.Equals(currentValue, item.Value, StringComparison.OrdinalIgnoreCase) || item.Selected)
                {
                    radio.Attributes["checked"] = "checked";
                }

                var label = new TagBuilder("label");
                label.Attributes["for"] = id;
                label.InnerHtml.Append(item.Text);

                builder.AppendHtml(radio);
                builder.AppendHtml(label);
            }

            return builder;
        }

        private static string GetExpressionText(LambdaExpression expression)
        {
            var body = expression.Body is UnaryExpression unary ? unary.Operand : expression.Body;
            var names = new Stack<string>();

            while (body is MemberExpression member)
            {
                names.Push(member.Member.Name);
                body = member.Expression!;
            }

            return string.Join(".", names);
        }
    }
}
