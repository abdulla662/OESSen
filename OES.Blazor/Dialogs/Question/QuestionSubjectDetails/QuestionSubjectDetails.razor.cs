using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.Subject;
using System.Globalization;

namespace OES.Blazor.Dialogs.Question.QuestionSubjectDetails
{
    public partial class QuestionSubjectDetails
    {
        [Parameter] public SubjectDto Model { get; set; } = new();

        [CascadingParameter] public MudDialogInstance MudDialog { get; set; } = default!;

        private static bool RightToLeft => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

        private void Close()
        {
            MudDialog.Close();
        }
    }
}