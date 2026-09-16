using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.QuestionCategory;
using System.Globalization;

namespace OES.Blazor.Dialogs.Question.QuestionCategory
{
    public partial class QuestionCategoryDetails
    {
        [Parameter] public QuestionCategoryDto Model { get; set; } = new();

        [CascadingParameter] public MudDialogInstance MudDialog { get; set; } = default!;

        private static bool RightToLeft => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

        private static string FormatDecimal(decimal? value)
        {
            return value.HasValue
                ? value.Value.ToString("0.###############", CultureInfo.InvariantCulture)
                : "-";
        }

        private void Close() => MudDialog.Close();
    }
}