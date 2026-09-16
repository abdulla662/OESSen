using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.QuestionCataegory;
using OES.Blazor.Services.Interfaces.QuestionType;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.Dtos.QuestionComment;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Question
{
    public partial class QuestionsQualityPassingChecklist
    {
        [Inject] IBlazQuestionCategoryService BlazQuestionCategoryService { get; set; }
        [Inject] IBLazQuestionType BLazQuestionType { get; set; }
        [Inject] IBlazItemBankService BlazItemBankService { get; set; }
        [Inject] IBlazQuestionService BlazQuestionService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Inject] GlobalUserContext GlobalUserContext { get; set; } = default!;

        private HashSet<PendingQuestionPaginationDto> _selectedQuestions = new();
        private List<QuestionCategoryDto> _categories;
        private List<QuestionTypeDto> _questionTypes;
        private List<RootItemBankDto> _itemBanks;
        private QuestionCategoryDto _selectedCategory;
        private RootItemBankDto _selectedItemBank;
        private QuestionTypeDto _selectedType;
        private int _changingKey;
        private List<TreeItemResponseDto> _branches = [];
        private TreeItemResponseDto _selectedBranch;

        public QuestionFilterPaginationModel questionFilterPaginationModel { get; set; } = new();
        public RootItemBankDto SelectedItemBank
        {
            get => _selectedItemBank;
            set
            {
                if (_selectedItemBank != value)
                {
                    _selectedItemBank = value;
                    questionFilterPaginationModel._selectedItemBankSignature = _selectedItemBank?.ItemBankSignature;

                    _selectedBranch = null;
                    _branches = [];
                    questionFilterPaginationModel._selectedBranchId = 0;

                    if (_selectedItemBank != null)
                        _ = LoadBranchesAsync(_selectedItemBank.Id, _selectedItemBank.ItemBankSignature);

                    _changingKey += 1;
                }
            }
        }
        public TreeItemResponseDto SelectedBranch
        {
            get => _selectedBranch;
            set
            {
                if (_selectedBranch != value)
                {
                    _selectedBranch = value;
                    questionFilterPaginationModel._selectedBranchId = _selectedBranch?.Id ?? 0;
                    _changingKey += 1;
                }
            }
        }
        public QuestionCategoryDto SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    questionFilterPaginationModel._selectedCategory = _selectedCategory?.Id ?? 0;
                    _changingKey += 1;
                }
            }
        }
        public QuestionTypeDto SelectedType
        {
            get => _selectedType;
            set
            {
                if (_selectedType != value)
                {
                    _selectedType = value;
                    questionFilterPaginationModel._selectedType = _selectedType?.Id ?? 0;
                    _changingKey += 1;
                }
            }
        }


        protected override async Task OnInitializedAsync()
        {
            var categoriesTask = BlazQuestionCategoryService.GetCategories();
            var questionTypesTask = BLazQuestionType.GetAllQuestionType();
            var itemBanksTask = BlazItemBankService.GetItemBanksListAsync();

            await Task.WhenAll(categoriesTask, questionTypesTask, itemBanksTask);

            _categories = categoriesTask.Result;
            _questionTypes = questionTypesTask.Result;
            _itemBanks = itemBanksTask.Result;
        }

        private async Task ShowBypassConfirmation()
        {
            if (_selectedQuestions == null || _selectedQuestions.Count == 0)
            {
                Snackbar.Add(Resource.PleaseSelectAtLeastOneQuestion, Severity.Warning);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>
            {
                { x => x.Content, string.Format(Resource.ConfirmBypassQualityCheck, _selectedQuestions.Count) },
                { x => x.SubmitText, Resource.Yes },
                { x => x.CancelText, Resource.No },
                { x => x.SubmitButtonColor, Color.Primary },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.Check }
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Small,
                CloseButton = false
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await BypassSelectedQuestions();
            }
        }

        public async Task BypassSelectedQuestions()
        {
            if (_selectedQuestions.Count == 0)
            {
                Snackbar.Add(Resource.PleaseSelectAtLeastOneQuestion, Severity.Error);
                return;
            }

            var selectedIds = _selectedQuestions.Select(q => q.Id).ToList();

            var bypassQuestionsDto = new BypassQuestionsDto(
                selectedIds,
                GlobalUserContext.UserName
            );

            var response = await BlazQuestionService.BypassQuestionsAsync(bypassQuestionsDto);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);
                _selectedQuestions.Clear();
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            _changingKey++;
        }

        protected Task OnSelectedQuestionsChanged(HashSet<PendingQuestionPaginationDto> selected)
        {
            _selectedQuestions = selected;

            StateHasChanged();

            return Task.CompletedTask;
        }

        private async Task LoadBranchesAsync(long itemBankId, string signature)
        {
            _branches = await BlazItemBankService.GetChildrenByParentIdAsync(itemBankId, signature);
            await InvokeAsync(StateHasChanged);
        }

        private Task<IEnumerable<TreeItemResponseDto>> SearchBranches(string searchText, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return Task.FromResult(_branches.AsEnumerable());

            return Task.FromResult(
                _branches.Where(x => x.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            );
        }
    }
}
