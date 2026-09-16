using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.CandidatesResult;
using System.Globalization;

namespace OES.Blazor.Dialogs.CandidatesResult
{
    public partial class CandidatesResultViewDialog
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public GetCandidateQuestionsAnswersDto Model { get; set; }

        private static bool RightToLeft => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

        private void Close()
        {
            MudDialog.Cancel();
        }
    }
}