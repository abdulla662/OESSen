using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.Question;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.QuestionMetaData;
using OES.Helper.Dtos.Question;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.Globalization;
using System.Text.Json;

namespace OES.Blazor.Dialogs.Question.QuestionViewDialog
{
    public partial class QuestionViewDialog
    {
        [Inject] IBlazQuestionMetaData BlazQuestionMetaData { get; set; } = default!;
        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; } = default!;
        [Inject] ISnackbar Snackbar { get; set; } = default!;
        [Inject] IDialogService DialogService { get; set; } = default!;

        private static bool RightToLeft => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
        private GetQuestionMetaDataForQCViewDto Model { get; set; } = new();
        private LayoutOrientation Orientation { get; set; } = LayoutOrientation.Horizontal;
        private Breakpoint SelectedScreenSize { get; set; }
        private string QuestionType { get; set; } = string.Empty;
        private string LanguagesNames { get; set; } = string.Empty;
        private string SelectScreenSizeClass { get; set; } = "w-100";
        private bool IsInitialized { get; set; } = false;
        public long QuestionMetaDataId { get; set; }
        public List<string> DeserializedExtensions { get; set; } = [];

        private bool IsSegmentQuestion =>
            Model?.QuestionTypeId == (long)Helper.Enums.QuestionType.SegmentWithAudioAnswer ||
            Model?.QuestionTypeId == (long)Helper.Enums.QuestionType.SegmentWithVideoAnswer;

        private bool HasMaxWords =>
            (Model?.QuestionTypeId == (long)Helper.Enums.QuestionType.Essay && Model?.MaxWords != null) ||
            (Model?.QuestionTypeId == (long)Helper.Enums.QuestionType.FileUploadResponse && Model?.MaxWords != null && Model?.FileUploadSettings?.ShowAnswerTextArea == true);


        protected override async Task OnInitializedAsync()
        {
            QuestionMetaDataId = await BlazSessionStorageService.GetValue<long>("PerformViewBtnClick");

            if (QuestionMetaDataId == 0)
            {
                Snackbar.Add(Resource.FailedToLoadQuestionDetails, Severity.Error);
                return;
            }

            await LoadQuestionMetaData(QuestionMetaDataId);

            SelectedScreenSize = Breakpoint.Md;
            SelectScreenSizeClass = "w-100";
            IsInitialized = true;
        }

        private async Task LoadQuestionMetaData(long questionMetaDataId)
        {
            var response = await BlazQuestionMetaData.GetQuestionMetaDataForQCView(questionMetaDataId);

            Model = response?.Data as GetQuestionMetaDataForQCViewDto ?? new();

            LanguagesNames = string.Join(", ", Model.QuestionDetails?.Select(x => x.LanguageName) ?? []);

            QuestionType = Model?.QuestionTypeName ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(Model?.LayoutName) && Enum.TryParse<LayoutOrientation>(Model.LayoutName, out var parsed))
            {
                Orientation = parsed;
            }

            if (Model?.FileUploadSettings != null && !string.IsNullOrWhiteSpace(Model.FileUploadSettings.SupportedFileExtensions))
            {
                DeserializedExtensions = JsonSerializer.Deserialize<List<string>>(Model.FileUploadSettings.SupportedFileExtensions) ?? [];
            }

            StateHasChanged();
        }

        private static string FormatDeltaValue(decimal? value)
        {
            return value?.ToString("G29", CultureInfo.InvariantCulture) ?? "0";
        }

        private async Task OpenItemBankTreeViewDialogAsync()
        {
            if (Model.ItemBankNodeId <= 0)
                return;

            var parameters = new DialogParameters<ItemBankTreeViewReadOnlyDialog.ItemBankTreeViewReadOnlyDialog>
            {
                { p => p.SelectedItemBankNodeId, Model.ItemBankNodeId },
                { p => p.ItemBankRootId, Model.ItemBankRootId ?? Model.ItemBankNodeId }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            await DialogService.ShowAsync<ItemBankTreeViewReadOnlyDialog.ItemBankTreeViewReadOnlyDialog>(string.Empty, parameters, options);
        }
    }
}
