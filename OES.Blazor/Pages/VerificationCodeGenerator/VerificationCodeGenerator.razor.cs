using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Dialogs.VerificationCode;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.VerificationCodeGenerator
{
    public partial class VerificationCodeGenerator
    {
        [Inject] private IJSRuntime JSRuntime { get; set; }

        [Inject] private IDialogService DialogService { get; set; }

        private string _registrationNumber = string.Empty;

        private string _candidateCode = string.Empty;

        private string _generatedCode = string.Empty;

        private string _copyButtonText = Resource.Copy;

        private void GenerateCode()
        {
            _generatedCode = VerificationCodeHelper.GenerateVerificationCode(_candidateCode, _registrationNumber);

            _copyButtonText = Resource.Copy;
        }

        private static string RemoveWhitespace(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var cleanedCharacters = value
                .Where(character => !char.IsWhiteSpace(character))
                .ToArray();

            return new string(cleanedCharacters);
        }

        private async Task CopyCodeAsync()
        {
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", _generatedCode);

            _copyButtonText = Resource.Copied;
        }

        private async Task OpenExportDialogAsync()
        {
            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseButton = true
            };

            await DialogService.ShowAsync<ExportVerificationCodeDialog>(Resource.ExportExcel, options);
        }
    }
}
