using System.Net;
using System.Text.RegularExpressions;
using RegexPatterns = OES.Helper.RegularExpressions.RegularExpressions;

namespace OES.Helper.General
{
    public static class HtmlTagsCleaner
    {
        public static string Clean(string htmlText)
        {
            if (string.IsNullOrWhiteSpace(htmlText))
            {
                return string.Empty;
            }

            string processed = Regex.Replace(htmlText, RegexPatterns.BlockTagsPattern, "\n", RegexOptions.IgnoreCase);

            processed = Regex.Replace(processed, RegexPatterns.MediaWithContentPattern, string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            processed = Regex.Replace(processed, RegexPatterns.AllTagsPattern, string.Empty);

            processed = WebUtility.HtmlDecode(processed);

            processed = processed
                .Replace("\u200B", "")
                .Replace("\u200C", "")
                .Replace("\u200D", "")
                .Replace("\uFEFF", "")
                .Replace("\u00A0", " ");

            return processed.Trim();
        }
    }
}
