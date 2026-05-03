using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Linq.Expressions;
using System.Text.Encodings.Web;

namespace Dev.CommonLibrary.Extensions.Helper
{
    /// <summary>
    /// チェックボックスリスト用 HTML ヘルパー拡張。
    /// </summary>
    public static class HtmlExtensionsForCheckBox
    {
        /// <summary>
        /// List&lt;string&gt; プロパティに対するチェックボックス群を出力する。
        /// </summary>
        public static IHtmlContent CheckBoxForSelectList<TModel, TProperty>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression,
            IEnumerable<SelectListItem> selectList,
            object? htmlAttributes = null)
            where TProperty : List<string>
        {
            var expressionText = GetExpressionText(expression);
            var fullName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(expressionText);
            var selectedValues = expression.Compile().Invoke(htmlHelper.ViewData.Model) ?? new List<string>();
            var builder = new HtmlContentBuilder();
            var index = 0;

            foreach (var item in selectList)
            {
                builder.AppendHtml(CheckBoxForValue(fullName, selectedValues, item, index++, htmlAttributes));
            }

            return builder;
        }

        /// <summary>
        /// List&lt;string&gt; プロパティに対する単一チェックボックスを出力する。
        /// </summary>
        public static IHtmlContent CheckBoxForValue<TModel, TProperty>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression,
            SelectListItem item,
            int index,
            object? htmlAttributes = null)
            where TProperty : List<string>
        {
            var expressionText = GetExpressionText(expression);
            var fullName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(expressionText);
            var selectedValues = expression.Compile().Invoke(htmlHelper.ViewData.Model) ?? new List<string>();
            return CheckBoxForValue(fullName, selectedValues, item, index, htmlAttributes);
        }

        private static IHtmlContent CheckBoxForValue(
            string fullName,
            IReadOnlyCollection<string> selectedValues,
            SelectListItem item,
            int index,
            object? htmlAttributes)
        {
            var id = $"{fullName}_{index}";
            var checkbox = new TagBuilder("input");
            checkbox.TagRenderMode = TagRenderMode.SelfClosing;
            checkbox.Attributes["type"] = "checkbox";
            checkbox.Attributes["name"] = fullName;
            checkbox.Attributes["id"] = id;
            checkbox.Attributes["value"] = item.Value;
            if (selectedValues.Contains(item.Value) || item.Selected)
            {
                checkbox.Attributes["checked"] = "checked";
            }
            CreateHtmlAttribute(checkbox, htmlAttributes);

            var hidden = new TagBuilder("input");
            hidden.TagRenderMode = TagRenderMode.SelfClosing;
            hidden.Attributes["type"] = "hidden";
            hidden.Attributes["name"] = fullName;
            hidden.Attributes["value"] = "";

            var label = new TagBuilder("label");
            label.Attributes["for"] = id;
            label.InnerHtml.Append(item.Text);

            var builder = new HtmlContentBuilder();
            builder.AppendHtml(checkbox);
            builder.AppendHtml(hidden);
            builder.AppendHtml(label);
            return builder;
        }

        private static void CreateHtmlAttribute(TagBuilder tagBuilder, object? htmlAttributes)
        {
            if (htmlAttributes == null) return;

            foreach (var property in htmlAttributes.GetType().GetProperties())
            {
                var key = property.Name.Replace("_", "-");
                var value = property.GetValue(htmlAttributes)?.ToString();
                if (!string.IsNullOrEmpty(value)) tagBuilder.Attributes[key] = HtmlEncoder.Default.Encode(value);
            }
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
