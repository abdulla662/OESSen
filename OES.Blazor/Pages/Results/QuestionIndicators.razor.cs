using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.DownloadProgressDialog;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.Question.QuestionIndicators;
using OES.Helper.Dtos.QuestionIndicator;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Text.Json;

namespace OES.Blazor.Pages.Results
{
    public partial class QuestionIndicators : ComponentBase
    {
        [Inject] public IBlazQuestionService QuestionService { get; set; }
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IDialogService DialogService { get; set; }

        private int PercentageValue { get; set; }
        private IndicatorType Type { get; set; }

        private ListItem<QuestionAnalyticsIndicatorDto> _listItemRef;
        private List<QuestionAnalyticsIndicatorDto> _currentData = [];
        private bool _isFiltering = false;
        private bool _hasSearched = false;
        private DateRange _dateRange = new(
            DateTime.Now.AddDays(-14),
            DateTime.Now
        );

        private object GetFilterDto() => new QuestionIndicatorFilterDto
        {
            Percentage = PercentageValue,
            Type = Type,
            FromDate = _dateRange?.Start,
            ToDate = _dateRange?.End
        };

        private async Task OnSearchClick()
        {
            if (PercentageValue < 0 || PercentageValue > 100) return;

            _isFiltering = true;
            _hasSearched = true;
            StateHasChanged();

            await (_listItemRef?.table?.ReloadServerData() ?? Task.CompletedTask);

            _isFiltering = false;
            StateHasChanged();
        }

        public async Task<CustomTableData<QuestionAnalyticsIndicatorDto>> GetIndicatorDataAsync(PaginationSearchModel pagination)
        {
            if (!_hasSearched)
            {
                return new CustomTableData<QuestionAnalyticsIndicatorDto>(
                    [],
                    0
                );
            }

            pagination.FilterObj = GetFilterDto();

            var result = await QuestionService.GetQuestionIndicatorDataAsync(pagination);

            _currentData = [.. result.Items];

            return result;
        }

        private async Task DownloadExcelAsync()
        {
            var options = new DialogOptions
            {
                CloseButton = false,
                CloseOnEscapeKey = false,
                MaxWidth = MaxWidth.ExtraSmall,
                FullWidth = true,
                Position = DialogPosition.Center
            };

            var dialog = DialogService.Show<DownloadProgressDialog>(Resource.PreparingFile, options);

            var request = new QuestionIndicatorExportRequestDto(PercentageValue, _dateRange?.Start, _dateRange?.End, Type);

            var response = await QuestionService.ExportQuestionIndicatorsAsync(request);

            var file = JsonSerializer.Deserialize<DownloadFileDto>(
                response.Data.ToString(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            dialog.Close();

            await DownloadFileAsync(file.FileName, file.FileContent);
        }

        private async Task DownloadFileAsync(string fileName, string base64Content)
        {
            await JS.InvokeVoidAsync("downloadBase64File", fileName, base64Content);
            Snackbar.Add($"Downloaded: {fileName}", Severity.Success);
        }
    }
}
