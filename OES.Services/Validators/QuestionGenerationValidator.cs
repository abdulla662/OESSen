using OES.Helper.Dtos.AIQuestionGenerator.Request;
using OES.Helper.Dtos.AIQuestionGenerator.Response;
using OES.Helper.Dtos.Validation;
using OES.Helper.Enums;
using OES.Interface.Interfaces;
using System.Text;
using System.Text.RegularExpressions;

namespace OES.Services.Validators
{
    public class QuestionGenerationValidator : IAIResponseResultValidator<AIGeneratedQuestionsResultDto, AIQuestionGenerationPromptContextDto>
    {
        private static readonly char[] SuspiciousControlChars = ['\b', '\t', '\f'];

        private static readonly Dictionary<char, char> RecoverableControlCharLetters = new()
        {
            ['\b'] = 'b',
            ['\t'] = 't',
            ['\f'] = 'f'
        };

        public AIResponseValidationResult Validate(AIGeneratedQuestionsResultDto result, AIQuestionGenerationPromptContextDto context)
        {
            var errors = new List<string>();

            if (result?.Questions is null || result.Questions.Count == 0)
            {
                return AIResponseValidationResult.Fail("No questions were returned.");
            }

            var requestedTypeIds = context.QuestionTypes.Select(d => d.QuestionTypeId).ToHashSet();

            foreach (var dist in context.QuestionTypes)
            {
                var actualCount = result.Questions.Count(q => q.QuestionTypeId == dist.QuestionTypeId);

                if (actualCount > dist.Count)
                {
                    errors.Add($"Expected at most {dist.Count} \"{dist.QuestionTypeName}\" question(s) but got {actualCount}.");
                }
            }

            var unrequested = result.Questions
                .Where(q => !requestedTypeIds.Contains(q.QuestionTypeId))
                .Select(q => q.QuestionTypeId)
                .Distinct()
                .ToList();

            foreach (var typeId in unrequested)
            {
                errors.Add($"Returned questions of type {typeId}, which was not part of the requested distribution.");
            }

            var uniqueQuestionBodies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var imageQuestionCount = 0;
            var candidateImageIds = context.ImageCandidates?.Select(i => i.DocumentId.ToString()).ToHashSet() ?? [];

            foreach (var q in result.Questions)
            {
                var label = string.IsNullOrWhiteSpace(q.Code) ? "(no code)" : q.Code;
                var detail = q.Details?.FirstOrDefault();

                if (detail is null)
                {
                    errors.Add($"[{label}] is missing its details object.");
                    continue;
                }

                detail.Body = RepairLatexMarkers(detail.Body);
                detail.Instructions = RepairLatexMarkers(detail.Instructions);
                detail.ModelAnswer = RepairLatexMarkers(detail.ModelAnswer);

                var choices = detail.Choices ?? [];

                foreach (var choice in choices)
                {
                    choice.Text = RepairLatexMarkers(choice.Text);
                }

                switch ((QuestionType)q.QuestionTypeId)
                {
                    case QuestionType.MCQ:
                    case QuestionType.TrueAndFalse:
                        if (!string.IsNullOrWhiteSpace(detail.Body) && choices.Count > 0)
                        {
                            var choiceTexts = choices
                                .Select(c => c.Text)
                                .Where(t => !string.IsNullOrWhiteSpace(t))
                                .Select(t => t!.Trim());

                            var stripped = StripLeakedChoiceText(detail.Body, choiceTexts);

                            if ((QuestionType)q.QuestionTypeId == QuestionType.TrueAndFalse)
                            {
                                stripped = StripTrueFalsePhrase(stripped);
                            }

                            if (stripped != detail.Body)
                            {
                                detail.Body = stripped;
                            }
                        }
                        break;
                }

                if (string.IsNullOrWhiteSpace(detail.Body))
                {
                    errors.Add($"[{label}] has an empty question body.");
                }
                else if (detail.Body.Trim().Length < 10)
                {
                    errors.Add($"[{label}] body is too short/became too short after removing leaked choice or answer text — rewrite it as a complete, standalone question with no options embedded.");
                }
                else if (!uniqueQuestionBodies.Add(detail.Body.Trim()))
                {
                    errors.Add($"[{label}] has an exact-duplicate body of another question in the set.");
                }

                ValidateMathAndChemistryNotation(label, "body", detail.Body, errors);
                ValidateMathAndChemistryNotation(label, "modelAnswer", detail.ModelAnswer, errors);
                foreach (var choice in choices)
                {
                    ValidateMathAndChemistryNotation(label, "choice text", choice.Text, errors);
                }

                if (detail.Body?.Contains("<img", StringComparison.OrdinalIgnoreCase) == true)
                {
                    detail.Body = NormalizeImageTag(detail.Body);
                    imageQuestionCount++;

                    var containsKnownImage = candidateImageIds.Count == 0 || candidateImageIds.Any(id => detail.Body.Contains(id, StringComparison.OrdinalIgnoreCase));

                    if (!containsKnownImage)
                    {
                        errors.Add($"[{label}] embeds an <img> tag whose assetId doesn't match any supplied image candidate.");
                    }

                    var textWithoutImage = Regex.Replace(
                        detail.Body,
                        @"<img\b[^>]*>",
                        "",
                        RegexOptions.IgnoreCase
                    );

                    var meaningfulTextLength = textWithoutImage
                        .Replace("&nbsp;", "")
                        .Where(c => !char.IsWhiteSpace(c) && !char.IsPunctuation(c))
                        .Count();

                    if (meaningfulTextLength < 10)
                    {
                        errors.Add(
                            $"[{label}] body contains an <img> tag but no substantial question text. " +
                            "The body must contain a real question in addition to the image.");
                    }
                }

                switch ((QuestionType)q.QuestionTypeId)
                {
                    case QuestionType.MCQ:
                        if (choices.Count != 4)
                        {
                            errors.Add($"[{label}] MCQ must have exactly 4 choices, found {choices.Count}.");
                        }
                        if (choices.Count(c => c.IsCorrect) != 1)
                        {
                            errors.Add($"[{label}] MCQ must have exactly 1 correct choice.");
                        }
                        if (choices.Any(c => string.IsNullOrWhiteSpace(c.Text)))
                        {
                            errors.Add($"[{label}] MCQ has one or more empty choice texts.");
                        }
                        break;

                    case QuestionType.TrueAndFalse:
                        if (choices.Count != 2)
                        {
                            errors.Add($"[{label}] True/False must have exactly 2 choices, found {choices.Count}.");
                        }
                        if (choices.Count(c => c.IsCorrect) != 1)
                        {
                            errors.Add($"[{label}] True/False must have exactly 1 correct choice.");
                        }
                        if (choices.Any(c => string.IsNullOrWhiteSpace(c.Text)))
                        {
                            errors.Add($"[{label}] True/False has one or more empty choice texts.");
                        }
                        if (choices.Count == 2 &&
                            string.Equals(choices[0].Text?.Trim(), choices[1].Text?.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            errors.Add($"[{label}] True/False choices must have two distinct texts (not identical).");
                        }
                        break;

                    case QuestionType.Essay:
                        if (choices.Count != 0)
                        {
                            errors.Add($"[{label}] Essay must have an empty choices array.");
                        }
                        if (string.IsNullOrWhiteSpace(detail.ModelAnswer))
                        {
                            errors.Add($"[{label}] Essay is missing a model answer.");
                        }
                        else if (!string.IsNullOrWhiteSpace(detail.Body) &&
                                 detail.ModelAnswer.Trim().Length > 15 &&
                                 detail.Body.Contains(detail.ModelAnswer.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            errors.Add($"[{label}] body appears to contain the model answer verbatim — the answer belongs only in modelAnswer. Rewrite the body as an unanswered question.");
                        }
                        break;
                }

                if (context.DifficultyLevels is { Count: > 0 })
                {
                    var level = context.DifficultyLevels.FirstOrDefault(d => d.Id == q.DifficultyLevelId);
                    if (level is null)
                    {
                        errors.Add($"[{label}] difficultyLevelId {q.DifficultyLevelId} is not an allowed level.");
                    }
                    else if (q.Delta < level.FromDelta || q.Delta > level.ToDelta)
                    {
                        errors.Add($"[{label}] delta {q.Delta:F2} is outside the \"{level.Name}\" range ({level.FromDelta:F2}-{level.ToDelta:F2}).");
                    }
                }
            }

            if (candidateImageIds.Count > 0 && imageQuestionCount == 0)
            {
                errors.Add(
                    $"No question used any of the {candidateImageIds.Count} available image(s), but at least one image-based question is required. " +
                    "Pick one qualifying IMAGE_ID from the AVAILABLE IMAGES list, write a complete question sentence about what it shows, " +
                    "and embed it as the last element of that question's body using the exact <img src=\"...?assetId={IMAGE_ID}\" /> format " +
                    "described in the instructions. Only leave zero image questions if every attached image is genuinely a logo/icon/watermark " +
                    "with no substantive content — if that's not the case here, add the image question now.");
            }

            return errors.Count == 0 ? AIResponseValidationResult.Success() : AIResponseValidationResult.Fail(errors);
        }

        private static string RepairLatexMarkers(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text ?? string.Empty;
            }

            var sb = new StringBuilder(text.Length);

            foreach (var ch in text)
            {
                if (RecoverableControlCharLetters.TryGetValue(ch, out var letter))
                {
                    sb.Append('~').Append(letter);
                }
                else if (ch == '\\')
                {
                    sb.Append('~');
                }
                else
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }

        private static void ValidateMathAndChemistryNotation(string label, string fieldName, string? text, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            if (text.IndexOfAny(SuspiciousControlChars) >= 0 || text.Contains('\\'))
            {
                errors.Add(
                    $"[{label}] {fieldName} still contains a raw backslash/control character after repair. " +
                    "This means the corruption pattern was not one of the automatically recoverable cases. " +
                    "Regenerate this field using ~ instead of \\ for every LaTeX command.");
            }

            var withoutLatexBlocks = Regex.Replace(text, @"\[Latex\]\[.*?\]", "", RegexOptions.Singleline);
            var strayMarkerMatches = Regex.Matches(withoutLatexBlocks, @"~[A-Za-z]+");
            if (strayMarkerMatches.Count > 0)
            {
                var examples = string.Join(", ", strayMarkerMatches.Select(m => m.Value).Distinct().Take(5));
                errors.Add(
                    $"[{label}] {fieldName} contains LaTeX marker command(s) ({examples}) outside an [Latex][...] wrapper. " +
                    "Wrap the full expression as [Latex][...] or rewrite it without LaTeX commands.");
            }

            var hyphenMatches = Regex.Matches(text, @"(?<=[A-Z])-(?=\d)");
            if (hyphenMatches.Count > 0)
            {
                errors.Add(
                    $"[{label}] {fieldName} contains a hyphen directly between an element letter and a digit (e.g. \"SO-4\"), which is not " +
                    "valid chemical notation. The digit is a subscript and must use _ instead (e.g. SO_4), even if an earlier subscript in " +
                    "the same formula already used a correct underscore.");
            }

            var exponentMatches = Regex.Matches(text, @"(?<![\^_A-Za-z0-9])[a-z](\d)\b");
            if (exponentMatches.Count > 0)
            {
                var examples = string.Join(", ", exponentMatches.Select(m => m.Value).Distinct().Take(5));
                errors.Add(
                    $"[{label}] {fieldName} contains an ambiguous bare exponent ({examples}) with no ^ marker. Rewrite as e.g. x^2.");
            }
        }

        private static string StripLeakedChoiceText(string body, IEnumerable<string> choiceTexts)
        {
            var cleaned = body;

            foreach (var text in choiceTexts)
            {
                if (text.Length <= 2) continue;

                var labeledPattern = $@"[\(\[]?[A-Da-d1-4][\)\.\]]?\s*{Regex.Escape(text)}";

                cleaned = Regex.Replace(cleaned, labeledPattern, "", RegexOptions.IgnoreCase);

                cleaned = Regex.Replace(cleaned, Regex.Escape(text), "", RegexOptions.IgnoreCase);
            }

            cleaned = Regex.Replace(
                cleaned,
                @"[\(\[]?[A-Da-d1-4][\)\.\]](?=\s*([\(\[]?[A-Da-d1-4][\)\.\]]|[\r\n]|$))",
                ""
            );

            return CollapseWhitespaceAndTrailingPunctuation(cleaned);
        }

        private static string StripTrueFalsePhrase(string body)
        {
            var cleaned = Regex.Replace(
                body,
                @"[\(\[]?\s*true\s*(or|/)\s*false\s*[\)\]]?\??",
                "",
                RegexOptions.IgnoreCase
            );

            return CollapseWhitespaceAndTrailingPunctuation(cleaned);
        }

        private static string CollapseWhitespaceAndTrailingPunctuation(string text)
        {
            var cleaned = Regex.Replace(text, @"\s{2,}", " ").Trim();
            cleaned = Regex.Replace(cleaned, @"[:,\-]\s*$", "").Trim();
            return cleaned;
        }

        private static string NormalizeImageTag(string body)
        {
            var match = Regex.Match(body, @"<img\b[^>]*>", RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return body;
            }

            var imgTag = match.Value;

            var srcMatch = Regex.Match(imgTag, @"src\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase);
            var src = srcMatch.Success ? srcMatch.Groups[1].Value : string.Empty;

            var altMatch = Regex.Match(imgTag, @"alt\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase);
            var alt = altMatch.Success ? altMatch.Groups[1].Value : string.Empty;

            var widthMatch = Regex.Match(imgTag, @"width\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase);
            var width = widthMatch.Success && !string.IsNullOrWhiteSpace(widthMatch.Groups[1].Value)
                ? widthMatch.Groups[1].Value
                : "600";

            var normalizedImg =
                $"<img src=\"{src}\" alt=\"{alt}\" width=\"{width}\" " +
                $"style=\"max-width: 600px; height: auto; width: {width}px;\" height=\"auto\">";

            var withoutImg = body.Remove(match.Index, match.Length);
            withoutImg = Regex.Replace(withoutImg, @"\s{2,}", " ").Trim();

            var textParagraph = string.IsNullOrEmpty(withoutImg) ? string.Empty : $"<p>{withoutImg}</p>";

            return
                $"<div style='direction: rtl'>{textParagraph}<p></p>" +
                $"<div class=\"se-component se-image-container __se__float-\"><figure style=\"width: {width}px;\">" +
                $"{normalizedImg}" +
                $"</figure></div><p><br></p></div>";
        }
    }
}