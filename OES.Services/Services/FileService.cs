using AutoMapper;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExcelDataReader;
using HtmlAgilityPack;
using HtmlToOpenXml;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;
using OES.Core.Entities;
using OES.Helper.Dtos.Document.Response;
using OES.Helper.Dtos.ExportFiles;
using OES.Helper.Dtos.FileDetails;
using OES.Helper.Dtos.Folder.Response;
using OES.Helper.Dtos.QTI;
using OES.Helper.Dtos.UploadFiles;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.General.DocLibBackEndHttpClientHelper;
using OES.Helper.General.NewApiResponse;
using OES.Helper.Interfaces;
using OES.Helper.QTICrypto;
using OES.Helper.ResourceFiles;
using OES.Helper.Static;
using OES.Interface.Interfaces;
using SharedHelper.Common;
using SharedHelper.General;
using System.Data;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using QuestionType = OES.Helper.Enums.QuestionType;

namespace OES.Services.Services
{
    public class FileService(ICommonService _commonService, IMapper _mapper, DocLibBackEndHttpClientHelper _docLibBackEndHttpClient, FilterParamsValues _filterParamsValues, IMemoryCache _cache) : IFileService
    {
        private readonly List<string> warnings = [];

        private const string cacheKey = "FileDetails_";

        // PARSE DATA FROM FILE
        public Task<List<UploadQuestionDetailsDto>> ParseQuestionsFromFile(string filePath)
        {
            if (System.IO.Path.GetExtension(filePath).Equals(".docx", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractQuestionsFromDocxToDtos(filePath);
            }
            if (System.IO.Path.GetExtension(filePath).Equals(".doc", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(Resource.UnsupportedWordVersion);
            }
            else if (System.IO.Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractQuestionsFromPdfToDtos(filePath);
            }
            if (System.IO.Path.GetExtension(filePath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractQuestionsFromExcel(filePath);
            }
            else if (System.IO.Path.GetExtension(filePath).Equals(".txt", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractQuestionsFromQtiAsync(filePath);
            }
            else
            {
                throw new InvalidOperationException(Resource.UnsupportedFileFormat);
            }
        }

        // EXTRACT DATA FROM FILE
        public static async Task<List<UploadQuestionDetailsDto>> ExtractQuestionsFromQtiAsync(string filePath)
        {
            var uploadQuestions = new List<UploadQuestionDetailsDto>();

            using (var stream = new StreamReader(filePath))
            {
                var qtiFileContent = await stream.ReadToEndAsync();

                var xDoc = XDocument.Parse(qtiFileContent);

                var assessmentItems = xDoc.Descendants(QTIBase.QtiAssessmentItem)
                    .Select(item => new QtiReverseDto
                    {
                        AssessmentIdentifier = Convert.ToInt64(item.Attribute(QTIBase.Identifier)?.Value ?? "0"),
                        AssessmentCode = item.Attribute(QTIBase.Title)?.Value,
                        QuestionCode = item.Attribute(QTIBase.Code)?.Value ?? "",
                        MaxChoices = Convert.ToInt64(item
                            .Descendants(QTIBase.QtiChoiceInteraction)
                            .FirstOrDefault()
                            ?.Attribute(QTIBase.MaxChoices)?.Value ?? "0"),
                        AssessmentCorrectResponse = [.. item.Descendants(QTIBase.QtiCorrectResponse)
                                                .Descendants(QTIBase.QtiValue)
                                                .Select(value => value.Value)],
                        AssessmentBody = item.Descendants(QTIBase.QtiPrompt).FirstOrDefault()?.Value,
                        AssessmentChoices = [.. item
                    .Descendants(QTIBase.QtiSimpleChoice)
                    .Select(choice => new AssessmentChoices
                    {
                        AssessmentChoicesIdentifier = Convert.ToInt64(choice.Attribute(QTIBase.Identifier)?.Value ?? "0"),
                        AssessmentChoicesContent = choice.Value,
                        AssessmentChoicesIsCorrect = (item
                                                    .Descendants(QTIBase.QtiCorrectResponse)
                                                    .Descendants(QTIBase.QtiValue)
                                                    .Any(correctAnswer => correctAnswer.Value == choice.Value)),
                    })
                        ]
                    }).ToList();

                var fileCodes = new HashSet<string>();

                foreach (var assessment in assessmentItems)
                {
                    int choiceCount = assessment.AssessmentChoices.Count;

                    if (choiceCount > 0)
                    {
                        int correctAnswerCount = assessment.AssessmentChoices.Count(c => c.AssessmentChoicesIsCorrect);

                        if (choiceCount == 2)
                            assessment.AssessmentQuestionType = nameof(QuestionType.TrueAndFalse);
                        else if (choiceCount > 2 && correctAnswerCount > 1)
                            assessment.AssessmentQuestionType = nameof(QuestionType.MultipleCorrectAnswers);
                        else if (choiceCount > 2 && correctAnswerCount == 1)
                            assessment.AssessmentQuestionType = nameof(QuestionType.MCQ);
                    }
                    else
                    {
                        assessment.AssessmentQuestionType = nameof(QuestionType.Essay);
                    }

                    var warningMessages = new List<string>();

                    if (fileCodes.Contains(assessment.QuestionCode))
                        warningMessages.Add(Resource.QuestionCodeAlreadyExistsInFile);
                    else
                        fileCodes.Add(assessment.QuestionCode);

                    if (string.IsNullOrWhiteSpace(assessment.AssessmentBody))
                        warningMessages.Add(Resource.QuestionBodyCannotBeEmpty);

                    var encodedBody = WebUtility.HtmlEncode(assessment.AssessmentBody);
                    var encodedModelAnswer = WebUtility.HtmlEncode(string.Join(", ", assessment.AssessmentCorrectResponse));

                    var uploadQuestion = new UploadQuestionDetailsDto
                    {
                        Id = assessment.AssessmentIdentifier,
                        QuestionCode = assessment.QuestionCode,
                        Body = encodedBody,
                        ModelAnswer = encodedModelAnswer,
                        QuestionType = assessment.AssessmentQuestionType,
                        Choices = assessment.AssessmentChoices?.Select(choice => new UploadChoicesDto
                        {
                            Identifier = choice.AssessmentChoicesIdentifier,
                            ChoiceText = WebUtility.HtmlEncode(choice.AssessmentChoicesContent),
                            IsCorrectAnswer = choice.AssessmentChoicesIsCorrect
                        }).ToList(),
                        HasWarning = warningMessages.Count > 0,
                        Warning = string.Join("; ", warningMessages)
                    };

                    uploadQuestions.Add(uploadQuestion);
                }
            }

            return uploadQuestions;
        }

        public async Task<List<UploadQuestionDetailsDto>> ExtractQuestionsFromDocxToDtos(string filePath)
        {
            var questionDetailsList = new List<UploadQuestionDetailsDto>();
            var usedQuestionCodes = new HashSet<string>();

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
            {
                var body = wordDoc.MainDocumentPart.Document.Body;

                if (body == null) return questionDetailsList;

                var allParagraphs = body.Elements<Paragraph>().ToList();

                string fullText = string.Empty;

                foreach (var p in allParagraphs) fullText += p.InnerText.Trim() + "\n";

                var questionBlocks = Regex.Split(fullText, DocxQuestionParsingConstants.QuestionCodeSplitPattern, RegexOptions.IgnoreCase)
                                          .Where(q => !string.IsNullOrWhiteSpace(q) && Regex.IsMatch(q, DocxQuestionParsingConstants.QuestionCodeKeywordPattern, RegexOptions.IgnoreCase))
                                          .ToList();

                for (int i = 0; i < questionBlocks.Count; i++)
                {
                    var currentQuestion = await ProcessSingleQuestion(
                        questionBlocks[i],
                        allParagraphs,
                        usedQuestionCodes,
                        wordDoc
                    );

                    if (currentQuestion != null && (!string.IsNullOrWhiteSpace(currentQuestion.Body) || currentQuestion.Choices.Any()))
                    {
                        questionDetailsList.Add(currentQuestion);
                    }
                }
            }

            return questionDetailsList;
        }

        private async Task<UploadQuestionDetailsDto> ProcessSingleQuestion(string questionBlock,
                                                                    List<Paragraph> allParagraphs,
                                                                    HashSet<string> usedQuestionCodes,
                                                                    WordprocessingDocument wordDoc
 )
        {
            var currentQuestion = new UploadQuestionDetailsDto
            {
                Id = GenerateUniqueLongId(),
                Body = string.Empty,
                Choices = new List<UploadChoicesDto>(),
                ModelAnswer = null,
                QuestionCode = string.Empty,
                HasWarning = false,
                Warning = string.Empty
            };

            var warningMessages = new List<string>();
            var processedImageRelIds = new HashSet<string>();

            // A. Extract Question Code
            var questionCodeMatch = Regex.Match(questionBlock, DocxQuestionParsingConstants.QuestionCodePattern, RegexOptions.IgnoreCase);
            if (questionCodeMatch.Success)
            {
                currentQuestion.QuestionCode = questionCodeMatch.Groups[1].Value
                    .Replace(DocxQuestionParsingConstants.DoubleClosingBracket, "")
                    .Replace(DocxQuestionParsingConstants.SingleClosingBracket, "")
                    .Trim();

                if (usedQuestionCodes.Contains(currentQuestion.QuestionCode))
                    warningMessages.Add(Resource.QuestionCodeAlreadyExistsInFile);
                else
                    usedQuestionCodes.Add(currentQuestion.QuestionCode);
            }
            else
            {
                warningMessages.Add(Resource.QuestionCodePrefix);
            }

            // B. Extract Item Bank Code
            var itemBankCodeMatch = Regex.Match(questionBlock, DocxQuestionParsingConstants.ItemBankCodePattern, RegexOptions.IgnoreCase);
            if (itemBankCodeMatch.Success)
            {
                currentQuestion.ItemBankCode = itemBankCodeMatch.Groups[1].Value
                    .Replace(DocxQuestionParsingConstants.DoubleClosingBracket, "")
                    .Replace(DocxQuestionParsingConstants.SingleClosingBracket, "")
                    .Trim();
            }

            // C. Scope Question Paragraphs
            var questionParagraphs = new List<Paragraph>();
            bool started = false;
            for (int i = 0; i < allParagraphs.Count; i++)
            {
                var pText = allParagraphs[i].InnerText;
                if (!started)
                {
                    if (pText.Contains(currentQuestion.QuestionCode) && Regex.IsMatch(pText, DocxQuestionParsingConstants.QuestionCodeKeywordPattern, RegexOptions.IgnoreCase))
                    {
                        started = true;
                        questionParagraphs.Add(allParagraphs[i]);
                    }
                }
                else
                {
                    if (Regex.IsMatch(pText, DocxQuestionParsingConstants.QuestionCodeStartPattern, RegexOptions.IgnoreCase)) break;
                    questionParagraphs.Add(allParagraphs[i]);
                }
            }

            // D. Scope Choice Paragraphs
            var choiceParagraphs = new List<Paragraph>();
            bool inChoices = false;
            foreach (var p in questionParagraphs)
            {
                string txt = p.InnerText;
                if (Regex.IsMatch(txt, DocxQuestionParsingConstants.ChoicesTagPattern, RegexOptions.IgnoreCase))
                {
                    inChoices = true;
                    continue;
                }
                if (Regex.IsMatch(txt, DocxQuestionParsingConstants.ModelAnswerTagPattern, RegexOptions.IgnoreCase))
                {
                    inChoices = false;
                }

                if (inChoices)
                {
                    choiceParagraphs.Add(p);
                }
            }

            // E. Process Choices with Images
            await ProcessChoicesAndImages(choiceParagraphs, currentQuestion, processedImageRelIds, wordDoc);

            // F. Build Body HTML (inline images) and extract the Question section
            var bodyParagraphs = questionParagraphs.Where(p => !choiceParagraphs.Contains(p)).ToList();
            var fullHtml = await BuildInlineHtml(bodyParagraphs, wordDoc, processedImageRelIds, DocxQuestionParsingConstants.BodyImageSize);

            string bodyHtml = ExtractSection(fullHtml, DocxQuestionParsingConstants.QuestionBodyStartPattern, DocxQuestionParsingConstants.QuestionBodyEndPattern);
            bodyHtml = Regex.Replace(bodyHtml, DocxQuestionParsingConstants.TrailingBracketsPattern, "").Trim();
            bodyHtml = Regex.Replace(bodyHtml, DocxQuestionParsingConstants.TrailingDotsBracketsPattern, "").Trim();
            bodyHtml = Regex.Replace(bodyHtml, DocxQuestionParsingConstants.MultipleDotsPattern, " ").Trim();
            bodyHtml = PreserveWhitespace(bodyHtml);
            if (!string.IsNullOrWhiteSpace(bodyHtml)) currentQuestion.Body = $"<p>{bodyHtml}</p>";

            // G. Extract Model Answer (raw text, fallback to HTML)
            var modelAnswerMatch = Regex.Match(questionBlock, DocxQuestionParsingConstants.ModelAnswerPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!modelAnswerMatch.Success)
            {
                modelAnswerMatch = Regex.Match(fullHtml, DocxQuestionParsingConstants.ModelAnswerPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }

            if (modelAnswerMatch.Success)
            {
                var maText = modelAnswerMatch.Groups[1].Value
                    .Replace(DocxQuestionParsingConstants.DoubleClosingBracket, "")
                    .Replace(DocxQuestionParsingConstants.SingleClosingBracket, "")
                    .Trim();
                maText = Regex.Replace(maText, DocxQuestionParsingConstants.MultipleDotsPattern, " ").Trim();
                maText = WebUtility.HtmlEncode(maText);
                maText = PreserveWhitespace(maText);
                currentQuestion.ModelAnswer = !string.IsNullOrWhiteSpace(maText) ? $"<p>{maText}</p>" : "";
            }
            else
            {
                currentQuestion.ModelAnswer = "";
            }

            // H. Determine Question Type
            int choiceCount = currentQuestion.Choices.Count;
            int correctAnswerCount = currentQuestion.Choices.Count(c => c.IsCorrectAnswer);
            bool hasModelAnswer = !string.IsNullOrWhiteSpace(currentQuestion.ModelAnswer);

            if (choiceCount > 0)
            {
                if (choiceCount == 2 && IsTrueFalseQuestion(currentQuestion.Choices))
                    currentQuestion.QuestionType = nameof(QuestionType.TrueAndFalse);
                else if (choiceCount >= 2 && correctAnswerCount > 1)
                    currentQuestion.QuestionType = nameof(QuestionType.MultipleCorrectAnswers);
                else if (choiceCount >= 2 && correctAnswerCount == 1)
                    currentQuestion.QuestionType = nameof(QuestionType.MCQ);
                else
                    currentQuestion.QuestionType = nameof(QuestionType.Essay);
            }
            else if (hasModelAnswer || choiceCount == 0)
            {
                currentQuestion.QuestionType = nameof(QuestionType.Essay);
            }

            // I. Set Model Answer from Choices if empty
            if (string.IsNullOrEmpty(currentQuestion.ModelAnswer) && choiceCount > 0)
            {
                var correctTxt = currentQuestion.Choices.Where(c => c.IsCorrectAnswer).Select(c => c.ChoiceText).ToList();
                if (correctTxt.Any()) currentQuestion.ModelAnswer = string.Join(", ", correctTxt);
            }

            // J. Add Warnings
            if (warningMessages.Count > 0)
            {
                currentQuestion.HasWarning = true;
                currentQuestion.Warning = string.Join("; ", warningMessages);
            }

            return currentQuestion;
        }

        private async Task ProcessChoicesAndImages(List<Paragraph> choiceParagraphs,
                                                       UploadQuestionDetailsDto currentQuestion,
                                                       HashSet<string> processedImageRelIds,
                                                       WordprocessingDocument wordDoc
            )
        {
            foreach (var paragraph in choiceParagraphs)
            {
                var rawText = paragraph.InnerText.Trim();

                rawText = Regex.Replace(rawText, DocxQuestionParsingConstants.ChoicesPrefixPattern, "", RegexOptions.IgnoreCase);
                rawText = rawText.Replace(DocxQuestionParsingConstants.DoubleClosingBracket, "").Trim();

                bool hasBlip = paragraph.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().Any();
                bool hasVml = paragraph.Descendants<DocumentFormat.OpenXml.Vml.ImageData>().Any();

                if (string.IsNullOrWhiteSpace(rawText) && !hasBlip && !hasVml)
                {
                    continue;
                }

                var paragraphImagesHtml = new StringBuilder();
                var imageRelIdsInPara = new List<string>();

                // Collect image IDs
                imageRelIdsInPara.AddRange(paragraph.Descendants<DocumentFormat.OpenXml.Drawing.Blip>()
                    .Select(b => b.Embed?.Value).Where(v => !string.IsNullOrEmpty(v)));
                imageRelIdsInPara.AddRange(paragraph.Descendants<DocumentFormat.OpenXml.Vml.ImageData>()
                    .Select(i => i.RelationshipId?.Value).Where(v => !string.IsNullOrEmpty(v)));

                // Upload images
                foreach (var relId in imageRelIdsInPara)
                {
                    if (processedImageRelIds.Contains(relId)) continue;
                    if (!wordDoc.MainDocumentPart.Parts.Any(prt => prt.RelationshipId == relId)) continue;

                    processedImageRelIds.Add(relId);

                    var imageUrl = await UploadImageAndGetUrl(relId, wordDoc);

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        paragraphImagesHtml.Append($"<img src=\"{imageUrl}\" class=\"{DocxQuestionParsingConstants.ImgCssClass}\" width=\"{DocxQuestionParsingConstants.ChoiceImageSize}\" height=\"{DocxQuestionParsingConstants.ChoiceImageSize}\" />");
                    }
                }

                var multiChoiceMatches = Regex.Matches(rawText, DocxQuestionParsingConstants.ChoiceItemPattern);
                if (multiChoiceMatches.Count > 1)
                {
                    foreach (Match m in multiChoiceMatches)
                    {
                        var txt = m.Groups[2].Value.Trim();
                        bool isCorrect = txt.Contains(DocxQuestionParsingConstants.CorrectAnswerMarker);
                        txt = txt.Replace(DocxQuestionParsingConstants.CorrectAnswerMarker, "").Trim();
                        txt = WebUtility.HtmlEncode(txt);
                        txt = PreserveWhitespace(txt);

                        if (!string.IsNullOrWhiteSpace(txt))
                        {
                            currentQuestion.Choices.Add(new UploadChoicesDto
                            {
                                ChoiceText = txt,
                                IsCorrectAnswer = isCorrect
                            });
                        }
                    }
                }
                else
                {
                    bool isCorrect = rawText.Contains(DocxQuestionParsingConstants.CorrectAnswerMarker);

                    var cleanText = Regex.Replace(rawText, DocxQuestionParsingConstants.BulletPrefixPattern, "").Trim();
                    cleanText = Regex.Replace(cleanText, DocxQuestionParsingConstants.ChoiceLetterPrefixPattern, "").Trim();
                    cleanText = cleanText.Replace(DocxQuestionParsingConstants.CorrectAnswerMarker, "").Trim();
                    cleanText = WebUtility.HtmlEncode(cleanText);
                    cleanText = PreserveWhitespace(cleanText);
                    string choiceContent = (cleanText + " " + paragraphImagesHtml.ToString()).Trim();

                    if (!string.IsNullOrWhiteSpace(choiceContent))
                    {
                        currentQuestion.Choices.Add(new UploadChoicesDto
                        {
                            ChoiceText = choiceContent,
                            IsCorrectAnswer = isCorrect
                        });
                    }
                }
            }
        }

        private async Task<string> BuildInlineHtml(IEnumerable<Paragraph> paragraphs, WordprocessingDocument wordDoc, HashSet<string> processedImageRelIds, int imgSize)
        {
            var sb = new StringBuilder();

            foreach (var paragraph in paragraphs)
            {
                foreach (var run in paragraph.Descendants<Run>())
                {
                    var blip = run.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
                    var vmlImg = run.Descendants<DocumentFormat.OpenXml.Vml.ImageData>().FirstOrDefault();
                    var relId = blip?.Embed?.Value ?? vmlImg?.RelationshipId?.Value;

                    if (!string.IsNullOrEmpty(relId))
                    {
                        if (processedImageRelIds.Contains(relId)) continue;
                        processedImageRelIds.Add(relId);

                        var imageUrl = await UploadImageAndGetUrl(relId, wordDoc);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            sb.Append($"<br/><img src=\"{imageUrl}\" class=\"{DocxQuestionParsingConstants.ImgCssClass}\" width=\"{imgSize}\" height=\"{imgSize}\" /><br/>");
                        }
                    }
                    else
                    {
                        var text = run.InnerText;
                        if (!string.IsNullOrEmpty(text))
                        {
                            sb.Append(WrapRunWithFormatting(text, run.RunProperties));
                        }
                    }
                }

                sb.Append(' ');
            }

            return sb.ToString();
        }

        private static string WrapRunWithFormatting(string text, RunProperties rPr)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var leadingMatch = Regex.Match(text, @"^\s+");
            var trailingMatch = Regex.Match(text, @"\s+$");

            string leading = leadingMatch.Success ? leadingMatch.Value : string.Empty;
            string trailing = trailingMatch.Success ? trailingMatch.Value : string.Empty;

            string core = text;
            if (leading.Length > 0) core = core.Substring(leading.Length);
            if (trailing.Length > 0 && core.Length >= trailing.Length)
                core = core.Substring(0, core.Length - trailing.Length);

            leading = WebUtility.HtmlEncode(leading);
            trailing = System.Net.WebUtility.HtmlEncode(trailing);

            if (string.IsNullOrEmpty(core))
                return WebUtility.HtmlEncode(text);

            string encodedCore = WebUtility.HtmlEncode(core);

            if (rPr == null)
                return leading + encodedCore + trailing;

            var styles = new List<string>();

            var color = rPr.Color?.Val?.Value;
            if (!string.IsNullOrEmpty(color) && !color.Equals("auto", StringComparison.OrdinalIgnoreCase))
                styles.Add($"color:#{color}");

            var sz = rPr.FontSize?.Val?.Value;
            if (!string.IsNullOrEmpty(sz) && int.TryParse(sz, out var halfPoints))
                styles.Add($"font-size:{halfPoints / 2}pt");

            var fontName = rPr.RunFonts?.Ascii?.Value;
            if (!string.IsNullOrEmpty(fontName))
                styles.Add($"font-family:'{fontName}'");

            var highlight = rPr.Highlight?.Val?.Value;
            if (highlight != null)
                styles.Add($"background-color:{highlight}");

            string formattedCore = encodedCore;

            if (styles.Count > 0)
                formattedCore = $"<span style=\"{string.Join(";", styles)}\">{formattedCore}</span>";

            bool isUnderline = rPr.Underline != null &&
                (rPr.Underline.Val == null || rPr.Underline.Val.Value != UnderlineValues.None);

            bool isItalic = (rPr.Italic != null && (rPr.Italic.Val == null || rPr.Italic.Val.Value)) ||
                            (rPr.ItalicComplexScript != null && (rPr.ItalicComplexScript.Val == null || rPr.ItalicComplexScript.Val.Value));

            bool isBold = (rPr.Bold != null && (rPr.Bold.Val == null || rPr.Bold.Val.Value)) ||
                          (rPr.BoldComplexScript != null && (rPr.BoldComplexScript.Val == null || rPr.BoldComplexScript.Val.Value));

            if (isUnderline) formattedCore = $"<u>{formattedCore}</u>";
            if (isItalic) formattedCore = $"<i>{formattedCore}</i>";
            if (isBold) formattedCore = $"<b>{formattedCore}</b>";

            return leading + formattedCore + trailing;
        }

        private static string ExtractSection(string fullHtml, string startTagPattern, string nextTagsPattern)
        {
            var startMatch = Regex.Match(fullHtml, startTagPattern, RegexOptions.IgnoreCase);
            if (!startMatch.Success) return string.Empty;

            int startIndex = startMatch.Index + startMatch.Length;

            int endIndex = fullHtml.Length;
            if (!string.IsNullOrEmpty(nextTagsPattern))
            {
                var endMatch = Regex.Match(fullHtml.Substring(startIndex), nextTagsPattern, RegexOptions.IgnoreCase);
                if (endMatch.Success) endIndex = startIndex + endMatch.Index;
            }

            return fullHtml.Substring(startIndex, endIndex - startIndex).Trim();
        }

        private async Task<string> UploadImageAndGetUrl(string relId, WordprocessingDocument wordDoc)
        {
            var imagePart = (ImagePart)wordDoc.MainDocumentPart.GetPartById(relId);
            var imageBytes = GetImageBytes(imagePart);
            var contentType = imagePart.ContentType;
            var fileName = Guid.NewGuid() + GetFileExtensionFromContentType(contentType);

            var imageData = new MediaFileDataDto
            {
                Content = imageBytes,
                Size = imageBytes.Length,
                Name = fileName,
                Type = contentType
            };

            var urlFilePath = await _docLibBackEndHttpClient.PostAsJsonAsync<MediaFileDataDto, NewApiResponse<DocumentUrlFileResponseDto>>(
                "Document/UploadDocumentContent",
                imageData
            );

            if (urlFilePath.Success)
            {
                return $"{CentralizedUrlHelper.DocLibApiBaseUrl}{urlFilePath.Data.FileRelativeUrl}";
            }

            return null;
        }

        public async Task<List<UploadQuestionDetailsDto>> ExtractQuestionsFromPdfToDtos(string filePath)
        {
            var questionDetailsList = new List<UploadQuestionDetailsDto>();

            using (PdfReader reader = new(filePath))
            {
                UploadQuestionDetailsDto currentQuestion = null;

                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    string pageText = PdfTextExtractor.GetTextFromPage(reader, i);

                    var lines = pageText.Split(["\n", "\r"], StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        string paragraphText = line.Trim();

                        if (string.IsNullOrWhiteSpace(paragraphText))
                            continue;

                        if (Regex.IsMatch(paragraphText, @"^\d+\)"))
                        {
                            if (currentQuestion != null)
                            {
                                int choiceCount = currentQuestion.Choices.Count;

                                if (choiceCount > 0)
                                {
                                    int correctAnswerCount = currentQuestion.Choices.Count(c => c.IsCorrectAnswer);

                                    if (choiceCount == 2)
                                        currentQuestion.QuestionType = nameof(QuestionType.TrueAndFalse);
                                    else if (choiceCount > 2 && correctAnswerCount > 1)
                                        currentQuestion.QuestionType = nameof(QuestionType.MultipleCorrectAnswers);
                                    else if (choiceCount > 2 && correctAnswerCount == 1)
                                        currentQuestion.QuestionType = nameof(QuestionType.MCQ);

                                    currentQuestion.ModelAnswer = string.Join(", ", currentQuestion.Choices.Where(c => c.IsCorrectAnswer).Select(c => c.ChoiceText));
                                }
                                else
                                {
                                    currentQuestion.QuestionType = nameof(QuestionType.Essay);
                                }

                                questionDetailsList.Add(currentQuestion);
                            }

                            currentQuestion = new UploadQuestionDetailsDto
                            {
                                Body = string.Empty,
                                Choices = [],
                                ModelAnswer = null
                            };

                            if (!string.IsNullOrWhiteSpace(paragraphText))
                            {
                                var images = ExtractImagesFromPdf(reader, i);

                                if (images.Count != 0)
                                {
                                    foreach (var image in images)
                                    {
                                        var contentType = GetContentTypeFromImage(image);

                                        var fileExtension = GetFileExtensionFromContentType(contentType);

                                        var fileName = SaveImageToFileSystem(image, fileExtension);

                                        var fileLength = image.Length;

                                        var imageData = new MediaFileDataDto
                                        {
                                            Content = image,
                                            Size = fileLength,
                                            Name = fileName,
                                            Type = contentType
                                        };

                                        var urlFilePath = await _docLibBackEndHttpClient.PostAsJsonAsync<MediaFileDataDto, NewApiResponse<DocumentUrlFileResponseDto>>("Document/UploadDocumentContent", imageData);

                                        if (urlFilePath.Success)
                                        {
                                            currentQuestion.Body += $" <img src=\"{CentralizedUrlHelper.DocLibApiBaseUrl}{urlFilePath.Data.FileRelativeUrl}\" class=\"img-fluid\" width=\"300\" height=\"300\" />";
                                        }
                                    }
                                }

                                var cleanedText = Regex.Replace(paragraphText, @"^[\da-zA-Z]+\)\s*", "").Trim();

                                if (!string.IsNullOrWhiteSpace(cleanedText))
                                {
                                    var encodedText = WebUtility.HtmlEncode(cleanedText);
                                    currentQuestion.Body = $"<p>{encodedText}</p>";
                                }
                            }
                        }
                        else if (Regex.IsMatch(paragraphText, @"^[a-zA-Z]\)"))
                        {
                            var choiceText = Regex.Replace(paragraphText, @"^[a-zA-Z]\)\s*", "").Trim();

                            bool isCorrect = paragraphText.Contains('*');

                            choiceText = choiceText.Replace("*", "").Trim();

                            choiceText = WebUtility.HtmlEncode(choiceText);

                            currentQuestion?.Choices.Add(new UploadChoicesDto
                            {
                                ChoiceText = choiceText,
                                IsCorrectAnswer = isCorrect
                            });
                        }
                        else
                        {
                            if (paragraphText.Contains('?'))
                            {
                                if (!string.IsNullOrWhiteSpace(paragraphText) && currentQuestion != null)
                                {
                                    var encodedText = WebUtility.HtmlEncode(paragraphText);
                                    currentQuestion.Body += $"<p>{encodedText}</p>";
                                }
                            }
                            else
                            {
                                if (currentQuestion != null)
                                {
                                    currentQuestion.ModelAnswer = WebUtility.HtmlEncode(paragraphText.Trim());
                                }
                            }
                        }
                    }
                }

                if (currentQuestion != null)
                {
                    int choiceCount = currentQuestion.Choices.Count;

                    if (choiceCount > 0)
                    {
                        int correctAnswerCount = currentQuestion.Choices.Count(c => c.IsCorrectAnswer);

                        if (choiceCount == 2)
                            currentQuestion.QuestionType = nameof(QuestionType.TrueAndFalse);
                        else if (choiceCount > 2 && correctAnswerCount > 1)
                            currentQuestion.QuestionType = nameof(QuestionType.MultipleCorrectAnswers);
                        else if (choiceCount > 2 && correctAnswerCount == 1)
                            currentQuestion.QuestionType = nameof(QuestionType.MCQ);

                        currentQuestion.ModelAnswer = string.Join(", ", currentQuestion.Choices.Where(c => c.IsCorrectAnswer).Select(c => c.ChoiceText));
                    }
                    else
                    {
                        currentQuestion.QuestionType = nameof(QuestionType.Essay);
                    }

                    questionDetailsList.Add(currentQuestion);
                }
            }

            return questionDetailsList;
        }

        public async Task<List<UploadQuestionDetailsDto>> ExtractQuestionsFromExcel(string filePath)
        {
            List<UploadQuestionDetailsDto> uploadQuestionFDetailsDtos = [];

            var data = ReadDataFromExcel(filePath);

            var existingCodesInFile = new HashSet<string>();

            foreach (var d in data)
            {
                if (d.Type == nameof(QuestionType.MCQ) ||
                    d.Type == nameof(QuestionType.MultipleCorrectAnswers) ||
                    d.Type == nameof(QuestionType.TrueAndFalse) ||
                    d.Choices.Count > 1)
                {
                    uploadQuestionFDetailsDtos.Add(ExtractChoiceQuestionFromExcel(d, existingCodesInFile));
                }
                else if (d.Type == nameof(QuestionType.Essay) || d.Choices.Count == 0)
                {
                    uploadQuestionFDetailsDtos.Add(ExtractQuestionsFromExcelEssay(d, existingCodesInFile));
                }
            }

            await Task.CompletedTask;

            return uploadQuestionFDetailsDtos;
        }

        // EXPORT DATA TO FILES

        /* EXCEL METHODS */

        public async Task<IApiResponse> ExportQuestionDetailsToFileAsync(List<long> questionIds, long typeId, long languageId)
        {
            var questionMetadata = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAllAsync(result => questionIds.Contains(result.Id) &&
                             result.QuestionStatus == QuestionStatus.Approved &&
                             result.QuestionTypeId == typeId,
                             Including: "QuestionType");

            if (!questionMetadata.Any(x => x.Id != 0))
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NoQuestionMetadataFound, HttpStatusCode.BadRequest, Resource.NoQuestionsFound);

            var questionDetails = await _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>()
                .GetAllAsync(result => questionMetadata.Select(x => x.Id).ToList().Contains(result.QuestionMetadataId) && result.Language.Id == languageId, Including: "QuestionsChoices");

            if (!questionDetails.Any(x => x.Id != 0))
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NoQuestionDetailsFound, HttpStatusCode.BadRequest, Resource.NoQuestionDetailsForLanguage);

