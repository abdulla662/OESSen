using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.Candidate;
using OES.Helper.Dtos.Candidate.Responses;
using OES.Helper.General;

namespace OES.Blazor.Dialogs.Candidate
{
    public partial class CandidateImportErrorsViewDialog
    {
        [CascadingParameter] private MudDialogInstance MudDialog { get; set; }
        [Parameter] public CandidateCombinedErrorsResponseDto CandidateCombinedErrorsResponseDto { get; set; } = new();

        private async Task<CustomTableData<ExcelValidationErrorDto>> ConvertToDataTableAsync(PaginationSearchModel paginationSearchModel)
        {
            var paginatedData = CandidateCombinedErrorsResponseDto
                .ExcelErrorsDto
                .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                .Take(paginationSearchModel.PageSize)
                .ToList();

            var tableData = new CustomTableData<ExcelValidationErrorDto>(paginatedData, CandidateCombinedErrorsResponseDto.ExcelErrorsDto.Count);

            await Task.CompletedTask;

            return tableData;
        }

        private void CloseDialog() => MudDialog.Close();
    }
}