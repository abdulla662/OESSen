using CSharpMath.SkiaSharp;
using SkiaSharp;
using System.Net;
using System.Text.RegularExpressions;

namespace OES.Services.Helpers
{
    public static class LatexImageRenderer
    {
        private const float DefaultFontSize = 20f;

        public static byte[]? RenderToPng(string latex, float fontSize = DefaultFontSize, SKColor? textColor = null)
        {
            if (string.IsNullOrWhiteSpace(latex))
            {
                return null;
            }

            try
            {
                latex = NormalizeLatex(latex);

                if (string.IsNullOrWhiteSpace(latex))
                {
                    return null;
                }

                var painter = new MathPainter
                {
                    LaTeX = latex,
                    FontSize = fontSize,
                    TextColor = textColor ?? SKColors.Black
                };

                if (!string.IsNullOrEmpty(painter.ErrorMessage))
                {
                    return null;
                }

                using var stream = painter.DrawAsStream(format: SKEncodedImageFormat.Png);

                if (stream is null || stream.Length == 0)
                {
                    return null;
                }

                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
            catch
            {
                return null;
            }
        }

        public static bool IsValidLatex(string latex)
        {
            if (string.IsNullOrWhiteSpace(latex))
            {
                return false;
            }

            try
            {
                latex = NormalizeLatex(latex);

                if (string.IsNullOrWhiteSpace(latex))
                {
                    return false;
                }

                var painter = new MathPainter
                {
                    LaTeX = latex,
                    FontSize = DefaultFontSize
                };

                if (!string.IsNullOrEmpty(painter.ErrorMessage))
                {
                    return false;
                }

                using var stream = painter.DrawAsStream(format: SKEncodedImageFormat.Png);

                return stream is not null && stream.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeLatex(string latex)
        {
            if (string.IsNullOrWhiteSpace(latex))
            {
                return latex;
            }

            latex = WebUtility.HtmlDecode(latex).Trim();

            latex = Regex.Replace(latex, @"'\s*~*\s*'?\s*prime\b", "''", RegexOptions.IgnoreCase);

            latex = latex.Replace("~", " ");

            latex = latex.Replace(@"\prime", "'");
            latex = latex.Replace(@"\stackrel", @"\overset");

            latex = Regex.Replace(
                latex,
                @"\\\\(?=(?:lim|frac|sqrt|int|sum|prod|to|infty|cdot|times|leq|geq|pm|alpha|beta|gamma|theta)\b)",
                @"\");

            latex = latex.Replace(@"\dfrac", @"\frac");
            latex = latex.Replace(@"\tfrac", @"\frac");

            return latex.Trim();
        }
    }
}