using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.AIQuestionGenerator.Response;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.QuestionChoices;
using OES.Helper.Enums;
using System.Globalization;

namespace OES.Blazor.Pages.AIQuestionGenerator.Dialogs;

public partial class AIQuestionViewDialog
{
    [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public AIQuestionMetadataDto? Question { get; set; }

    private static bool RightToLeft => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
    private LayoutOrientation Orientation => AIQuestionMetadataDialog.GetLayoutOrientation(Question?.LayoutName ?? string.Empty);
    private Breakpoint SelectedScreenSize { get; set; } = Breakpoint.Md;
    private string SelectScreenSizeClass { get; set; } = "w-100";

    private string QuestionTypeName => Question?.QuestionTypeName ?? string.Empty;

    private void Close() => MudDialog.Close();

    private static string FormatDeltaValue(decimal value)
    {
        return value.ToString("G29", CultureInfo.InvariantCulture);
    }

    private static QuestionDataDto MapToQuestionDataDto(AIQuestionDetailsDto detail)
    {
        var body = detail.Body;
        var modelAnswer = detail.ModelAnswer ?? string.Empty;
        var instructions = detail.Instructions;

        if (detail.UseArabicNumbers)
        {
            body = ConvertDigitsToArabic(body);
            modelAnswer = ConvertDigitsToArabic(modelAnswer);
            instructions = ConvertDigitsToArabic(instructions);
        }

        return new QuestionDataDto
        {
            Body = body,
            ModelAnswer = modelAnswer,
            Instructions = instructions,
            LanguageId = detail.LanguageId,
            Choices = detail.Choices?.Select((c, index) => new ChoiceDataDto
            {
                Id = index + 1,
                ChoiceText = detail.UseArabicNumbers ? ConvertDigitsToArabic(c.Text) : c.Text,
                IsCorrectAnswer = c.IsCorrect,
                OrderId = c.Order
            }).ToList()
        };
    }

    private static string ConvertDigitsToArabic(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return html;

        var regex = new System.Text.RegularExpressions.Regex(@"(<[^>]+>)|(\d)");
        var arabicDigits = new[] { "٠", "١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩" };

        return regex.Replace(html, m =>
        {
            if (m.Groups[1].Success)
            {
                return m.Value;
            }
            int digit = m.Value[0] - '0';
            return arabicDigits[digit];
        });
    }
}
