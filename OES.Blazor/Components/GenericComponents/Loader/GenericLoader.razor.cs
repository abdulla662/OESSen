using Microsoft.AspNetCore.Components;

namespace OES.Blazor.Components.GenericComponents.Loader
{
    public partial class GenericLoader
    {
        [Parameter] public string LoaderText { get; set; } = string.Empty;
    }
}
