using Microsoft.AspNetCore.Components;
using OES.Helper.Dtos.QcComments;
using OES.Helper.General;

namespace OES.Blazor.Components.QualityChecker.Comments
{
    public partial class QuestionLogs
    {
        [Parameter] public long QuestionMetaDataId { get; set; }

        private bool _ShowLogs = false;

        private void ShowLogs()
        {
            _ShowLogs = !_ShowLogs;

            StateHasChanged();
        }

        public async Task<CustomTableData<QcCommentDto>> GetAllQuestionLogsAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _blazQCommentService.GetCommentsByMetaDataId(paginationSearchModel, QuestionMetaDataId);
        }
    }
}
