using System.Text.RegularExpressions;

namespace OES.Helper.General
{
    public static class ArabicTextHelper
    {
        private static readonly Regex ArabicRunRegex = new(
            $"{RegularExpressions.RegularExpressions.ArabicCharactersExpression}\\s+",
            RegexOptions.Compiled
        );

        public static string FixArabicDirection(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            return ArabicRunRegex.Replace(text, m =>
            {
                var run = m.Value;

                if (!run.Any(IsArabicChar))
                    return run;

                var chars = run.ToCharArray();

                Array.Reverse(chars);

                return new string(chars);
            });
        }

        private static bool IsArabicChar(char c) =>
            (c >= '\u0600' && c <= '\u06FF') ||
            (c >= '\u0750' && c <= '\u077F') ||
            (c >= '\u08A0' && c <= '\u08FF') ||
            (c >= '\uFB50' && c <= '\uFDFF') ||
            (c >= '\uFE70' && c <= '\uFEFF');
    }
}
