using System;
using System.Text;

namespace Uptick.Utils
{
    public static class StringExtentions
    {
        public static string ReplaceAt(this string input, int index, char newChar)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            var builder = new StringBuilder(input) {[index] = newChar};
            return builder.ToString();
        }

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
    }
}