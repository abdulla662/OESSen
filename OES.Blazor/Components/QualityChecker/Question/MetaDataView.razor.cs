using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.QuestionMetaData;
using OES.Helper.Dtos.Question;
using OES.Helper.Enums;
using System.Text.Json;

namespace OES.Blazor.Components.QualityChecker.Question
{
    public partial class MetaDataView : ComponentBase
    {
        [Inject] IBlazQuestionMetaData BlazQuestionMetaData { get; set; } = default!;

        [Parameter] public long QuestionMetaDataId { get; set; }

        private GetQuestionMetaDataForQCViewDto Model { get; set; } = new();

        private LayoutOrientation Orientation { get; set; } = LayoutOrientation.Horizontal;

        private Breakpoint SelectedScreenSize { get; set; }

        private string QuestionType { get; set; } = string.Empty;

        private string SelectScreenSizeClass { get; set; } = "w-100";

        public List<string> DeserializedExtensions { get; set; } = [];

        public List<string> Languages { get; set; } = [];


        protected override async Task OnParametersSetAsync()
        {
            if (QuestionMetaDataId == 0) return;

            var response = await BlazQuestionMetaData.GetQuestionMetaDataForQCView(QuestionMetaDataId);

            Model = response?.Data as GetQuestionMetaDataForQCViewDto ?? new();

            if (!string.IsNullOrWhiteSpace(Model?.FileUploadSettings?.SupportedFileExtensions))
            {
                DeserializedExtensions = JsonSerializer.Deserialize<List<string>>(Model.FileUploadSettings.SupportedFileExtensions) ?? [];
            }

            QuestionType = Model?.QuestionTypeName ?? string.Empty;

            Languages = Model?.QuestionDetails?.ConvertAll(x => x.LanguageName) ?? [];

            SelectedScreenSize = Breakpoint.Md;

            SelectScreenSizeClass = SelectedScreenSize switch
            {
                Breakpoint.Xs => "w-50",
                Breakpoint.Sm => "w-75",
                Breakpoint.Md => "w-100",
                _ => "w-100"
            };

            if (!string.IsNullOrWhiteSpace(Model?.LayoutName) &&
                Enum.TryParse<LayoutOrientation>(Model.LayoutName, out var parsed))
            {
                Orientation = parsed;
            }

            StateHasChanged();
        }
    }
}
