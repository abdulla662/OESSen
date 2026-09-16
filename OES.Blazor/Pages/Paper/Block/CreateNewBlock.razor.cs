using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Extentions.DialogHelpers;
using OES.Blazor.Services.Interfaces.Block;
using OES.Blazor.Services.Interfaces.DeltaType;
using OES.Blazor.Services.Interfaces.DifficultyLevel;
using OES.Blazor.Services.Interfaces.DifficultyProfile;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.QuestionCataegory;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Blazor.Services.Interfaces.QuestionType;
using OES.Helper.Dtos.Block.Requests;
using OES.Helper.Dtos.DeltaType;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Paper.Block
{
    public partial class CreateNewBlock
    {
        [Inject] IBLazQuestionType BLazQuestionType { get; set; }
        [Inject] IBlazQuestionCategoryService BLazQuestionCategory { get; set; }
        [Inject] IBlazItemBankService BlazItemBankService { get; set; }
        [Inject] IBlazDifficultyLevelService BlazDifficultyLevelService { get; set; }
        [Inject] IBlazDifficultyProfileService BlazProfileService { get; set; }
        [Inject] IBlazDeltaTypeService BlazDeltaTypeService { get; set; }
        [Inject] IBlazBlockService BlazBlockService { get; set; }
        [Inject] IBlazQuestionService BlazQuestionService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Inject] IBlazQuestionLanguageService BlazLanguageService { get; set; }
        [Inject] IBlazGroupService BlazGroupService { get; set; }

        private CreateOrUpdateBlockRequestDto Model { get; set; } = new CreateOrUpdateBlockRequestDto();
        private List<GetOESGroupDto> SelectedGroups { get; set; } = [];

        private List<DifficultyLevelDto> _difficultyLevels = [];
        private List<GetDeltaTypeDto> _deltaTypes = [];
        private List<QuestionTypeDto> _questionTypes = [];
        private List<QuestionCategoryDto> _blockTypes = [];
        private List<RootItemBankDto> _itemBanks = [];
        private List<ProfileDto> _difficultyProfiles = [];
        private readonly HashSet<ApprovedQuestionsPaginationDto> _selectedQuestions = [];
        private DifficultyLevelDto _selectedDifficultyLevelInput;
        private ProfileDto _selectedDifficultyProfileInput;
        private GetDeltaTypeDto _selectedDeltaTypeInput;
        private QuestionTypeDto _selectedQuestionType;
        private QuestionCategoryDto _selectedBlockType;
        private RootItemBankDto _selectedItemBank;
        private bool disabled = true;
        private int _questionsListChangingKey;
        private List<LanguageDto> _languages = [];
        private LanguageDto _selectedLanguage;
        private bool _considerDifficultyLevel = true;

        public LanguageDto SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value)
                {
                    _selectedLanguage = value;
                    QuestionFilterPaginationModel._selectedLanguage = _selectedLanguage?.Id ?? 0;
                    _questionsListChangingKey += 1;
                }
            }
        }

        public QuestionFilterPaginationModel QuestionFilterPaginationModel { get; set; } = new QuestionFilterPaginationModel();

        private bool IsSubmitButtonDisabled =>
            string.IsNullOrWhiteSpace(Model?.Name) ||
            string.IsNullOrWhiteSpace(Model?.Code) ||
            string.IsNullOrWhiteSpace(_selectedBlockType?.Name) ||
            string.IsNullOrWhiteSpace(_selectedDeltaTypeInput?.Name) ||
            (_selectedDifficultyLevelInput?.Id ?? 0) <= 0 ||
            _selectedQuestions.Count == 0;

        private bool ShowQuestion =>
            QuestionFilterPaginationModel._selectedDeltaType > 0 &&
            (!ConsiderDifficultyLevel || QuestionFilterPaginationModel._selectedDifficultyLevel > 0) &&
            QuestionFilterPaginationModel._selectedLanguage > 0;

        public bool ConsiderDifficultyLevel
        {
            get => _considerDifficultyLevel;
            set
            {
                if (_considerDifficultyLevel != value)
                {
                    _considerDifficultyLevel = value;
                    Model.ConsiderDifficultyLevel = value;
                    QuestionFilterPaginationModel._isConsiderDifficulty = value;
                    _questionsListChangingKey += 1;
                    StateHasChanged();
                }
            }
        }

        public GetDeltaTypeDto SelectedDeltaTypeInput
        {
            get => _selectedDeltaTypeInput;
            set
            {
                if (_selectedDeltaTypeInput != value)
                {
                    OnDeltaTypeChanged(value);
                }
            }
        }

        public DifficultyLevelDto SelectedDifficultyLevelInput
        {
            get => _selectedDifficultyLevelInput;
            set
            {
                if (_selectedDifficultyLevelInput != value)
                {
                    OnDifficultyLevelChanged(value);
                }
            }
        }

        public ProfileDto SelectedDifficultyProfileInput
        {
            get => _selectedDifficultyProfileInput;
            set
            {
                if (_selectedDifficultyProfileInput != value)
                {
                    OnDifficultyProfileChanged(value);
                }
            }
        }

        public QuestionCategoryDto SelectedBlockType
        {
            get => _selectedBlockType;
            set
            {
                if (_selectedBlockType != value)
                {
                    OnBlockTypeChanged(value);
                }
            }
        }

        public QuestionTypeDto SelectedQuestionType
        {
            get => _selectedQuestionType;
            set
            {
                if (_selectedQuestionType != value)
                {
                    _selectedQuestionType = value;
                    QuestionFilterPaginationModel._selectedType = _selectedQuestionType?.Id ?? 0;
                    _questionsListChangingKey += 1;
                }
            }
        }

        public RootItemBankDto SelectedItemBank
        {
            get => _selectedItemBank;
            set
            {
                if (_selectedItemBank != value)
                {
                    _selectedItemBank = value;
                    QuestionFilterPaginationModel._selectedItemBank = _selectedItemBank?.Id ?? 0;
                    _questionsListChangingKey += 1;
                }
            }
        }

        protected override async void OnInitialized()
        {
            // Create all tasks
            var deltaTypesTask = BlazDeltaTypeService.GetDeltaTypes();
            var difficyltyprofile = BlazProfileService.GetProfiles();
            var questionTypesTask = BLazQuestionType.GetAllQuestionType();
            var questionCategoriesTask = BLazQuestionCategory.GetCategories();
            var itemBanksTask = BlazItemBankService.GetItemBanksListAsync();
            var languagesTask = BlazLanguageService.GetAllLanguagesAsync();

            // Run all tasks concurrently
            await Task.WhenAll(
                deltaTypesTask,
                questionTypesTask,
                questionCategoriesTask,
                itemBanksTask,
                difficyltyprofile,
                languagesTask
            );

            // Assign results, handling potential null returns
            _deltaTypes = await deltaTypesTask ?? [];
            _difficultyProfiles = await difficyltyprofile ?? [];
            _questionTypes = await questionTypesTask ?? [];
            _blockTypes = await questionCategoriesTask ?? [];
            _itemBanks = await itemBanksTask ?? [];
            _languages = await languagesTask ?? [];
        }

        private bool IsSelectedQuestion(object question)
        {
            var q = (ApprovedQuestionsPaginationDto)question;

            return _selectedQuestions.Contains(q);
        }

        private void OnBlockNameChanged(string name)
        {
            Model.Name = name;
            Model.Code = name?.Trim().Replace(" ", "-");
        }

        private async Task OnSubmit()
        {
            if (_selectedQuestions == null || !_selectedQuestions.Any())
            {
                Snackbar.Add(Resource.PleaseSelectAtLeastOneQuestion, Severity.Error);
                return;
            }

            Model.Name = Model.Name?.Trim();
            Model.QuestionsIds = [.. _selectedQuestions.Select(q => q.Id)];
            Model.DifficultyLevelId = _selectedDifficultyLevelInput?.Id ?? 0;
            Model.DeltaTypeId = _selectedDeltaTypeInput?.Id ?? 0;
            Model.BlockTypeId = _selectedBlockType?.Id ?? 0;
            Model.LanguageId = _selectedLanguage?.Id ?? 0;
            Model.OESGroupIds = SelectedGroups.ConvertAll(g => g.Id);
            var response = await BlazBlockService.AddNewBlock(Model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);

                NavigateBack();
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);

                if (response.CustomCodeStatus == CustomCodeStatus.BlockMismatch)
                {
                    _selectedQuestions.Clear();

                    StateHasChanged();
                }
            }
        }

        private async Task<bool> ConfirmChangeAsync(string message)
        {
            var parameters = new DialogParameters<GenericDialog>
            {
                { x => x.Title, Resource.Alert },
                { x => x.Content, message },
                { x => x.CancelText, Resource.No },
                { x => x.SubmitText, Resource.Yes }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);
            var result = await dialog.Result;

            return !result.Canceled;
        }

        private void ClearSelectedQuestions(string infoMessage)
        {
            _selectedQuestions.Clear();

            Snackbar.Add(infoMessage, Severity.Info);
        }

        private async Task OnDeltaTypeChanged(GetDeltaTypeDto selectedDeltaType)
        {
            bool isDifferent = _selectedDeltaTypeInput?.Id != selectedDeltaType?.Id;

            if (_selectedQuestions.Any() && selectedDeltaType != null && isDifferent)
            {
                if (!await ConfirmChangeAsync(Resource.DeltaTypeChangeRemovesQuestions))
                    return;

                ClearSelectedQuestions(Resource.SelectedQuestionsRemovedDelta);
            }

            _selectedDeltaTypeInput = selectedDeltaType;

            if (_selectedDeltaTypeInput != null)
            {
                _selectedDifficultyProfileInput = null;

                disabled = true;

                if (_selectedDifficultyProfileInput != null)
                {
                    _difficultyLevels = await BlazDifficultyLevelService.GetDifficultyLevelsByDeltaTypeId(_selectedDeltaTypeInput.Id);
                    disabled = false;
                }
            }
            else
            {
                _difficultyLevels = [];
                disabled = true;
            }

            _selectedDifficultyLevelInput = null;
            QuestionFilterPaginationModel._selectedDeltaType = _selectedDeltaTypeInput?.Id ?? 0;
            QuestionFilterPaginationModel._selectedDifficultyLevel = 0;
            _questionsListChangingKey++;

            StateHasChanged();
        }

        private async Task OnDifficultyLevelChanged(DifficultyLevelDto selectedDifficiltyLevel)
        {
            bool isDifferent = _selectedDifficultyLevelInput?.Id != selectedDifficiltyLevel?.Id;

            if (ConsiderDifficultyLevel && _selectedQuestions.Any() && selectedDifficiltyLevel != null && isDifferent)
            {
                if (!await ConfirmChangeAsync(Resource.ClearQuestionsOnProfileChangeProceed))
                    return;

                ClearSelectedQuestions(Resource.QuestionsClearedOnProfileChange);
            }

            _selectedDifficultyLevelInput = selectedDifficiltyLevel;
            QuestionFilterPaginationModel._selectedDifficultyLevel = _selectedDifficultyLevelInput?.Id ?? 0;
            _questionsListChangingKey++;

            StateHasChanged();
        }

        private async Task OpenGroupDialog()
        {
            SelectedGroups = await DialogInteractionService.OpenSelectionDialogAsync(
                dialogService: DialogService,
                title: Resource.SelectGroup,
                preSelectedItems: SelectedGroups,
                endpointService: async pagination =>
                {
                    var allGroups = await BlazBlockService.GetUserBlockGroupsAsync();

                    if (!string.IsNullOrWhiteSpace(pagination.SearchKey))
                    {
                        allGroups = [.. allGroups.Where(g => g.Name.Contains(pagination.SearchKey, StringComparison.OrdinalIgnoreCase))];
                    }

                    return new CustomTableData<GetOESGroupDto>
                    {
                        Items = allGroups,
                        TotalItems = allGroups.Count
                    };
                },
                resourceType: ResourceType.Block,
                onDelete: EventCallback.Factory.Create<Guid>(this, DeleteGroupAsync)
            );
        }

        private async Task DeleteGroupAsync(Guid groupId)
        {
            var response = await BlazGroupService.DeleteGroupAsync(groupId);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
                Snackbar.Add(response.Message, Severity.Success);
            else
                Snackbar.Add(response.Message, Severity.Error);
        }

        private async Task OnDifficultyProfileChanged(ProfileDto selectedDifficiltyProfile)
        {
            bool isDifferent = _selectedDifficultyProfileInput?.Id != selectedDifficiltyProfile?.Id;

            _selectedDifficultyProfileInput = selectedDifficiltyProfile;

            QuestionFilterPaginationModel._selectedDifficultyProfile = _selectedDifficultyProfileInput?.Id ?? 0;

            _questionsListChangingKey++;

            if (_selectedDifficultyProfileInput != null)
            {
                _difficultyLevels = await BlazDifficultyLevelService.GetDifficultyLevelByProfileIdAsync(_selectedDifficultyProfileInput.Id);

                disabled = false;

                if (_difficultyLevels == null || !_difficultyLevels.Any())
                {
                    disabled = true;
                    _selectedDifficultyLevelInput = null;
                }
            }
            else
            {
                _difficultyLevels = [];

                _selectedDifficultyLevelInput = null;

                disabled = true;
            }

            StateHasChanged();

            if (_selectedQuestions.Count > 0 && selectedDifficiltyProfile != null && isDifferent)
            {
                if (!await ConfirmChangeAsync(Resource.ClearQuestionsOnProfileChangeProceed))
                    return;

                ClearSelectedQuestions(Resource.QuestionsClearedOnProfileChange);
            }
        }

        private void OnBlockTypeChanged(QuestionCategoryDto selectedBlockType)
        {
            _selectedBlockType = selectedBlockType;

            StateHasChanged();
        }

        private void SelectedQuestionChanged(object selectedQuestion)
        {
            _selectedQuestions.Add((ApprovedQuestionsPaginationDto)selectedQuestion);

            StateHasChanged();
        }

        private void RemoveQuestion(ApprovedQuestionsPaginationDto question)
        {
            _selectedQuestions.Remove(question);

            StateHasChanged();
        }

        private void NavigateBack()
        {
            NavigationManager.NavigateTo("/BlocksList");
        }

        private void OnSelectedQuestionsChanged(HashSet<ApprovedQuestionsPaginationDto> _)
        {
            // You don't need this statement here _selectedQuestions = selectedQuestions;, since the reference of _selectedQuestions is updated internally with the newly selected/deselected items.

            StateHasChanged();
        }

        private async Task OnConsiderDifficultyLevelChanged(bool value)
        {
            if (value && _selectedQuestions.Count != 0)
            {
                bool hasMismatched = _selectedQuestions.Any(q => q.DifficultyLevel != _selectedDifficultyLevelInput?.Name);
                if (hasMismatched)
                {
                    bool confirm = await ConfirmChangeAsync(Resource.ClearQuestionsOnProfileChangeProceed);
                    if (!confirm)
                    {
                        return;
                    }
                    _selectedQuestions.RemoveWhere(q => q.DifficultyLevel != _selectedDifficultyLevelInput?.Name);
                    Snackbar.Add(Resource.QuestionsClearedOnProfileChange, Severity.Warning);
                }
            }

            ConsiderDifficultyLevel = value;
        }
    }
}