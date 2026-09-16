using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.DifficultyProfile;
using System.Globalization;

namespace OES.Blazor.Dialogs.DifficultyProfile
{
    public partial class DifficultyProfileDetails
    {
        [Parameter] public DifficultyProfileDto Model { get; set; } = new();

        [CascadingParameter] public MudDialogInstance MudDialog { get; set; } = default!;

        private static bool RightToLeft => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

        private void Close() => MudDialog.Close();
    }
}
