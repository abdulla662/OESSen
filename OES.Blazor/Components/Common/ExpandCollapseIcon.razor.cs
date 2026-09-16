using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace OES.Blazor.Components.Common
{
    public partial class ExpandCollapseIcon
    {
        [Parameter] public bool IsExpanded { get; set; }

        private static bool IsRtl => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

        private string IconPath => IsExpanded
            ? "M12 15.5l-6-6h12l-6 6z"  // ExpandLess icon path (arrow down)
            : (IsRtl ? "M8 12l6-6v12l-6-6z" : "M16 12l-6-6v12l6-6z"); // ExpandMore icon path (arrow to the right) or (arrow to the left) based on RTL

        private string CurrentColor => IsExpanded
            ? " rgb(0, 183, 255)"
            : "rgba(2,64,119,1)";
    }
}