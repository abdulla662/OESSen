using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace OES.Blazor.Components.Common
{
    public partial class NumberLanguageSelector
    {
        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

        [Parameter] public bool Value { get; set; }
        [Parameter] public EventCallback<bool> ValueChanged { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JSRuntime.InvokeVoidAsync(
                    "setEditorNumeralMode",
                    Value
                );
            }
        }

        private async Task OnValueChanged(bool value)
        {
            Value = value;

            await ValueChanged.InvokeAsync(value);

            await JSRuntime.InvokeVoidAsync(
                "setEditorNumeralMode",
                value
            );
        }
    }
}