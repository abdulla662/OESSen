using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.QuestionMetaData;
using OES.Helper.Dtos.Question.QuestionVersions;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Components.QualityChecker.Versions
{
    public partial class QuestionVersions
    {
        [Inject] IBlazQuestionMetaData BlazQuestionMetaData { get; set; } = default!;
        [Inject] ISnackbar Snackbar { get; set; } = default!;

        [Parameter] public long QuestionMetaDataId { get; set; }

        private bool _showVersions = false;
        private bool _isLoading = false;
        private List<QuestionDetailsVersionDto> _versions = [];

        private async Task ToggleVersions()
        {
            _showVersions = !_showVersions;

            if (_showVersions && _versions.Count == 0)
            {
                _isLoading = true;
                StateHasChanged();

                var response = await BlazQuestionMetaData.GetQuestionVersionsAsync(QuestionMetaDataId);
                if (response?.Data is List<QuestionDetailsVersionDto> data)
                {
                    _versions = data;
                }
                else
                {
                    Snackbar.Add(Resource.FailedToLoadQuestionDetails, Severity.Error);
                    _showVersions = false;
                }

                _isLoading = false;
            }

            StateHasChanged();
        }
    }
}
