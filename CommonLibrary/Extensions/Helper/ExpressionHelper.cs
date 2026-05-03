using System.Linq.Expressions;

namespace Dev.CommonLibrary.Extensions.Helper
{
    /// <summary>
    /// ラムダ式からフィールド名を取り出すユーティリティ。
    /// </summary>
    internal static class ExpressionHelper
    {
        /// <summary>
        /// ラムダ式のメンバーパスを "A.B.C" 形式の文字列で返す。
        /// </summary>
        internal static string GetExpressionText(LambdaExpression expression)
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
