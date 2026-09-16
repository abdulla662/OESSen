using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.DeltaType;
using System.Globalization;

namespace OES.Blazor.Dialogs.DeltaType
{
    public partial class DeltaTypeDetails
    {
        [Parameter] public GetDeltaTypeDto Model { get; set; } = new GetDeltaTypeDto();

        [CascadingParameter] public MudDialogInstance MudDialog { get; set; } = default!;

        private static bool RightToLeft => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

        private void Close()
        {
            MudDialog.Close();
        }
    }
}