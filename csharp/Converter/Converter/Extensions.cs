using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Converter
{
    public static class Extensions
    {
        public static bool IsPascalCase(this SyntaxToken token)
        {
            var val = token.Text;
            if (string.IsNullOrEmpty(val))
                return false;
            return Regex.IsMatch(val, "^[A-Z]");
        }

        public static string ToPascalCase(this string val)
        {
            if (string.IsNullOrEmpty(val))
                return val;
            return val[0].ToString().ToUpper() + val.Remove(0, 1);
        }
        public static ValueTask<T> ToValueTask<T>(this Task<T> task)
        {
            return new ValueTask<T>(task);
        }
    }
}