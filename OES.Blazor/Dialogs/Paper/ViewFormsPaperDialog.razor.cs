using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.PdfGeneratorService;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Globalization;
using System.Text.Json;

namespace OES.Blazor.Dialogs.Paper
{
    public partial class ViewFormsPaperDialog
    {
        [Inject] IJSRuntime JS { get; set; }
        [Inject] ISnackbar Snackbar { get; set; } = default!;
        [Inject] IBlazSessionStorageService SessionStorage { get; set; } = default!;
        [Inject] IBlazePdfGeneratorService BlazePdfService { get; set; } = default!;

        [Parameter] public GetPaperWithFormsDto Paper { get; set; } = new();

        private int CurrentFormIndex = 0;

        private bool IsFirst => CurrentFormIndex == 0;

        private bool IsLast => Paper.Forms == null || CurrentFormIndex == Paper.Forms.Count - 1;

        private bool HideDetailsForPdf { get; set; } = false;

        private static string CurrentLanguage => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? "ar" : "en";

        private MudDialog pdfDialog;

        private void NextForm()
        {
            if (!IsLast) CurrentFormIndex++;
        }

        private void PrevForm()
        {
            if (!IsFirst) CurrentFormIndex--;
        }

        private async Task OpenDialogAsync()
        {
            var options = new DialogOptions()
            {
                CloseButton = true,
                MaxWidth = MaxWidth.ExtraSmall
            };

            await pdfDialog.ShowAsync(string.Empty, options);
        }

        private async Task DownloadPdf(bool includeDetails)
        {
            Snackbar.Add(Resource.GeneratingPDFFile, Severity.Info);

            GetPaperWithFormsDto paperForPdf;

            if (Paper.Forms?.Count > 0 && CurrentFormIndex >= 0 && CurrentFormIndex < Paper.Forms.Count)
            {
                var selectedForm = Paper.Forms[CurrentFormIndex];

                paperForPdf = new GetPaperWithFormsDto
                {
                    PaperId = Paper.PaperId,
                    Name = Paper.Name,
                    Description = Paper.Description,
                    Code = Paper.Code,
                    LanguageDirection = Paper.LanguageDirection,
                    Abbreviation = Paper.Abbreviation,
                    QuestionsCount = selectedForm.Questions?.Count ?? 0,
                    Duration = Paper.Duration,
                    TotalMarks = Paper.TotalMarks,
                    Type = Paper.Type,
                    Forms = [selectedForm]
                };
            }
            else
            {
                paperForPdf = Paper;
            }

            var pdfBytes = await BlazePdfService.GeneratePaperFormPdfAsync(paperForPdf, includeDetails);

            await JS.InvokeVoidAsync(
                "downloadPdfFile",
                includeDetails ? $"Paper-form-{Paper.Name}-with-details-{CurrentLanguage}" : $"Paper-form-{Paper.Name}-{CurrentLanguage}",
                pdfBytes
            );

            Snackbar.Add(Resource.PdfGeneratedSuccessfully, Severity.Success);
        }

        private async Task OpenExamScreenDialog()
        {
            await SessionStorage.SetValue(MiscConstants.PerformEditBtnClick, Paper.PaperId.ToString());

            await SessionStorage.SetValue(MiscConstants.ExamScreenFormId, Paper.Forms[CurrentFormIndex].FormId.ToString());

            await JS.InvokeVoidAsync(MiscConstants.OpenInNewTab, "/ExamScreen");
        }

        private static List<string> GetDeserializedExtensions(string? supportedFileExtensions)
        {
            if (string.IsNullOrWhiteSpace(supportedFileExtensions))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(supportedFileExtensions) ?? [];
            }
            catch
            {
                return [];
            }
        }
    }
}