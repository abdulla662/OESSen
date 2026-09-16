using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.QuestionCataegory;
using OES.Blazor.Services.Interfaces.QuestionType;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.General;
using SharedHelper.General;
using System.Text.Json;

namespace OES.Blazor.Pages.Question
{
    public partial class QuestionsAdvancedFilter : ComponentBase
    {
        [Inject] IBlazQuestionService BlazQuestionService { get; set; }
        [Inject] IBlazQuestionCategoryService BlazQuestionCategoryService { get; set; }
        [Inject] IBLazQuestionType BLazQuestionType { get; set; }
        [Inject] IBlazAuthService BlazAuthService { get; set; }
        [Inject] IJSRuntime JS { get; set; }

        private Guid _listChangingKey = Guid.NewGuid();
        private PaginationSearchModel _filterObject = new();
        private bool _isExporting;

        private List<QuestionCategoryDto> _categories = [];
        private List<QuestionTypeDto> _questionTypes = [];
        private List<string> _authors = [];

        private string _questionCode;
        private QuestionCategoryDto _selectedCategory;
        private QuestionTypeDto _selectedType;
        private string _selectedCreatedBy;
        private string _selectedModifiedBy;
        private DateTime? _createdFromDate;
        private DateTime? _createdToDate;
        private DateTime? _modifiedFromDate;
        private DateTime? _modifiedToDate;

        protected override async Task OnInitializedAsync()
        {
            var categoriesTask = BlazQuestionCategoryService.GetCategories();
            var typesTask = BLazQuestionType.GetAllQuestionType();
            var authorsTask = BlazQuestionService.GetQuestionAuthorsAsync();

            await Task.WhenAll(categoriesTask, typesTask, authorsTask);

            _categories = categoriesTask.Result ?? [];
            _questionTypes = typesTask.Result ?? [];
            _authors = authorsTask.Result ?? [];
        }

        private void ApplyFilters()
        {
            _filterObject = new PaginationSearchModel
            {
                SearchKey = string.IsNullOrWhiteSpace(_questionCode) ? null : _questionCode.Trim(),
                FromDate = _createdFromDate,
                ToDate = _createdToDate?.AddDays(1),
                FilterObj = new QuestionAdvancedFilterDto
                {
                    CategoryId = _selectedCategory?.Id,
                    QuestionTypeId = _selectedType?.Id,
                    CreatedByUserName = _selectedCreatedBy == null ? null : _selectedCreatedBy.ToLower() == nameof(System).ToLower() ? "superadmin" : _selectedCreatedBy,
                    ModifiedByUserName = _selectedModifiedBy == null ? null : _selectedModifiedBy.ToLower() == nameof(System).ToLower() ? "superadmin" : _selectedModifiedBy,
                    ModifiedFromDate = _modifiedFromDate,
                    ModifiedToDate = _modifiedToDate?.AddDays(1).AddTicks(-1)
                }
            };

            _listChangingKey = Guid.NewGuid();
        }

        private void ResetFilters()
        {
            _questionCode = null;
            _selectedCategory = null;
            _selectedType = null;
            _selectedCreatedBy = null;
            _selectedModifiedBy = null;
            _createdFromDate = null;
            _createdToDate = null;
            _modifiedFromDate = null;
            _modifiedToDate = null;
            _filterObject = new PaginationSearchModel();
            _listChangingKey = Guid.NewGuid();
        }

        private async Task ExportToExcelAsync()
        {
            _isExporting = true;
            StateHasChanged();

            try
            {
                var token = await BlazAuthService.GetDecryptedTokenFromLocalStorageAsync();
                var url = $"{CentralizedUrlHelper.OesApiBaseUrl}api/Question/ExportFilteredQuestionsToExcel";
                var exportRequest = new PaginationSearchModel
                {
                    PaginationOff = true,
                    SearchKey = _filterObject.SearchKey,
                    FromDate = _filterObject.FromDate,
                    ToDate = _filterObject.ToDate,
                    FilterObj = _filterObject.FilterObj
                };
                var bodyJson = JsonSerializer.Serialize(exportRequest);
                var fileName = $"FilteredQuestions_{DateTime.Now:yyyy-MM-dd}.xlsx";
                await JS.InvokeVoidAsync("downloadExcelViaFetch", url, bodyJson, fileName, token);
            }
            finally
            {
                _isExporting = false;
                StateHasChanged();
            }
        }

        private Task<IEnumerable<string>> SearchAuthors(string searchText, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return Task.FromResult(_authors.AsEnumerable());

            return Task.FromResult(_authors.Where(x =>
                x.Contains(searchText, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
