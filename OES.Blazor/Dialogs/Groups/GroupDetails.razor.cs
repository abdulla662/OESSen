using Microsoft.AspNetCore.Components;
using OES.Helper.Dtos.OESUserGroups;
using System.Globalization;

namespace OES.Blazor.Dialogs.Groups
{
    public partial class GroupDetails : ComponentBase
    {
        [Parameter] public OESGroupDetailsDto Model { get; set; } = new OESGroupDetailsDto();

        private static bool RightToLeft => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
    }
}