            var exportData = questionDetails
                .SelectMany(result =>
                {
                    var mainRow = new
                    {
                        QuestionId = result.QuestionMetadataId,
                        QuestionBody = result.Body,
                        Choices = string.Empty,
                        CorrectAnswer = result.QuestionMetadata?.QuestionTypeId == (int)OES.Helper.Enums.QuestionType.Essay ? "Is Essay" : string.Empty,
                        Type = result.QuestionMetadata?.QuestionType?.Name ?? "N/A"
                    };

                    var choiceRows = result.QuestionsChoices?.Select(choice => new
                    {
                        QuestionId = result.QuestionMetadataId,
                        QuestionBody = string.Empty,
                        Choices = choice?.ChoiceText ?? string.Empty,
                        CorrectAnswer = choice?.IsCorrectAnswer == true ? "*" : string.Empty,
                        Type = string.Empty
                    }) ?? [];

                    return new[] { mainRow }.Concat(choiceRows);
                }).ToList();

            var filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Exports", $"QuestionDetails_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx");

            if (!Directory.Exists(System.IO.Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.AddWorksheet("QuestionDetails");

                var properties = exportData.FirstOrDefault()?.GetType().GetProperties();

                if (properties != null)
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = properties[i].Name;
                    }

                    for (int rowIndex = 0; rowIndex < exportData.Count; rowIndex++)
                    {
                        for (int colIndex = 0; colIndex < properties.Length; colIndex++)
                        {
                            var cellValue = properties[colIndex].GetValue(exportData[rowIndex]);

                            if (cellValue is long || cellValue is int || cellValue is decimal || cellValue is double)
                            {
                                worksheet.Cell(rowIndex + 2, colIndex + 1).Value = Convert.ToDouble(cellValue);
                            }
                            else if (cellValue is string)
                            {
                                worksheet.Cell(rowIndex + 2, colIndex + 1).Value = (string)cellValue;
                            }
                            else
                            {
                                worksheet.Cell(rowIndex + 2, colIndex + 1).Value = cellValue?.ToString() ?? string.Empty;
                            }
                        }
                    }
                }

