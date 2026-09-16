using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.DifficultyLevel;
using System.Globalization;

namespace OES.Blazor.Dialogs.DifficultyLevel
{
    public partial class DifficultyLevelDetails
    {
        [Parameter] public DifficultyLevelDto Model { get; set; } = new();

        [CascadingParameter] public MudDialogInstance MudDialog { get; set; } = default!;

        private static bool RightToLeft => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

        private void Close() => MudDialog.Close();
    }
}
