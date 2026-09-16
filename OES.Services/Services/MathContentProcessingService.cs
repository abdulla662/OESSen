using Microsoft.Extensions.Logging;
using OES.Helper.Dtos.AIQuestionGenerator.Response;
using OES.Helper.General;
using OES.Helper.RegularExpressions;
using OES.Services.Helpers;
using SharedHelper.General;
using System.Net;
using System.Text.RegularExpressions;

namespace OES.Services.Services
{
    public class MathContentProcessingService
    {
        private readonly AIAssetStorage _aiAssetStorage;
        private readonly ILogger<MathContentProcessingService> _logger;

        public MathContentProcessingService(AIAssetStorage aiAssetStorage, ILogger<MathContentProcessingService> logger)
        {
            _aiAssetStorage = aiAssetStorage;
            _logger = logger;
        }

        public void ProcessMathContent(List<AIQuestionMetadataDto> questions, string languageCode)
        {
            if (questions == null || questions.Count == 0)
                return;

            foreach (var question in questions)
            {
                if (question.Details == null)
                {
                    continue;
                }

                foreach (var detail in question.Details)
                {
                    detail.Body = ProcessContent(detail.Body);

                    detail.Instructions = ProcessContent(detail.Instructions);

                    detail.ModelAnswer = ProcessContent(detail.ModelAnswer);

                    if (detail.Choices == null)
                    {
                        continue;
                    }

                    foreach (var choice in detail.Choices)
                    {
                        choice.Text = ProcessContent(choice.Text);
                    }
                }
            }
        }

        private string ProcessContent(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return content ?? string.Empty;

            content = ReplaceLatexPlaceholders(content);
            content = ReplaceDollarLatexFallback(content);
            content = ReplaceSimpleMath(content);

            return content;
        }

        private string ReplaceDollarLatexFallback(string content)
        {
            return Regex.Replace(
                content,
                RegularExpressions.DollarLatexFallbackPattern,
                match =>
                {
                    var latex = WebUtility.HtmlDecode(match.Groups["latex"].Value).Trim();
                    latex = ConvertLatexMarkers(latex);
                    latex = NormalizeLatex(latex);

                    var pngBytes = LatexImageRenderer.RenderToPng(latex);
                    if (pngBytes is null || pngBytes.Length == 0)
                    {
                        _logger.LogWarning("Failed to render fallback $-delimited LaTeX: {Latex}", latex);
                        return latex;
                    }

                    var assetId = _aiAssetStorage.Save(pngBytes, "image/png");
                    var encodedLatex = WebUtility.HtmlEncode(latex);
                    var src = $"{CentralizedUrlHelper.OesApiBaseUrl.TrimEnd('/')}/{MiscConstants.GetAIAsset}?assetId={assetId}";
                    var equationLanguageCode = Regex.IsMatch(latex, RegularExpressions.ArabicCharactersExpression) ? "ar" : "en";

                    return
                        $"<br><object data=\"{src}\" type=\"image/png\" class=\"equation-img\" " +
                        $"data-latex=\"{encodedLatex}\" data-lang=\"{equationLanguageCode}\" " +
                        $"contenteditable=\"false\" unselectable=\"on\" " +
                        $"style=\"display:inline-block;vertical-align:middle;height:45px;width:auto;\"></object><br>";
                },
                RegexOptions.Singleline);
        }

        private string ReplaceLatexPlaceholders(string content)
        {
            return Regex.Replace(
                content,
                RegularExpressions.LatexPlaceholderPattern,
                match =>
                {
                    var latex = WebUtility.HtmlDecode(match.Groups["latex"].Value).Trim();

                    latex = ConvertLatexMarkers(latex);
                    latex = NormalizeLatex(latex);

                    var pngBytes = LatexImageRenderer.RenderToPng(latex);

                    if (pngBytes is null || pngBytes.Length == 0)
                    {
                        _logger.LogWarning("Failed to render LaTeX expression: {Latex}", latex);

                        return match.Value;
                    }

                    var assetId = _aiAssetStorage.Save(pngBytes, "image/png");

                    var encodedLatex = WebUtility.HtmlEncode(latex);

                    var src = $"{CentralizedUrlHelper.OesApiBaseUrl.TrimEnd('/')}/" + $"{MiscConstants.GetAIAsset}?assetId={assetId}";

                    var equationLanguageCode = Regex.IsMatch(latex, RegularExpressions.ArabicCharactersExpression) ? "ar" : "en";

                    return
                        $"<br><object " +
                        $"data=\"{src}\" " +
                        $"type=\"image/png\" " +
                        $"class=\"equation-img\" " +
                        $"data-latex=\"{encodedLatex}\" " +
                        $"data-lang=\"{equationLanguageCode}\" " +
                        $"contenteditable=\"false\" " +
                        $"unselectable=\"on\" " +
                        $"style=\"display:inline-block;" +
                        $"vertical-align:middle;" +
                        $"height:45px;" +
                        $"width:auto;\">" +
                        $"</object><br>";
                },
                RegexOptions.Singleline);
        }

        private static string ConvertLatexMarkers(string latex)
        {
            return Regex.Replace(latex, @"~(?=[A-Za-z])", "\\");
        }

        private static string NormalizeLatex(string latex)
        {
            return latex.Replace(@"\prime", "'").Trim();
        }

        private static string ReplaceSimpleMath(string content)
        {
            var parts = Regex.Split(content, @"(<[^>]+>)", RegexOptions.Singleline);

            for (var i = 0; i < parts.Length; i++)
            {
                if (i % 2 != 0)
                    continue;

                parts[i] = NormalizeChemicalSubscripts(parts[i]);
                parts[i] = ConvertSuperscripts(parts[i]);
                parts[i] = ConvertSubscripts(parts[i]);
            }

            return string.Concat(parts);
        }

        private static string NormalizeChemicalSubscripts(string text)
        {
            if (string.IsNullOrEmpty(text) || !Regex.IsMatch(text, RegularExpressions.ChemicalEquationHintPattern))
            {
                return text;
            }

            return Regex.Replace(text, RegularExpressions.ChemicalElementDigitPattern, m => $"{m.Groups["elem"].Value}_{m.Groups["digit"].Value}");
        }

        private static string ConvertSuperscripts(string text)
        {
            return Regex.Replace(text, RegularExpressions.SimpleMathSuperscriptPattern, match => $"{match.Groups["base"].Value}<sup>{GetMatchedValue(match)}</sup>");
        }

        private static string ConvertSubscripts(string text)
        {
            return Regex.Replace(text, RegularExpressions.SimpleMathSubscriptPattern, match => $"{match.Groups["base"].Value}<sub>{GetMatchedValue(match)}</sub>");
        }

        private static string GetMatchedValue(Match match)
        {
            if (match.Groups["value"].Success)
            {
                return match.Groups["value"].Value;
            }

            if (match.Groups["valueBraced"].Success)
            {
                return match.Groups["valueBraced"].Value;
            }

            if (match.Groups["valueSimple"].Success)
            {
                return match.Groups["valueSimple"].Value;
            }

            return string.Empty;
        }
    }
}