                workbook.SaveAs(filePath);
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.FileExportedSuccessfully, filePath);
        }

        public async Task<IApiResponse> ExportQuestionDetailsToFileAsync(List<long> questionIds, long languageId)
        {
            var questionMetadata = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAllAsync(result => questionIds.Contains(result.Id) &&
                             result.QuestionStatus == QuestionStatus.Approved,
                             Including: "QuestionType");

            if (!questionMetadata.Any(x => x.Id != 0))
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NoQuestionMetadataFound, HttpStatusCode.BadRequest, "No Questions Found");

            var questionDetails = await _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>()
                .GetAllAsync(result => questionIds.Contains(result.QuestionMetadataId) && result.Language.Id == languageId, Including: "QuestionsChoices");

            if (!questionDetails.Any(x => x.Id != 0))
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NoQuestionDetailsFound, HttpStatusCode.BadRequest, "No Question Details Found For The Given Language");

            var exportData = questionDetails
                .SelectMany(result =>
                {
                    var mainRow = new
                    {
                        QuestionId = result.QuestionMetadataId,
                        QuestionBody = result.Body,
                        Choices = string.Empty,
                        CorrectAnswer = result.QuestionMetadata?.QuestionTypeId == (int)QuestionType.Essay ? result.ModelAnswer ?? string.Empty : string.Empty,
                        Type = result.QuestionMetadata?.QuestionType?.Name ?? "N/A"
                    };

                    if (result.QuestionMetadata?.QuestionType?.Name == "MCQ")
                    {
                        var choiceRows = result.QuestionsChoices?.Select(choice => new
                        {
                            QuestionId = result.QuestionMetadataId,
                            QuestionBody = string.Empty,
                            Choices = choice?.ChoiceText ?? string.Empty,
                            CorrectAnswer = choice?.IsCorrectAnswer == true ? "*" : string.Empty,
                            Type = string.Empty
                        }) ?? [];

                        return new[] { mainRow }.Concat(choiceRows);
                    }
                    else if (result.QuestionMetadata?.QuestionType?.Name == "Essay")
                    {
                        var essayRow = new
                        {
                            QuestionId = result.QuestionMetadataId,
                            QuestionBody = result.Body,
                            Choices = string.Empty,
                            CorrectAnswer = result.ModelAnswer ?? string.Empty,
                            Type = "Essay"
                        };

                        return new[] { essayRow };
                    }

                    return Enumerable.Empty<object>();
                }).ToList();

            var filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Exports", $"QuestionDetails_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx");

            if (!Directory.Exists(System.IO.Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.AddWorksheet("QuestionDetails");

                var properties = exportData.FirstOrDefault()?.GetType().GetProperties();

                if (properties != null)
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = properties[i].Name;
                    }

                    for (int rowIndex = 0; rowIndex < exportData.Count; rowIndex++)
                    {
                        for (int colIndex = 0; colIndex < properties.Length; colIndex++)
                        {
                            var cellValue = properties[colIndex].GetValue(exportData[rowIndex]);

                            if (cellValue is long || cellValue is int || cellValue is decimal || cellValue is double)
                            {
                                worksheet.Cell(rowIndex + 2, colIndex + 1).Value = Convert.ToDouble(cellValue);
                            }
                            else if (cellValue is string)
                            {
                                worksheet.Cell(rowIndex + 2, colIndex + 1).Value = (string)cellValue;
                            }
                            else
                            {
                                worksheet.Cell(rowIndex + 2, colIndex + 1).Value = cellValue?.ToString() ?? string.Empty;
                            }
                        }
                    }
                }

                workbook.SaveAs(filePath);
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.FileExportedSuccessfully, filePath);
        }

        public async Task<byte[]> ExportQuestionItemBankHierarchyToFileAsync(ExportDataFileDto exportDataFileDto)
        {
            byte[] fileBytes = [];

            try
            {
                var itemBanks = await _commonService
                    ._unitOfWork
                    .Repository<ItemBank, long>()
                    .GetAllAsync(x => exportDataFileDto.itemBanksIds.Contains(x.Id));

                if (!itemBanks.Any())
                    return fileBytes;

                var itemBankIds = itemBanks.Select(result => result.Id).ToList();

                var questionMetadata = await _commonService
                    ._unitOfWork
                    .Repository<QuestionMetadata, long>()
                    .GetAllAsync(result => itemBankIds.Contains(result.ItemBankId) &&
                                 result.QuestionStatus == QuestionStatus.Approved &&
                                 result.QuestionDetails.Any(qd => qd.LanguageId == exportDataFileDto.languageId),
                                 Including: "QuestionDetails.QuestionsChoices,ItemBank,QuestionType");

                if (!questionMetadata.Any())
                    return fileBytes;

                var exportData = new List<QuestionExportModel>();

                foreach (var meta in questionMetadata)
                {
                    foreach (var qd in meta.QuestionDetails.Where(qd => qd.LanguageId == exportDataFileDto.languageId))
                    {
                        if (qd.QuestionsChoices != null && qd.QuestionsChoices.Any())
                        {
                            var choices = qd.QuestionsChoices.Select((qc, index) => new QuestionExportModel
                            {
                                QuestionId = meta.Id,
                                ItemBank = meta.ItemBank?.Name ?? Resource.Unknown,
                                QuestionBody = index == 0 ? (exportDataFileDto.withTags ? qd.Body ?? Resource.NoBody : HtmlTagsCleaner.Clean(qd.Body ?? Resource.NoBody)) : string.Empty,
                                Choice = exportDataFileDto.withTags ? qc.ChoiceText : HtmlTagsCleaner.Clean(qc.ChoiceText),
                                CorrectAnswer = qc.IsCorrectAnswer ? "*" : string.Empty,
                                QuestionType = meta.QuestionType?.Name ?? Resource.Unknown
                            });

                            exportData.AddRange(choices);
                        }
                        else
                        {
                            exportData.Add(new QuestionExportModel
                            {
                                QuestionId = meta.Id,
                                ItemBank = meta.ItemBank?.Name ?? Resource.Unknown,
                                QuestionBody = exportDataFileDto.withTags ? qd.Body ?? Resource.NoBody : HtmlTagsCleaner.Clean(qd.Body ?? Resource.NoBody),
                                Choice = string.Empty,
                                CorrectAnswer = meta.QuestionType?.Name == "Essay" ? (exportDataFileDto.withTags ? qd.ModelAnswer ?? string.Empty : HtmlTagsCleaner.Clean(qd.ModelAnswer ?? string.Empty)) : string.Empty,
                                QuestionType = meta.QuestionType?.Name ?? Resource.Unknown
                            });
                        }
                    }
                }

                if (exportData.Count == 0) return fileBytes;

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.AddWorksheet("QuestionDetails");

                    var properties = typeof(QuestionExportModel).GetProperties();

                    for (int i = 0; i < properties.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = properties[i].Name;
                    }

                    for (int rowIndex = 0; rowIndex < exportData.Count; rowIndex++)
                    {
                        for (int colIndex = 0; colIndex < properties.Length; colIndex++)
                        {
                            var cellValue = properties[colIndex].GetValue(exportData[rowIndex]);
                            worksheet.Cell(rowIndex + 2, colIndex + 1).Value = cellValue?.ToString() ?? string.Empty;
                        }
                    }

                    using var memoryStream = new MemoryStream();

                    workbook.SaveAs(memoryStream);

                    memoryStream.Seek(0, SeekOrigin.Begin);

                    fileBytes = memoryStream.ToArray();
                }
            }
            catch (Exception)
            {
                return fileBytes;
            }

            return fileBytes;
        }

        /* EXCEL METHODS */

        /* DOCX METHODS */

        public async Task<IApiResponse> ExportQuestionDetailsToDocxFileAsync(List<long> questionIds, long typeId, long languageId)
        {
            var questionMetadata = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAllAsync(result => questionIds.Contains(result.Id) &&
                             result.QuestionStatus == QuestionStatus.Approved &&
                             result.QuestionTypeId == typeId,
                             Including: "QuestionType");

            if (!questionMetadata.Any(x => x.Id != 0))
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NoQuestionMetadataFound, HttpStatusCode.BadRequest, Resource.NoQuestionsFound);

            var questionDetails = await _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>()
                .GetAllAsync(result => questionMetadata.Select(x => x.Id).ToList().Contains(result.QuestionMetadataId) && result.Language.Id == languageId, Including: "QuestionsChoices");

            if (!questionDetails.Any(x => x.Id != 0))
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NoQuestionDetailsFound, HttpStatusCode.BadRequest, Resource.NoQuestionDetailsForLanguage);

            var filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Exports", $"QuestionDetails_{DateTimeHelper.Now:yyyyMMddHHmmss}.docx");

            if (!Directory.Exists(System.IO.Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
            }

            using (var wordDoc = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            {
                var mainPart = wordDoc.AddMainDocumentPart();

                mainPart.Document = new Document();

                var body = new Body();

                var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();

                numberingPart.Numbering = new Numbering();

                var abstractNumQuestions = new AbstractNum(
                    new Level(
                        new StartNumberingValue { Val = 1 },
                        new NumberingFormat { Val = NumberFormatValues.Decimal },
                        new LevelText { Val = "%1)" },
                        new ParagraphProperties(new Indentation { Left = "720", Hanging = "360" })
                    )
                    { LevelIndex = 0 }
                )
                { AbstractNumberId = 1 };

                numberingPart.Numbering.Append(abstractNumQuestions);

                var numberingInstanceQuestions = new NumberingInstance(
                    new AbstractNumId { Val = 1 }
                )
                { NumberID = 1 };

                numberingPart.Numbering.Append(numberingInstanceQuestions);

                int questionIndex = 0;

                foreach (var question in questionDetails)
                {
                    questionIndex++;

                    var questionParagraph = new Paragraph(
                        new ParagraphProperties(
                            new NumberingProperties(
                                new NumberingLevelReference { Val = 0 },
                                new NumberingId { Val = 1 }
                            )
                        ),
                        new Run(new Text($"{question.Body}"))
                    );

                    body.AppendChild(questionParagraph);

                    if (question.QuestionsChoices != null && question.QuestionsChoices.Any())
                    {
                        char choiceLetter = 'a';

                        foreach (var choice in question.QuestionsChoices)
                        {
                            var choiceText = string.IsNullOrEmpty(choice.ChoiceText) ? " " : choice.ChoiceText;

                            var choiceParagraph = new Paragraph(
                                new ParagraphProperties(
                                    new Indentation { Left = "750" }
                                ),
                                new Run(new Text($"{choiceLetter}) {choiceText}{(choice.IsCorrectAnswer ? "*" : "")}"))
                            );

                            body.AppendChild(choiceParagraph);

                            choiceLetter++;
                        }
                    }

                    body.AppendChild(new Paragraph());
                }

                mainPart.Document.AppendChild(body);
                mainPart.Document.Save();
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.FileExportedSuccessfully, filePath);
        }

        public async Task<IApiResponse> ExportQuestionDetailsToDocxFileAsync(List<long> questionIds, long languageId)
        {
            var questionMetadata = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAllAsync(result => questionIds.Contains(result.Id) &&
                             result.QuestionStatus == QuestionStatus.Approved,
                             Including: "QuestionType");

            if (!questionMetadata.Any(x => x.Id != 0))
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NoQuestionMetadataFound, HttpStatusCode.BadRequest, "No Questions Found");

            var questionDetails = await _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>()
                .GetAllAsync(result => questionMetadata.Select(x => x.Id).ToList().Contains(result.QuestionMetadataId) && result.Language.Id == languageId, Including: "QuestionsChoices");

            if (!questionDetails.Any(x => x.Id != 0))
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NoQuestionDetailsFound, HttpStatusCode.BadRequest, "No Question Details Found For The Given Language");

            var filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Exports", $"QuestionDetails_{DateTimeHelper.Now:yyyyMMddHHmmss}.docx");

            if (!Directory.Exists(System.IO.Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
            }

            using (var wordDoc = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            {
                var mainPart = wordDoc.AddMainDocumentPart();

                mainPart.Document = new Document();

                var body = new Body();

                var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();

                numberingPart.Numbering = new Numbering();

                var abstractNumQuestions = new AbstractNum(
                    new Level(
                        new StartNumberingValue { Val = 1 },
                        new NumberingFormat { Val = NumberFormatValues.Decimal },
                        new LevelText { Val = "%1)" },
                        new ParagraphProperties(new Indentation { Left = "720", Hanging = "360" })
                    )
                    { LevelIndex = 0 }
                )
                { AbstractNumberId = 1 };

                numberingPart.Numbering.Append(abstractNumQuestions);

                var numberingInstanceQuestions = new NumberingInstance(
                    new AbstractNumId { Val = 1 }
                )
                { NumberID = 1 };

                numberingPart.Numbering.Append(numberingInstanceQuestions);

                foreach (var question in questionDetails)
                {
                    var questionParagraph = new Paragraph(
                        new ParagraphProperties(
                            new NumberingProperties(
                                new NumberingLevelReference { Val = 0 },
                                new NumberingId { Val = 1 }
                            )
                        ),
                        new Run(new Text($"{question.Body}"))
                    );

                    body.AppendChild(questionParagraph);

                    if (question.QuestionMetadata.QuestionType.Name == nameof(QuestionType.MCQ))
                    {
                        if (question.QuestionsChoices != null && question.QuestionsChoices.Any())
                        {
                            char choiceLetter = 'a';

                            foreach (var choice in question.QuestionsChoices)
                            {
                                var choiceText = string.IsNullOrEmpty(choice.ChoiceText) ? " " : choice.ChoiceText;

                                var choiceParagraph = new Paragraph(
                                    new ParagraphProperties(
                                        new Indentation { Left = "750" }
                                    ),
                                    new Run(new Text($"{choiceLetter}) {choiceText}{(choice.IsCorrectAnswer ? "*" : "")}"))
                                );

                                body.AppendChild(choiceParagraph);

                                choiceLetter++;
                            }
                        }
                    }
                    else if (question.QuestionMetadata.QuestionType.Name == nameof(QuestionType.Essay))
                    {
                        var modelAnswerParagraph = new Paragraph(
                            new ParagraphProperties(
                                new Indentation { Left = "750" }
                            ),
                            new Run(new Text($"Model Answer : {question.ModelAnswer}"))
                        );

                        body.AppendChild(modelAnswerParagraph);
                    }

                    body.AppendChild(new Paragraph());
                }

                mainPart.Document.AppendChild(body);
                mainPart.Document.Save();
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.FileExportedSuccessfully, filePath);
        }

        public async Task<byte[]> ExportQuestionItemBankHierarchyToDocxFileAsync(ExportDataFileDto exportDataFileDto)
        {
            byte[] fileBytes = [];

            var questionMetadata = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAllAsync(result => exportDataFileDto.itemBanksIds.Contains(result.ItemBankId) &&
                             result.QuestionStatus == QuestionStatus.Approved &&
                             result.QuestionDetails.Any(qd => qd.LanguageId == exportDataFileDto.languageId),
                             Including: "QuestionDetails.QuestionsChoices,QuestionType");

            if (!questionMetadata.Any())
                return fileBytes;

            var filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Exports", $"Questions_{DateTimeHelper.Now:yyyyMMddHHmmss}.docx");

            if (!Directory.Exists(System.IO.Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
            }

            using (var memoryStream = new MemoryStream())
            {
                using (var wordDoc = WordprocessingDocument.Create(memoryStream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
                {
                    var mainPart = wordDoc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = new Body();

                    var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
                    numberingPart.Numbering = new Numbering();

                    var abstractNumQuestions = new AbstractNum(
                        new Level(
                            new StartNumberingValue { Val = 1 },
                            new NumberingFormat { Val = NumberFormatValues.Decimal },
                            new LevelText { Val = "%1)" },
                            new ParagraphProperties(new Indentation { Left = "720", Hanging = "360" })
                        )
                        { LevelIndex = 0 }
                    )
                    { AbstractNumberId = 1 };

                    numberingPart.Numbering.Append(abstractNumQuestions);

                    var numberingInstanceQuestions = new NumberingInstance(new AbstractNumId { Val = 1 })
                    {
                        NumberID = 1
                    };

                    numberingPart.Numbering.Append(numberingInstanceQuestions);

                    var converter = new HtmlConverter(mainPart);

                    foreach (var meta in questionMetadata)
                    {
                        foreach (var question in meta.QuestionDetails.Where(qd => qd.LanguageId == exportDataFileDto.languageId))
                        {
                            var questionHtml = exportDataFileDto.withTags ? question.Body : HtmlTagsCleaner.Clean(question.Body);

                            string processedBody = question.Body;

                            if (!string.IsNullOrWhiteSpace(processedBody))
                            {
                                var doc = new HtmlDocument();

                                doc.LoadHtml(processedBody);

                                var mediaNodes = doc.DocumentNode.SelectNodes("//img[@src]");

                                if (mediaNodes != null)
                                {
                                    foreach (var node in mediaNodes)
                                    {
                                        var src = node.GetAttributeValue("src", string.Empty);

                                        if (!string.IsNullOrWhiteSpace(src))
                                        {
                                            var uri = new Uri(src);
                                            string pathAndQuery = uri.PathAndQuery;
                                            string newUri = $"{CentralizedUrlHelper.DocLibApiBaseUrl}{pathAndQuery}";
                                            node.SetAttributeValue("src", newUri);
                                        }
                                    }

                                    processedBody = doc.DocumentNode.OuterHtml;
                                }
                            }

                            var questionParagraphs = await converter.ParseAsync(processedBody);

                            foreach (var p in questionParagraphs)
                            {
                                foreach (var image in p.Elements<Drawing>())
                                {
                                    var imageParagraph = new Paragraph();

                                    imageParagraph.Append(image.CloneNode(true));

                                    body.Append(imageParagraph);
                                }
                            }

                            var questionParagraph = new Paragraph();

                            var paragraphProperties = new ParagraphProperties(
                                new NumberingProperties(
                                    new NumberingLevelReference { Val = 0 },
                                    new NumberingId { Val = 1 }
                                )
                            );

                            questionParagraph.PrependChild(paragraphProperties);

                            foreach (var p in questionParagraphs)
                            {
                                foreach (var run in p.Elements<Run>())
                                {
                                    questionParagraph.Append(run.CloneNode(true));
                                }
                            }

                            body.Append(questionParagraph);

                            if (question.QuestionsChoices != null && question.QuestionsChoices.Any())
                            {
                                char choiceLetter = 'a';

                                foreach (var choice in question.QuestionsChoices)
                                {
                                    var choiceText = string.IsNullOrEmpty(choice.ChoiceText) ? " " : choice.ChoiceText;

                                    var choiceParagraph = new Paragraph(
                                        new ParagraphProperties(
                                            new Indentation { Left = "750" }
                                        ),
                                        exportDataFileDto.withTags
                                            ? new Run(new Text($"{choiceLetter}) {choiceText}{(choice.IsCorrectAnswer ? "*" : "")}"))
                                            : new Run(new Text($"{choiceLetter}) {HtmlTagsCleaner.Clean(choiceText)}{(choice.IsCorrectAnswer ? "*" : "")}"))
                                    );

                                    body.AppendChild(choiceParagraph);

                                    choiceLetter++;
                                }

                                var modelAnswerParagraph = new Paragraph(
                                    new ParagraphProperties(
                                        new Indentation { Left = "750" }
                                    ),
                                    exportDataFileDto.withTags
                                        ? new Run(new Text(string.Format(Resource.ModelAnswerWithValue, question.ModelAnswer ?? Resource.NoAnswerProvided)))
                                        : new Run(new Text(string.Format(Resource.ModelAnswerWithValue, HtmlTagsCleaner.Clean(question.ModelAnswer ?? Resource.NoAnswerProvided))))
                                );

                                body.AppendChild(modelAnswerParagraph);
                            }
                            else if (meta.QuestionType?.Name == nameof(QuestionType.Essay))
                            {
                                var modelAnswerParagraph = new Paragraph(
                                    new ParagraphProperties(
                                        new Indentation { Left = "750" }
                                    ),
                                    exportDataFileDto.withTags ? new Run(new Text($"{Resource.ModelAnswer}: {question.ModelAnswer ?? Resource.NoAnswerProvided}")) : new Run(new Text($"Model Answer: {HtmlTagsCleaner.Clean(question.ModelAnswer ?? "No Answer Provided")}"))
                                );

                                body.AppendChild(modelAnswerParagraph);
                            }

                            body.AppendChild(new Paragraph());
                        }
                    }

                    mainPart.Document.AppendChild(body);

                    wordDoc.Save();
                }

                fileBytes = memoryStream.ToArray();
            }

            return fileBytes;
        }

        /* DOCX METHODS */

        // ADD QUESTIONS TO DATABASE
        public async Task<IApiResponse> AddFileQuestions(string uploadQuestions, string metadata)
        {
            using (var transaction = await _commonService._unitOfWork.BeginTransactionAsync())
            {
                try
                {
                    using (var command = _commonService._unitOfWork.CreateDbCommand())
                    {
                        command.Transaction = transaction.GetDbTransaction();
                        command.CommandText = "AddFileQuestions";
                        command.CommandType = CommandType.StoredProcedure;

                        var inputQuestions = new MySqlParameter("@UploadQuestionData", MySqlDbType.JSON)
                        {
                            Value = uploadQuestions
                        };
                        var inputOrgSignature = new MySqlParameter("@OrgSignature", MySqlDbType.VarChar, 255)
                        {
                            Value = _filterParamsValues.Signature
                        };
                        var inputCreationUser = new MySqlParameter("@CreationUser", MySqlDbType.VarChar, 255)
                        {
                            Value = _filterParamsValues.UserEmail
                        };
                        var inputMetaData = new MySqlParameter("@QuestionMetaData", MySqlDbType.JSON)
                        {
                            Value = metadata
                        };
                        var inputOrganizationId = new MySqlParameter("@OrganizationId", MySqlDbType.Int32)
                        {
                            Value = _filterParamsValues.OrganizationId
                        };
                        var returnStatusParam = new MySqlParameter("@status", MySqlDbType.Int32)
                        {
                            Direction = ParameterDirection.Output
                        };
                        var errorMessageParam = new MySqlParameter("@errorMessage", MySqlDbType.VarChar, 255)
                        {
                            Direction = ParameterDirection.Output
                        };
                        var AesKeyParam = new MySqlParameter("@AesKey", MySqlDbType.LongText)
                        {
                            Value = Secrets.EncryptionKey
                        };
                        var AesIVParam = new MySqlParameter("@AesIV", MySqlDbType.LongText)
                        {
                            Value = Secrets.EncryptionIV
                        };

                        command.Parameters.Add(inputQuestions);
                        command.Parameters.Add(inputOrgSignature);
                        command.Parameters.Add(inputCreationUser);
                        command.Parameters.Add(inputMetaData);
                        command.Parameters.Add(inputOrganizationId);
                        command.Parameters.Add(returnStatusParam);
                        command.Parameters.Add(errorMessageParam);
                        command.Parameters.Add(AesKeyParam);
                        command.Parameters.Add(AesIVParam);

                        await _commonService._unitOfWork.OpenConnectionAsync();

                        await command.ExecuteNonQueryAsync();

                        int returnStatus = Convert.ToInt32(returnStatusParam.Value);
                        string errorMessage = Convert.ToString(errorMessageParam.Value);

                        if (returnStatus == 0)
                        {
                            await transaction.RollbackAsync();

                            return _commonService._apiResponse.GetApiResponse(
                                CustomCodeStatus.InternalServerError,
                                HttpStatusCode.InternalServerError,
                                $"{Resource.ErrorOccurred}: {errorMessage}"
                            );
                        }

                        await transaction.CommitAsync();

                        if (returnStatus == 1 && _cache.TryGetValue(cacheKey, out FileDetails cachedFileDetails))
                        {
                            var fileEntity = _mapper.Map<FileDetails>(cachedFileDetails);

                            await _commonService._unitOfWork.Repository<FileDetails, long>().AddAsync(fileEntity);

                            await _commonService._unitOfWork.Complete();
                        }

                        return _commonService._apiResponse.GetApiResponse(
                            CustomCodeStatus.Success,
                            HttpStatusCode.OK,
                            Resource.QuestionsAddedSuccessfully
                        );
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.InternalServerError,
                        HttpStatusCode.InternalServerError,
                        $"{Resource.Error}: {ex.Message}"
                    );
                }
            }
        }

        // HANDLE FILE UPLOAD
        public async Task<IApiResponse> HandleFileUploadAsync(IFormFile formFile, string questionTypeNames)
        {
            if (formFile == null || formFile.Length == 0)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.FileNotProvidedOrEmpty);

            // Step 1: Save the file to a temporary location
            var tempFilePath = await SaveFileToTempLocationAsync(formFile);

            // Step 2: Determine the file type
            string fileType = GetFileType(tempFilePath);

            // Step 3: Generate file details and cache them
            var fileDetails = await GenerateAndCacheFileDetailsAsync(tempFilePath, fileType, formFile.FileName);

            // Step 4: Map file details to DTO
            var fileDto = MapFileDetailsToDto(fileDetails, fileType);

            // Step 5: Check for existing similar files
            var existingFiles = await GetExistingSimilarFilesAsync(fileDto);

            // Step 6: Calculate similarity percentages
            var similarityFiles = CalculateSimilarities(fileDto, existingFiles, fileType);

            // Step 7: Prepare uploaded file and similarities
            var uploadFileAndSimilarities = PrepareUploadedFileAndSimilarities(formFile.FileName, similarityFiles);

            // Step 8: Parse questions from the file
            var questions = await ParseQuestionsFromFile(tempFilePath);

            // Step 9: Validate questions and prepare warnings
            var _warnings = ValidateQuestions(questions, questionTypeNames);

            // Step 10: Clean up temporary file
            File.Delete(tempFilePath);

            // Step 11: Prepare and return the response
            return PrepareResponse(_warnings, questions, uploadFileAndSimilarities);
        }


        #region Helper Methods
        private static UploadQuestionDetailsDto ExtractChoiceQuestionFromExcel(ReadExcelFile data, HashSet<string> existingCodesInFile)
        {
            var warningMessages = new List<string>();

            if (existingCodesInFile.Contains(data.QuestionCode))
                warningMessages.Add(Resource.QuestionCodeAlreadyExistsInFile);
            else
                existingCodesInFile.Add(data.QuestionCode);

            var encodedChoices = data.Choices?.Select(c => new UploadChoicesDto
            {
                ChoiceText = WebUtility.HtmlEncode(c.ChoiceText),
                IsCorrectAnswer = c.IsCorrectAnswer
            }).ToList() ?? [];

            var mcqQuestion = new UploadQuestionDetailsDto
            {
                Body = WebUtility.HtmlEncode(data.QuestionBody),
                ModelAnswer = string.Join(
                    ", ",
                    encodedChoices
                        .Where(x => x.IsCorrectAnswer)
                        .Select(x => x.ChoiceText)
                ),
                Choices = encodedChoices,
                QuestionType = data.Type,
                ItemBankCode = data.ItemBankCode,
                QuestionCode = data.QuestionCode,
                HasWarning = warningMessages.Count > 0,
                Warning = string.Join("; ", warningMessages)
            };

            return mcqQuestion;
        }

        private static UploadQuestionDetailsDto ExtractQuestionsFromExcelEssay(ReadExcelFile data, HashSet<string> existingCodesInFile)
        {
            var warningMessages = new List<string>();

            if (existingCodesInFile.Contains(data.QuestionCode))
                warningMessages.Add(Resource.QuestionCodeAlreadyExistsInFile);
            else
                existingCodesInFile.Add(data.QuestionCode);

            var encodedChoices = data.Choices?.Select(c => new UploadChoicesDto
            {
                ChoiceText = WebUtility.HtmlEncode(c.ChoiceText),
                IsCorrectAnswer = c.IsCorrectAnswer
            }).ToList() ?? [];

            var essayQuestion = new UploadQuestionDetailsDto
            {
                Body = WebUtility.HtmlEncode(data.QuestionBody),
                ModelAnswer = WebUtility.HtmlEncode(data.ModelAnswer),
                Choices = encodedChoices,
                QuestionType = data.Type,
                ItemBankCode = data.ItemBankCode,
                QuestionCode = data.QuestionCode,
                HasWarning = warningMessages.Count > 0,
                Warning = string.Join("; ", warningMessages),
            };

            return essayQuestion;
        }

        private static async Task<string> SaveFileToTempLocationAsync(IFormFile formFile)
        {
            var tempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + System.IO.Path.GetExtension(formFile.FileName));

            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await formFile.CopyToAsync(stream);
            }

            return tempFilePath;
        }

        private static string GetFileType(string filePath)
        {
            return System.IO.Path.GetExtension(filePath).ToLower() switch
            {
                ".docx" => "word",
                ".xlsx" => "excel",
                ".pdf" => "pdf",
                ".txt" => "qti",
                _ => throw new NotSupportedException(Resource.Unsupportedfileformat)
            };
        }

        private async Task<FileDetails> GenerateAndCacheFileDetailsAsync(string tempFilePath, string fileType, string fileName)
        {
            var fileDetails = await GenerateFileDetailsAsync(tempFilePath, fileType, fileName);

            _cache.Set(cacheKey, fileDetails, TimeSpan.FromHours(1));

            return fileDetails;
        }

        private static FileDetailsDto MapFileDetailsToDto(FileDetails fileDetails, string fileType)
        {
            return new FileDetailsDto
            {
                FileName = fileDetails.FileName,
                Authors = fileDetails.Authors,
                Pages = fileDetails.Pages,
                Size = fileDetails.Size,
                DateCreated = fileDetails.DateCreated,
                DateModified = fileDetails.DateModified,
                WordCount = fileDetails.WordCount,
                LineCount = fileDetails.LineCount,
                FileType = fileType
            };
        }

        private static List<FileNameAndProbability> CalculateSimilarities(FileDetailsDto fileDto, List<FileDetails> existingFiles, string fileType)
        {
            var similarityFiles = new List<FileNameAndProbability>();

            foreach (var databaseFile in existingFiles)
            {
                double similarityPercentage = CalculateSimilarity(fileDto, databaseFile, fileType);

                similarityFiles.Add(new FileNameAndProbability
                {
                    FileName = databaseFile.FileName,
                    Probability = similarityPercentage,
                    CreationDate = databaseFile.CreationDate
                });
            }

            return similarityFiles;
        }

        private static UploadedFileAndSimilarities PrepareUploadedFileAndSimilarities(string fileName, List<FileNameAndProbability> similarityFiles)
        {
            return new UploadedFileAndSimilarities
            {
                UploadedFileName = fileName,
                FileNameAndProbabilities = similarityFiles
            };
        }

        private List<string> ValidateQuestions(List<UploadQuestionDetailsDto> questions, string questionTypeNames)
        {
            var questionTypesMetaData = JsonSerializer.Deserialize<List<string>>(questionTypeNames, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var questionTypes = _commonService._unitOfWork.Repository<Core.Entities.QuestionType, long>().Query(false, false).ToList();

            var existingCodes = _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .AsNoTracking()
                .Select(q => q.Code)
                .ToHashSet();

            var itemBankList = _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .Query()
                .AsNoTracking()
                .Select(ib => new { ib.Code, ib.Id })
                .ToList();

            var itemBankDict = itemBankList
                .Where(ib => !string.IsNullOrWhiteSpace(ib.Code))
                .GroupBy(ib => ib.Code, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().Id,
                    StringComparer.OrdinalIgnoreCase
                );

            bool hasItemBankColumn = questions.Any(q => !string.IsNullOrWhiteSpace(q.ItemBankCode));

            foreach (var q in questions)
            {
                var matchingQuestionType = questionTypes.Find(qt => qt.Name.Equals(q.QuestionType));

                q.QuestionTypeId = matchingQuestionType?.Id ?? 0;

                if (!questionTypesMetaData.Contains(q.QuestionType))
                {
                    q.Warning = Resource.SelectedQuestionTypesDoNotMatchTypesInFile;
                }

                if (string.IsNullOrWhiteSpace(q.QuestionCode))
                {
                    q.Warning = Resource.QuestionCodeCannotBeEmpty;
                }
                else if (existingCodes.Contains(q.QuestionCode))
                {
                    q.Warning = Resource.QuestionCodeAlreadyExists;
                }

                if (hasItemBankColumn)
                {
                    if (string.IsNullOrWhiteSpace(q.ItemBankCode))
                    {
                        q.Warning = Resource.ItemBankCodeNotFound;
                    }
                    else if (!itemBankDict.TryGetValue(q.ItemBankCode, out var itemBankId))
                    {
                        q.Warning = Resource.ItemBankCodeNotFound;
                    }
                    else
                    {
                        q.ItemBankId = itemBankId;
                    }
                }

                switch (q.QuestionType)
                {
                    case nameof(QuestionType.MCQ):
                    case nameof(QuestionType.TrueAndFalse):
                    case nameof(QuestionType.MultipleCorrectAnswers):
                        if (q.Choices == null || !q.Choices.Any())
                            q.Warning = Resource.QuestionMustContainChoices;
                        if (IsBodyEmptyOrWhitespace(q.Body))
                            q.Warning = Resource.QuestionBodyCannotBeEmpty;
                        break;

                    case nameof(QuestionType.Essay):
                    case nameof(QuestionType.SegmentWithAudioAnswer):
                    case nameof(QuestionType.SegmentWithVideoAnswer):
                        if (IsBodyEmptyOrWhitespace(q.Body))
                            q.Warning = Resource.QuestionBodyCannotBeEmpty;
                        if (string.IsNullOrWhiteSpace(q.ModelAnswer))
                            q.WarningInfo = Resource.QuestionWithoutModelAnswer;
                        break;

                    default:
                        q.Warning = string.Format(Resource.UnexpectedQuestionType, q.QuestionType);
                        break;
                }

                if (!string.IsNullOrWhiteSpace(q.Warning))
                {
                    warnings.Add(q.Warning);
                    q.HasWarning = true;
                }
                else
                {
                    q.HasWarning = false;
                }
            }

            return warnings;
        }

        private static bool IsBodyEmptyOrWhitespace(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return true;

            if (body.Contains("<img", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("<video", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("<audio", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            string cleanedText = Regex.Replace(body, @"<[^>]+>", "").Trim();
            return string.IsNullOrWhiteSpace(cleanedText);
        }

        private ApiResponse PrepareResponse(List<string> warnings, List<UploadQuestionDetailsDto> questions, UploadedFileAndSimilarities uploadFileAndSimilarities)
        {
            if (questions.Count != 0)
            {
                var responseData = new
                {
                    Warnings = warnings,
                    Questions = questions,
                    FileDetailsProbabilities = uploadFileAndSimilarities,
                };

                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.GetData, responseData);
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure, HttpStatusCode.NotFound, Resource.InvalidQuestionMetadataId, warnings);
        }

        private async Task<List<FileDetails>> GetExistingSimilarFilesAsync(FileDetailsDto fileDto)
        {
            var existingFiles = await _commonService
                ._unitOfWork
                .Repository<FileDetails, long>()
                .GetAllAsync(result => (result.Size == fileDto.Size ||
                                        result.Authors.Equals(fileDto.Authors) ||
                                        result.DateCreated.Date == fileDto.DateCreated.Date ||
                                        result.DateModified.Date == fileDto.DateModified.Date ||
                                        result.FileName == fileDto.FileName) &&
                                        result.FileType == fileDto.FileType);

            return existingFiles.ToList();
        }

        private static async Task<FileDetails> GenerateFileDetailsAsync(string filePath, string fileType, string fileName)
        {
            FileDetails fileDetails = new()
            {
                FileName = fileName,
                FileType = fileType,
                Title = string.Empty,
                Size = new FileInfo(filePath).Length,
                DateAccessed = File.GetLastAccessTime(filePath)
            };

            if (fileType.Equals("word", StringComparison.OrdinalIgnoreCase))
            {
                using var wordDoc = WordprocessingDocument.Open(filePath, false);
                var props = wordDoc.PackageProperties;
                fileDetails.Authors = props.Creator;
                fileDetails.LastSavedBy = props.LastModifiedBy;
                fileDetails.RevisionNumber = long.TryParse(props.Revision, out var revision) ? revision : 0;
                var docProps = wordDoc.ExtendedFilePropertiesPart.Properties;
                fileDetails.Pages = docProps?.Pages?.Text != null ? long.Parse(docProps.Pages.Text) : null;
                fileDetails.WordCount = docProps?.Words?.Text != null ? long.Parse(docProps.Words.Text) : null;
                fileDetails.CharacterCount = docProps?.Characters?.Text != null ? long.Parse(docProps.Characters.Text) : null;
                fileDetails.ParagraphCount = docProps?.Paragraphs?.Text != null ? long.Parse(docProps.Paragraphs.Text) : null;
                fileDetails.LineCount = docProps?.Lines?.Text != null ? long.Parse(docProps.Lines.Text) : null;
                fileDetails.DateCreated = props.Created.Value;
                fileDetails.DateModified = props.Modified.Value;
            }
            else if (fileType.Equals("excel", StringComparison.OrdinalIgnoreCase))
            {
                SpreadsheetDocument excelDoc = SpreadsheetDocument.Open(filePath, false);
                var props = excelDoc.PackageProperties;
                fileDetails.Authors = props.Creator;
                fileDetails.LastSavedBy = props.LastModifiedBy;
                fileDetails.Pages = null;
                fileDetails.DateCreated = props.Created.Value;
                fileDetails.DateModified = props.Modified.Value;
            }
            else if (fileType.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                UglyToad.PdfPig.PdfDocument pdfDocument = UglyToad.PdfPig.PdfDocument.Open(filePath);
                var props = pdfDocument.Information;
                fileDetails.DateCreated = ConvertPdfDateToDateTime(props.CreationDate);
                fileDetails.DateModified = ConvertPdfDateToDateTime(props.ModifiedDate);
                fileDetails.FileName = fileName;
                fileDetails.Authors = props.Author;
            }
            else if (fileType.Equals("qti", StringComparison.OrdinalIgnoreCase))
            {
                FileInfo fileInfo = new(filePath);
                fileDetails.DateCreated = fileInfo.CreationTime;
                fileDetails.DateModified = fileInfo.LastWriteTime;
                fileDetails.Size = fileInfo.Length;
            }

            await Task.CompletedTask;

            return fileDetails;
        }

        // Computes the similarity percentage between two files by comparing attributes based on the file type ("word" , "excel" , "pdf" and "qti" )
        private static double CalculateSimilarity(FileDetailsDto newFile, FileDetails existingFile, string fileType)
        {
            int matchingFields = 0;

            int totalFields = 7;

            if (fileType == "word")
            {
                if (newFile.FileName == existingFile.FileName) matchingFields++;
                if (newFile.Pages == existingFile.Pages) matchingFields++;
                if (newFile.WordCount == existingFile.WordCount) matchingFields++;
                if (newFile.LineCount == existingFile.LineCount) matchingFields++;
                if (newFile.DateCreated.Date == existingFile.DateCreated.Date) matchingFields++;
                if (newFile.DateModified.Date == existingFile.DateModified.Date) matchingFields++;
                if (newFile.Authors.Equals(existingFile.Authors, StringComparison.OrdinalIgnoreCase)) matchingFields++;
            }
            else if (fileType == "excel")
            {
                totalFields = 5;

                if (newFile.DateCreated.Date == existingFile.DateCreated.Date) matchingFields++;
                if (newFile.DateModified.Date == existingFile.DateModified.Date) matchingFields++;
                if (newFile.Size == existingFile.Size) matchingFields++;
                if (newFile.Authors == existingFile.Authors) matchingFields++;
                if (newFile.FileName == existingFile.FileName) matchingFields++;
            }
            else if (fileType.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                totalFields = 5;

                if (newFile.FileName == existingFile.FileName) matchingFields++;
                if (newFile.Size == existingFile.Size) matchingFields++;
                if (newFile.DateCreated.Date == existingFile.DateCreated.Date) matchingFields++;
                if (newFile.DateModified.Date == existingFile.DateModified.Date) matchingFields++;
                if (newFile.Authors == existingFile.Authors) matchingFields++;
            }
            else if (fileType.Equals("qti", StringComparison.OrdinalIgnoreCase))
            {
                totalFields = 4;

                if (newFile.FileName == existingFile.FileName) matchingFields++;
                if (newFile.Size == existingFile.Size) matchingFields++;
                if (newFile.DateCreated.Date == existingFile.DateCreated.Date) matchingFields++;
                if (newFile.DateModified.Date == existingFile.DateModified.Date) matchingFields++;
            }

            return Math.Round((matchingFields / (double)totalFields) * 100, 2);
        }

        private static DateTime ConvertPdfDateToDateTime(string pdfDate)
        {
            if (string.IsNullOrEmpty(pdfDate))
                return DateTime.MinValue;

            if (pdfDate.StartsWith("D:"))
                pdfDate = pdfDate.Substring(2);

            if (DateTime.TryParseExact(pdfDate.Substring(0, 14), "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                return result;

            return DateTime.MinValue;
        }

        private static string GetFileExtensionFromContentType(string contentType)
        {
            return contentType switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "image/gif" => ".gif",
                "image/bmp" => ".bmp",
                "image/tiff" => ".tiff",
                _ => ".bin"
            };
        }

        private static List<byte[]> ExtractImagesFromPdf(PdfReader reader, int pageNumber)
        {
            List<byte[]> images = [];

            var page = reader.GetPageN(pageNumber);
            var resources = page.GetAsDict(PdfName.RESOURCES);
            var xObject = resources.GetAsDict(PdfName.XOBJECT);

            if (xObject != null)
            {
                foreach (var item in xObject.Keys)
                {
                    var obj = xObject.Get(item);

                    if (obj is PRIndirectReference indirectReference)
                    {
                        obj = PdfReader.GetPdfObject(indirectReference);
                    }

                    if (obj is PRStream stream)
                    {
                        var subtype = stream.GetAsName(PdfName.SUBTYPE);

                        if (subtype.Equals(PdfName.IMAGE))
                        {
                            byte[] imageBytes = PdfReader.GetStreamBytesRaw(stream);

                            if (imageBytes != null && imageBytes.Length > 0)
                            {
                                images.Add(imageBytes);
                            }
                        }
                    }
                }
            }

            return images;
        }

        private List<ReadExcelFile> ReadDataFromExcel(string filePath)
        {
            // Register encoding provider for code page 1252 (Windows-1252)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var data = new List<ReadExcelFile>();

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var headerRow = ReadHeaderRow(reader);

                if (headerRow == null) return data;

                ReadExcelFile currentQuestion = null;

                while (reader.Read())
                {
                    var questionBody = GetCellValue(reader, headerRow, "QuestionBody");
                    var choiceText = GetCellValue(reader, headerRow, "Choice");
                    var type = GetCellValue(reader, headerRow, "QuestionType");
                    var correct = GetCellValue(reader, headerRow, "CorrectAnswer");
                    var itembank = GetCellValue(reader, headerRow, "ItemBankCode");
                    var questionCode = GetCellValue(reader, headerRow, "QuestionCode");

                    // New question starts when Code is present (QuestionBody may be empty)
                    var isNewQuestion = !string.IsNullOrEmpty(questionCode) && !string.IsNullOrEmpty(type);

                    if (isNewQuestion)
                    {
                        // Add the previous question to the list if it exists
                        if (currentQuestion != null)
                        {
                            data.Add(currentQuestion);
                        }

                        // Create a new question (QuestionBody can be empty - will be caught in validation)
                        currentQuestion = new ReadExcelFile
                        {
                            QuestionBody = questionBody,
                            ModelAnswer = correct,
                            Type = type,
                            Choices = [],
                            ItemBankCode = itembank,
                            QuestionCode = questionCode
                        };
                    }

                    // Add choices to the current question
                    if (currentQuestion != null && !string.IsNullOrEmpty(choiceText))
                    {
                        var choice = new UploadChoicesDto
                        {
                            ChoiceText = choiceText,
                            IsCorrectAnswer = correct == "*"
                        };

                        currentQuestion.Choices.Add(choice);
                    }
                }
                if (currentQuestion != null)
                {
                    data.Add(currentQuestion);
                }
            }

            return data;
        }

        private static long GenerateUniqueLongId()
        {
            return DateTimeHelper.Now.Ticks;
        }

        private static byte[] GetImageBytes(ImagePart imagePart)
        {
            using (var stream = imagePart.GetStream())
            {
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }

        private static string GetContentTypeFromImage(byte[] imageData)
        {
            // Use the file header to determine the MIME type
            if (imageData.Length >= 4 && imageData[0] == 0xFF && imageData[1] == 0xD8)
                return "image/jpeg";
            if (imageData.Length >= 8 && imageData[0] == 0x89 && imageData[1] == 0x50)
                return "image/png";
            if (imageData.Length >= 6 && imageData[0] == 0x47 && imageData[1] == 0x49)
                return "image/gif";
            if (imageData.Length >= 2 && imageData[0] == 0x42 && imageData[1] == 0x4D)
                return "image/bmp";
            if (imageData.Length >= 4 && imageData[0] == 0x49 && imageData[1] == 0x49)
                return "image/tiff";

            throw new NotSupportedException(Resource.UnsupportedImageFormat);
        }

        private static string SaveImageToFileSystem(byte[] imageBytes, string fileExtension)
        {
            var fileName = $"{Guid.NewGuid()}{fileExtension}";

            var directoryPath = System.IO.Path.Combine("wwwroot", "images", "UploadFile");

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            var filePath = System.IO.Path.Combine(directoryPath, fileName);

            File.WriteAllBytes(filePath, imageBytes);

            return filePath;
        }

        private static List<string>? ReadHeaderRow(IExcelDataReader reader)
        {
            if (reader.Read())
            {
                var headerRow = new List<string>();

                for (int col = 0; col < reader.FieldCount; col++)
                {
                    headerRow.Add(reader.GetValue(col)?.ToString() ?? string.Empty);
                }

                return headerRow;
            }
            return null;
        }

        private static string GetCellValue(IExcelDataReader reader, List<string> headerRow, string columnName)
        {
            int columnIndex = headerRow.IndexOf(columnName);

            if (columnIndex >= 0 && columnIndex < reader.FieldCount)
            {
                return reader.GetValue(columnIndex)?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }

        private static void AppendItemBankToDocument(Body body, ItemBank itemBank, IEnumerable<QuestionMetadata> questionMetadata, long languageId)
        {
            var itemBankParagraph = new Paragraph(
                new Run(new Text(itemBank.Name))
            );

            body.AppendChild(itemBankParagraph);

            var itemBankQuestions = questionMetadata.Where(meta => meta.ItemBankId == itemBank.Id);

            foreach (var meta in itemBankQuestions)
            {
                foreach (var detail in meta.QuestionDetails.Where(qd => qd.LanguageId == languageId))
                {
                    var questionParagraph = new Paragraph(
                        new ParagraphProperties(
                            new NumberingProperties(
                                new NumberingLevelReference { Val = 0 },
                                new NumberingId { Val = 1 }
                            )
                        ),
                        new Run(new Text($"{detail.Body}"))
                    );

                    body.AppendChild(questionParagraph);

                    if (meta.QuestionType?.Name == "MCQ" && detail.QuestionsChoices != null)
                    {
                        foreach (var choice in detail.QuestionsChoices)
                        {
                            var choiceParagraph = new Paragraph(
                                new ParagraphProperties(
                                    new NumberingProperties(
                                        new NumberingLevelReference { Val = 1 },
                                        new NumberingId { Val = 2 }
                                    )
                                ),
                                new Run(new Text($"{choice.ChoiceText}{(choice.IsCorrectAnswer ? "*" : "")}"))
                            );

                            body.AppendChild(choiceParagraph);
                        }
                    }
                }
            }

            foreach (var childItemBank in itemBank.Childreen)
            {
                AppendItemBankToDocument(body, childItemBank, questionMetadata, languageId);
            }
        }

        public async Task<ApiResponse> UploadCandidatesExcelFileAsync(IFormFile excelFile, string fileName, CancellationToken cancellationToken = default)
        {
            if (excelFile == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.NoFileUploadedOrEmpty
                );
            }

            var allowedExtensions = new List<string> { ".xlsx", ".xls" };

            var fileExtension = System.IO.Path.GetExtension(excelFile.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.OnlyExcelFilesAllowed
                );
            }

            var foldersApiResponse = await _docLibBackEndHttpClient.GetAsync<NewApiResponse<OrganizationPredefinedFoldersResponseDto>>("Folder/EnsureOrganizationPredefinedFoldersExist", cancellationToken);

            if (!foldersApiResponse.Success)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    foldersApiResponse.Message
                );
            }

            var folderId = foldersApiResponse.Data.CandidatesExcelFilesFolderId;

            var boundary = $"----WebKitFormBoundary{DateTimeHelper.Now.Ticks:X}";

            using var formData = new MultipartFormDataContent(boundary);

            await using var memoryStream = new MemoryStream();

            await excelFile.CopyToAsync(memoryStream, cancellationToken);

            memoryStream.Position = 0;

            var fileContent = new StreamContent(memoryStream);

            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(excelFile.ContentType);

            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"formFile\"",
                FileName = $"\"{fileName}\""
            };

            formData.Add(fileContent);

            formData.Add(new StringContent(folderId.ToString()), "folderId");

            var documentUploadApiResponse = await _docLibBackEndHttpClient.PostMultipartAsync<NewApiResponse<DocumentMetadataResponseDto>>(
                "Document/Upload",
                formData,
                cancellationToken
            );

            if (!documentUploadApiResponse.Success)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    documentUploadApiResponse.Message
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.CandidateExcelFileUploadedSuccessfully,
                documentUploadApiResponse.Data
            );
        }

        private static bool IsTrueFalseQuestion(List<UploadChoicesDto> choices)
        {
            if (choices.Count != 2) return false;

            int matchCount = 0;
            foreach (var choice in choices)
            {
                var cleanText = Regex.Replace(choice.ChoiceText, @"<[^>]+>", "").Trim();
                cleanText = Regex.Replace(cleanText, @"[^\w\s]", "").Trim();

                if (TrueFalseValidationConstants.AllValidKeywords.Contains(cleanText))
                {
                    matchCount++;
                }
            }

            return matchCount == 2;
        }

        private static string PreserveWhitespace(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            text = text.Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;");

            text = Regex.Replace(text, " {2,}", m => " " + string.Concat(Enumerable.Repeat("&nbsp;", m.Value.Length - 1)));

            return text;
        }
        #endregion Helper Methods
    }
}