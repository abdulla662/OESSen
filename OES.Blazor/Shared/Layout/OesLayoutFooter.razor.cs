using Microsoft.AspNetCore.Components;

namespace OES.Blazor.Shared.Layout
{
    public partial class OesLayoutFooter
    {
        [Parameter] public string CssClasses { get; set; } = "d-flex justify-center pa-4 Footer-paper";
    }
}
