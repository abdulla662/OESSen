using OES.Helper;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.QuestionChoices;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Net;
using System.Text.RegularExpressions;

namespace OES.Services.PdfGeneratorServiceDocuments
{
    public class PaperFormDocument : IDocument
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GetPaperWithFormsDto _paperWithForms;
        private readonly Dictionary<string, byte[]> _imageCache = [];
        private static readonly TimeSpan _httpTimeout = TimeSpan.FromSeconds(30);
        private readonly bool _includeDetails;

        public PaperFormDocument(GetPaperWithFormsDto paperWithFormsDto, bool includeDetails, IHttpClientFactory httpClientFactory)
        {
            _paperWithForms = paperWithFormsDto;
            _includeDetails = includeDetails;
            _httpClientFactory = httpClientFactory;
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Cairo"));

                if (_paperWithForms.LanguageDirection == "RTL")
                    page.ContentFromRightToLeft();

                page.Content().Column(mainCol =>
                {
                    #region Paper Header

                    mainCol.Item()
                       .Border(1)
                       .BorderColor(Colors.Grey.Lighten2)
                       .Padding(10)
                       .Column(col =>
                       {
                           col.Item().PaddingTop(10).Table(table =>
                           {
                               table.ColumnsDefinition(columns =>
                               {
                                   columns.RelativeColumn();
                                   columns.RelativeColumn();
                               });

                               table.Cell().Text(t =>
                               {
                                   t.Span("🅰").FontFamily("Noto Color Emoji").FontColor("#009bd9");
                                   t.Span($"{Resource.Name}: ").FontColor("#616161").SemiBold();
                                   t.Span(FormatTextBasedOnLayout(_paperWithForms.Name)).FontColor("#024077");
                               });

                               table.Cell().Text(t =>
                               {
                                   t.Span("#").FontFamily("Noto Color Emoji").FontColor("#009bd9");
                                   t.Span($"{Resource.Code}: ").FontColor("#616161").SemiBold();
                                   t.Span(FormatTextBasedOnLayout(_paperWithForms.Code)).FontColor("#024077");
                               });

                               table.Cell().Text(t =>
                               {
                                   t.Span("📝").FontFamily("Noto Color Emoji").FontColor("#009bd9");
                                   t.Span($"{Resource.Description}: ").FontColor("#616161").SemiBold();
                                   t.Span(FormatTextBasedOnLayout(_paperWithForms.Description)).FontColor("#024077");
                               });

                               table.Cell().Text(t =>
                               {
                                   t.Span("☰").FontFamily("Noto Color Emoji").FontColor("#009bd9");
                                   t.Span($"{Resource.QuestionCount}: ").FontColor("#616161").SemiBold();
                                   t.Span(FormatTextBasedOnLayout(_paperWithForms.QuestionsCount.ToString())).FontColor("#024077");
                               });

                               table.Cell().Text(t =>
                               {
                                   t.Span("🕐").FontFamily("Noto Color Emoji").FontColor("#009bd9");
                                   t.Span($"{Resource.Duration}: ").FontColor("#616161").SemiBold();
                                   t.Span(FormatTextBasedOnLayout(_paperWithForms.Duration.ToString())).FontColor("#024077");
                               });

                               table.Cell().Text(t =>
                               {
                                   t.Span("☑").FontFamily("Noto Color Emoji").FontColor("#009bd9");
                                   t.Span($"{Resource.TotalMarks}: ").FontColor("#616161").SemiBold();
                                   t.Span(FormatTextBasedOnLayout(_paperWithForms.TotalMarks.ToString())).FontColor("#024077");
                               });
                           });
                       });

                    #endregion

                    var currentForm = _paperWithForms.Forms.FirstOrDefault();

                    if (currentForm == null)
                        return;

                    #region Form Header

                    mainCol.Item()
                           .PaddingVertical(10)
                           .Border(1)
                           .BorderColor(Colors.Grey.Lighten2)
                           .Background(Colors.Grey.Lighten5)
                           .Padding(8)
                           .Row(row =>
                           {
                               row.Spacing(16);

                               row.AutoItem().Text(t =>
                               {
                                   t.Span("📄").FontFamily("Noto Color Emoji").FontColor("#009bd9");
                                   t.Span(currentForm.FormName).FontColor("#009bd9");
                               });

                               row.AutoItem().Text(t =>
                               {
                                   t.Span("📄").FontFamily("Noto Color Emoji").FontColor("#009bd9");
                                   t.Span(currentForm.FormCode).FontColor("#009bd9");
                               });

                               row.AutoItem().Text(t =>
                               {
                                   t.Span("📄").FontFamily("Noto Color Emoji").FontColor("#009bd9");
                                   t.Span(currentForm.FormDescription).FontColor("#009bd9");
                               });
                           });

                    #endregion

                    foreach (var question in currentForm.Questions)
                    {
                        mainCol.Item().PaddingBottom(15).Column(qCol =>
                        {
                            #region Question Details

                            if (_includeDetails)
                            {
                                qCol.Item()
                                    .Background(Colors.Grey.Lighten5)
                                    .Padding(6)
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                        });

                                        table.Cell().Text(t =>
                                        {
                                            t.Span("#").FontFamily("Noto Color Emoji").FontColor("#009bd9").FontSize(8);
                                            t.Span($"{Resource.Code}: ").FontColor("#616161").SemiBold().FontSize(8);
                                            t.Span(FormatTextBasedOnLayout(question.Metadata.Code)).FontSize(8).FontColor("#024077");
                                        });

                                        table.Cell().Text(t =>
                                        {
                                            t.Span("📘").FontFamily("Noto Color Emoji").FontColor("#009bd9").FontSize(8);
                                            t.Span($"{Resource.Subject}: ").FontColor("#616161").SemiBold().FontSize(8);
                                            t.Span(FormatTextBasedOnLayout(question.Metadata.Subject)).FontSize(8).FontColor("#024077");
                                        });

                                        table.Cell().Text(t =>
                                        {
                                            t.Span("❓").FontFamily("Noto Color Emoji").FontColor("#009bd9").FontSize(8);
                                            t.Span($"{Resource.Type}: ").FontColor("#616161").SemiBold().FontSize(8);
                                            t.Span(FormatTextBasedOnLayout(question.Metadata.TypeDisplay)).FontSize(8).FontColor("#024077");
                                        });

                                        table.Cell().Text(t =>
                                        {
                                            t.Span("🗂").FontFamily("Noto Color Emoji").FontColor("#009bd9").FontSize(8);
                                            t.Span($"{Resource.Category}: ").FontColor("#616161").SemiBold().FontSize(8);
                                            t.Span(FormatTextBasedOnLayout(question.Metadata.Category)).FontSize(8).FontColor("#024077");
                                        });

                                        table.Cell().Text(t =>
                                        {
                                            t.Span("☑").FontFamily("Noto Color Emoji").FontColor("#009bd9").FontSize(8);
                                            t.Span($"{Resource.Score}: ").FontColor("#616161").SemiBold().FontSize(8);
                                            t.Span(FormatTextBasedOnLayout(question.Score.ToString())).FontSize(8).FontColor("#024077");
                                        });

                                        table.Cell().Text(t =>
                                        {
                                            t.Span("📋").FontFamily("Noto Color Emoji").FontColor("#009bd9").FontSize(8);
                                            t.Span($"{Resource.PaperQuestionStatus}: ").FontColor("#616161").SemiBold().FontSize(8);
                                            t.Span(FormatTextBasedOnLayout(question.PaperQuestionStatus.ToLocalizedString())).FontSize(8).FontColor("#024077");
                                        });

                                        table.Cell().Text(t =>
                                        {
                                            t.Span("🕐").FontFamily("Noto Color Emoji").FontColor("#009bd9").FontSize(8);
                                            t.Span($"{Resource.SectionName}: ").FontColor("#616161").SemiBold().FontSize(8);
                                            t.Span(FormatTextBasedOnLayout(question.SectionName)).FontSize(8).FontColor("#024077");
                                        });
                                    });
                            }

                            #endregion

                            foreach (var data in question.Metadata.QuestionData)
                            {
                                RenderQuestionBody(qCol, data, isBold: true, bodyHeight: 160);

                                if (question.Metadata.Type.Equals(nameof(QuestionType.MCQ), StringComparison.OrdinalIgnoreCase) ||
                                    question.Metadata.Type.Equals(nameof(QuestionType.MultipleCorrectAnswers), StringComparison.OrdinalIgnoreCase) ||
                                    question.Metadata.Type.Equals(nameof(QuestionType.TrueAndFalse), StringComparison.OrdinalIgnoreCase)
                                )
                                {
                                    RenderChoices(qCol, data.Choices ?? [], leftPadding: 15, choiceImageHeight: 120);
                                }
                            }

                            #region SubQuestions

                            if (question.Metadata.SubQuestions?.Any() == true)
                            {
                                qCol.Item().PaddingTop(10);

                                bool isMatchingPairs = question.Metadata.Type?.Equals(QuestionType.MatchingPairs.ToString(), StringComparison.OrdinalIgnoreCase) == true;

                                if (isMatchingPairs)
                                {
                                    RenderMatchingPairs(qCol, question.Metadata.SubQuestions);
                                }
                                else
                                {
                                    RenderComprehensionSubQuestions(qCol, question.Metadata.SubQuestions);
                                }
                            }

                            #endregion
                        });
                    }
                });
            });
        }

        #region HelperFunctions
        public async Task PreloadImagesAsync()
        {
            var imageUrls = _paperWithForms
                .Forms
                .SelectMany(f => f.Questions)
                .SelectMany(q =>
                {
                    var urls = new List<string>();

                    foreach (var d in q.Metadata.QuestionData)
                    {
                        urls.AddRange(ParseHtml(d.Body)?.ImageUrls ?? Enumerable.Empty<string>());

                        if (d.Choices != null)
                        {
                            foreach (var choice in d.Choices)
                            {
                                urls.AddRange(ParseHtml(choice.ChoiceText)?.ImageUrls ?? Enumerable.Empty<string>());
                            }
                        }
                    }

                    if (q.Metadata.SubQuestions != null)
                    {
                        foreach (var subQ in q.Metadata.SubQuestions)
                        {
                            foreach (var d in subQ.QuestionData)
                            {
                                urls.AddRange(ParseHtml(d.Body)?.ImageUrls ?? Enumerable.Empty<string>());

                                if (d.Choices != null)
                                {
                                    foreach (var choice in d.Choices)
                                    {
                                        urls.AddRange(ParseHtml(choice.ChoiceText)?.ImageUrls ?? Enumerable.Empty<string>());
                                    }
                                }
                            }
                        }
                    }

                    return urls;
                })
                .Distinct()
                .ToList();

            if (imageUrls.Count == 0)
            {
                return;
            }

            var httpClient = _httpClientFactory.CreateClient();

            httpClient.Timeout = _httpTimeout;

            foreach (var url in imageUrls)
            {
                if (_imageCache.ContainsKey(url))
                    continue;

                _imageCache[url] = await httpClient.GetByteArrayAsync(url);
            }
        }

        private static ParsedHtmlContent ParseHtml(string html)
        {
            var result = new ParsedHtmlContent();

            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            var imgRegex = new Regex(
                "<img[^>]*src=[\"']([^\"']+)[\"'][^>]*>",
                RegexOptions.IgnoreCase | RegexOptions.Compiled
            );

            foreach (Match match in imgRegex.Matches(html))
            {
                result.ImageUrls.Add(match.Groups[1].Value);
            }

            var objectRegex = new Regex(
                "<object[^>]*data=[\"']([^\"']+)[\"'][^>]*>",
                RegexOptions.IgnoreCase | RegexOptions.Compiled
            );

            foreach (Match match in objectRegex.Matches(html))
            {
                var dataUrl = match.Groups[1].Value;
                if (!string.IsNullOrWhiteSpace(dataUrl))
                {
                    result.ImageUrls.Add(dataUrl);
                }
            }

            var htmlWithoutImages = Regex.Replace(html, "<img[^>]*>", string.Empty, RegexOptions.IgnoreCase);
            htmlWithoutImages = Regex.Replace(htmlWithoutImages, "<object[^>]*>.*?</object>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var textWithoutTags = Regex.Replace(htmlWithoutImages, "<.*?>", string.Empty);

            result.Text = WebUtility
                .HtmlDecode(textWithoutTags)
                .Replace("\u00A0", " ")
                .Trim();

            return result;
        }

        private string FormatTextBasedOnLayout(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            if (_paperWithForms.LanguageDirection == "RTL")
            {
                return "\u202A" + text + "\u202C";
            }

            return text;
        }

        private void RenderQuestionBody(ColumnDescriptor col, QuestionDataDto data, bool isBold = true, float bodyHeight = 160)
        {
            var parsed = ParseHtml(data.Body);

            if (parsed == null) return;

            if (!string.IsNullOrWhiteSpace(parsed.Text))
            {
                col.Item().PaddingVertical(5).Text(t => t.Span(parsed.Text).FontColor(isBold ? "#114b7e" : "#024077"));
            }

            foreach (var img in parsed.ImageUrls)
            {
                if (_imageCache.TryGetValue(img, out var imageBytes))
                {
                    col.Item().PaddingTop(6).AlignCenter().Height(bodyHeight).Image(imageBytes).FitArea();
                }
            }
        }

        private void RenderChoices(ColumnDescriptor col, List<ChoiceDataDto> choices, int leftPadding = 15, float choiceImageHeight = 120)
        {
            if (choices == null || !choices.Any()) return;

            foreach (var choice in choices)
            {
                var parsed = ParseHtml(choice.ChoiceText);

                col.Item().PaddingLeft(leftPadding).PaddingVertical(3).Row(r =>
                {
                    r.ConstantItem(15).Text("○").FontColor("#024077");
                    r.RelativeItem().Column(choiceCol =>
                    {
                        if (parsed != null)
                        {
                            choiceCol.Item().Text(parsed.Text).FontColor("#024077");
                            foreach (var img in parsed.ImageUrls)
                            {
                                if (_imageCache.TryGetValue(img, out var imageBytes))
                                    choiceCol.Item().PaddingTop(4).Height(choiceImageHeight).Image(imageBytes).FitArea();
                            }
                        }
                    });
                });
            }
        }

        private void RenderMatchingPairs(ColumnDescriptor qCol, List<QuestionMetadataPaginationDto> subQuestions)
        {
            var terms = new List<ParsedHtmlContent>();

            foreach (var subQuestion in subQuestions)
            {
                foreach (var subData in subQuestion.QuestionData)
                {
                    var parsed = ParseHtml(subData.Body);

                    if (parsed != null && (!string.IsNullOrWhiteSpace(parsed.Text) || parsed.ImageUrls.Any()))
                        terms.Add(parsed);
                }
            }

            var allChoices = subQuestions
                .SelectMany(sq => sq.QuestionData)
                .SelectMany(qd => qd.Choices ?? [])
                .Select(c => ParseHtml(c.ChoiceText))
                .Where(p => p != null && (!string.IsNullOrWhiteSpace(p.Text) || p.ImageUrls.Any()))
                .ToList();

            qCol.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem()
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Background(Colors.White)
                    .Padding(10)
                    .Column(leftCol =>
                    {
                        foreach (var term in terms)
                        {
                            leftCol.Item().PaddingBottom(8).Column(termCol =>
                            {
                                if (!string.IsNullOrWhiteSpace(term.Text))
                                {
                                    termCol.Item()
                                        .Text(term.Text)
                                        .FontColor("#024077")
                                        .FontSize(10);
                                }

                                foreach (var img in term.ImageUrls)
                                {
                                    if (_imageCache.TryGetValue(img, out var imageBytes))
                                    {
                                        termCol.Item()
                                            .PaddingTop(4)
                                            .MaxHeight(80)
                                            .AlignCenter()
                                            .Image(imageBytes)
                                            .FitArea();
                                    }
                                }
                            });
                        }
                    });

                row.ConstantItem(15);

                row.RelativeItem()
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Background(Colors.White)
                    .Padding(10)
                    .Column(rightCol =>
                    {
                        foreach (var choice in allChoices)
                        {
                            rightCol.Item().PaddingBottom(8).Column(choiceCol =>
                            {
                                if (!string.IsNullOrWhiteSpace(choice.Text))
                                {
                                    choiceCol.Item()
                                        .Text(choice.Text)
                                        .FontColor("#024077")
                                        .FontSize(10);
                                }

                                foreach (var img in choice.ImageUrls)
                                {
                                    if (_imageCache.TryGetValue(img, out var imageBytes))
                                    {
                                        choiceCol.Item()
                                            .PaddingTop(4)
                                            .MaxHeight(80)
                                            .AlignCenter()
                                            .Image(imageBytes)
                                            .FitArea();
                                    }
                                }
                            });
                        }
                    });
            });
        }

        private void RenderSubQuestionByType(ColumnDescriptor subQCol, QuestionMetadataPaginationDto subQuestion)
        {
            foreach (var subData in subQuestion.QuestionData)
            {
                RenderQuestionBody(subQCol, subData, isBold: false, bodyHeight: 120);

                if (subQuestion.Type.Equals(nameof(QuestionType.MCQ), StringComparison.OrdinalIgnoreCase) ||
                    subQuestion.Type.Equals(nameof(QuestionType.MultipleCorrectAnswers), StringComparison.OrdinalIgnoreCase) ||
                    subQuestion.Type.Equals(nameof(QuestionType.TrueAndFalse), StringComparison.OrdinalIgnoreCase)
                )
                {
                    RenderChoices(subQCol, subData.Choices ?? [], leftPadding: 15, choiceImageHeight: 100);
                }
            }
        }

        private void RenderComprehensionSubQuestions(ColumnDescriptor qCol, List<QuestionMetadataPaginationDto> subQuestions)
        {
            foreach (var subQuestion in subQuestions)
            {
                qCol.Item()
                    .PaddingTop(8)
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Background(Colors.White)
                    .Padding(10)
                    .Column(subQCol => RenderSubQuestionByType(subQCol, subQuestion));
            }
        }
        #endregion
    }

    public sealed class ParsedHtmlContent
    {
        public string Text { get; set; } = string.Empty;

        public List<string> ImageUrls { get; set; } = [];
    }
}
