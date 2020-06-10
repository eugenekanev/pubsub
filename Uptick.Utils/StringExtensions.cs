namespace Uptick.Utils
{
    public static class StringExtensions
    {
        public static string Top(this string text, int size)
        {
            return string.IsNullOrEmpty(text) || text.Length <= size
                ? text
                : text.Substring(0, size);
        }

    }
}