using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Extentions.DialogHelpers;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.Block;
using OES.Blazor.Services.Interfaces.DeltaType;
using OES.Blazor.Services.Interfaces.DifficultyLevel;
using OES.Blazor.Services.Interfaces.DifficultyProfile;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
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
    public partial class EditBlock
    {
        [Inject] IBLazQuestionType BLazQuestionType { get; set; }
        [Inject] IBlazQuestionCategoryService BLazQuestionCategory { get; set; }
        [Inject] IBlazItemBankService BlazItemBankService { get; set; }
        [Inject] IBlazDifficultyLevelService BlazDifficultyLevelService { get; set; }
        [Inject] IBlazDeltaTypeService BlazDeltaTypeService { get; set; }
        [Inject] IBlazBlockService BlazBlockService { get; set; }
        [Inject] IBlazQuestionService BlazQuestionService { get; set; }
        [Inject] IBlazSessionStorageService SessoinStorage { set; get; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Inject] IBlazQuestionLanguageService BlazLanguageService { get; set; }
        [Inject] IBlazDifficultyProfileService BlazProfileService { get; set; }
        [Inject] IBlazAuthService AuthService { get; set; }
        [Inject] IBlazGroupService BlazGroupService { get; set; }

        private CreateOrUpdateBlockRequestDto Model { get; set; } = new CreateOrUpdateBlockRequestDto();
        private List<GetOESGroupDto> OesGroupsDtos { get; set; } = [];
        private IEnumerable<GetOESGroupDto> SelectedGroups { get; set; } = [];
        private bool ConsiderDifficultyLevel { get; set; } = true;

        private List<ApprovedQuestionsPaginationDto> _selectedQuestions = [];
        private List<DifficultyLevelDto> _difficultyLevels = [];
        private List<GetDeltaTypeDto> _deltaTypes = [];
        private List<QuestionTypeDto> _questionTypes = [];
        private List<QuestionCategoryDto> _blockTypes = [];
        private List<RootItemBankDto> _itemBanks = [];
        private QuestionTypeDto _selectedTypeFilter;
        private RootItemBankDto _selectedItemBankFilter;
        private GetDeltaTypeDto _selectedDeltaTypeInput;
        private DifficultyLevelDto _selectedDifficultyLevelInput = new();
        private QuestionCategoryDto _selectedBlockType = new();
        private List<LanguageDto> _languages = [];
        private List<ProfileDto> _difficultyProfiles = [];
        private LanguageDto _selectedLanguage = new();
        private ProfileDto _selectedDifficultyProfileInput = new();
        private QuestionFilterPaginationModel _questionFilterPaginationModel = new();
        private int _questionsListChangingKey;
        private bool _isLoading = false;
        private string _originalName;
        private string _originalCode;
        private string _originalDescription;
        private long _originalTypeId;
        private long _originalDeltaTypeId;
        private List<long> _originalQuestionIds = [];
        private List<Guid> _originalGroupIds = [];
        private long? _originalDifficultyLevelId;
        private bool _hasInitialDataLoaded = false;

        private bool IsSubmitButtonDisabled
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Model?.Name) ||
                    string.IsNullOrWhiteSpace(Model?.Code) ||
                    string.IsNullOrWhiteSpace(_selectedBlockType?.Name) ||
                    (_selectedDifficultyLevelInput?.Id ?? 0) <= 0 ||
                    _selectedQuestions.Count == 0)
                {
                    return true;
                }

                if (!_hasInitialDataLoaded)
                {
                    return true;
                }

                long currentTypeId = _selectedBlockType?.Id ?? 0;
                long currentDeltaTypeId = _selectedDeltaTypeInput.Id;
                long? currentDifficultyLevelId = _selectedDifficultyLevelInput?.Id;
                var currentQuestionIds = _selectedQuestions.Select(q => q.Id).ToList();
                var currentGroupIds = SelectedGroups.Select(g => g.Id).ToList();
                bool hasChanges =
                    Model.Name != _originalName ||
                    Model.Code != _originalCode ||
                    Model.Description != _originalDescription ||
                    currentTypeId != _originalTypeId ||
                    currentDeltaTypeId != _originalDeltaTypeId ||
                    currentDifficultyLevelId != _originalDifficultyLevelId ||
                    !currentQuestionIds.Order().SequenceEqual(_originalQuestionIds.Order()) ||
                    !currentGroupIds.Order().SequenceEqual(_originalGroupIds.Order());

                return !hasChanges;
            }
        }

        public RootItemBankDto SelectedItemBank
        {
            get => _selectedItemBankFilter;
            set
            {
                if (_selectedItemBankFilter != value)
                {
                    _selectedItemBankFilter = value;
                    _questionFilterPaginationModel._selectedItemBank = _selectedItemBankFilter?.Id ?? 0;
                    _questionsListChangingKey += 1;
                }
            }
        }

        public QuestionTypeDto SelectedQuestionType
        {
            get => _selectedTypeFilter;
            set
            {
                if (_selectedTypeFilter != value)
                {
                    _selectedTypeFilter = value;
                    _questionFilterPaginationModel._selectedType = _selectedTypeFilter?.Id ?? 0;
                    _questionsListChangingKey += 1;
                }
            }
        }


        protected override async Task OnInitializedAsync()
        {
            var id = await SessoinStorage.GetValue<long>("PerformEditBtnClick");

            // Create all tasks
            var responseTask = BlazBlockService.GetBlockDataById(id);
            var questionCategoriesTask = BLazQuestionCategory.GetCategories();
            var deltaTypesTask = BlazDeltaTypeService.GetDeltaTypes();
            var difficultyProfilesTask = BlazProfileService.GetProfiles();
            var questionTypesTask = BLazQuestionType.GetAllQuestionType();
            var itemBanksTask = BlazItemBankService.GetItemBanksListAsync();
            var languagesTask = BlazLanguageService.GetAllLanguagesAsync();

            // Run all tasks concurrently
            await Task.WhenAll(
                responseTask,
                questionCategoriesTask,
                deltaTypesTask,
                difficultyProfilesTask,
                questionTypesTask,
                itemBanksTask,
                languagesTask
            );

            // Assign results with null handling
            var response = await responseTask;
            _blockTypes = await questionCategoriesTask ?? [];
            _deltaTypes = await deltaTypesTask ?? [];
            _questionTypes = await questionTypesTask ?? [];
            _difficultyProfiles = await difficultyProfilesTask ?? [];
            _itemBanks = await itemBanksTask ?? [];
            _languages = await languagesTask ?? [];

            Model = new CreateOrUpdateBlockRequestDto
            {
                Id = response.Id,
                Name = response.Name,
                Code = response.Code,
                Description = response.Description,
                DeltaTypeId = response.DeltaTypeId,
                DifficultyLevelId = response.DifficultyLevelId,
                BlockTypeId = response.BlockTypeId,
                QuestionsIds = [.. response.Questions.Select(q => q.Id)],
                LanguageId = response.LanguageId,
            };

            _originalName = Model.Name;
            _originalCode = Model.Code;
            _originalDescription = Model.Description;
            _originalTypeId = Model.BlockTypeId;
            _originalDeltaTypeId = Model.DeltaTypeId;
            _originalDifficultyLevelId = Model.DifficultyLevelId;
            _originalQuestionIds = [.. Model.QuestionsIds];
            _selectedLanguage = _languages.Find(x => x.Id == response.LanguageId);

            _selectedQuestions = response.Questions;

            _selectedDeltaTypeInput = _deltaTypes.Find(x => x.Id == (int)Model.DeltaTypeId);
            _selectedDifficultyProfileInput = _difficultyProfiles.Find(x => x.Id == response.DifficultyProfileId);
            _difficultyLevels = await BlazDifficultyLevelService.GetDifficultyLevelByProfileIdAsync(response.DifficultyProfileId);
            _selectedDifficultyLevelInput = _difficultyLevels.Find(x => x.Id == Model.DifficultyLevelId);
            _selectedBlockType = _blockTypes.Find(x => x.Id == (int)Model.BlockTypeId);

            ConsiderDifficultyLevel = response.ConsiderDifficultyLevel;
            Model.ConsiderDifficultyLevel = ConsiderDifficultyLevel;

            _questionFilterPaginationModel._selectedDeltaType = _selectedDeltaTypeInput?.Id ?? 0;
            _questionFilterPaginationModel._selectedDifficultyLevel = _selectedDifficultyLevelInput?.Id ?? 0;
            _questionFilterPaginationModel._selectedCategory = _selectedBlockType?.Id ?? 0;
            _questionFilterPaginationModel._isConsiderDifficulty = ConsiderDifficultyLevel;

            OesGroupsDtos = await BlazBlockService.GetUserBlockGroupsAsync();
            var blockGroups = await BlazBlockService.GetBlockGroupsAsync(Model.Id);
            var modelGroupIds = blockGroups?.GroupsIds ?? [];

            SelectedGroups = [.. OesGroupsDtos.Where(g => modelGroupIds.Contains(g.Id))];

            _originalGroupIds = [.. SelectedGroups.Select(g => g.Id)];

            _hasInitialDataLoaded = true;
            _isLoading = true;

            StateHasChanged();
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
            Model.QuestionsIds = _selectedQuestions.ConvertAll(q => q.Id);
            Model.BlockTypeId = _selectedBlockType?.Id ?? 0;
            Model.DeltaTypeId = _selectedDeltaTypeInput?.Id ?? 0;
            Model.DifficultyLevelId = _selectedDifficultyLevelInput?.Id ?? 0;
            Model.OESGroupIds = [.. SelectedGroups.Select(g => g.Id)];
            var response = await BlazBlockService.EditBlock(Model);

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

            var dialog = DialogService.Show<GenericDialog>(string.Empty, parameters, options);
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
            var previousDeltaType = _selectedDeltaTypeInput;

            if (_selectedQuestions.Any() &&
                selectedDeltaType != null &&
                (previousDeltaType == null || previousDeltaType.Id != selectedDeltaType.Id))
            {
                var confirmed = await ConfirmChangeAsync("Changing delta type will remove all selected questions. Do you want to continue?");
                if (!confirmed) return;

                ClearSelectedQuestions("Selected questions have been removed due to delta type change.");
            }

            _selectedDeltaTypeInput = selectedDeltaType;

            if (_selectedDeltaTypeInput != null)
            {
                _difficultyLevels = await BlazDifficultyLevelService.GetDifficultyLevelsByDeltaTypeId(_selectedDeltaTypeInput.Id);
            }
            else
            {
                _difficultyLevels = [];
            }

            _selectedDifficultyLevelInput = null;
            _questionFilterPaginationModel._selectedDeltaType = _selectedDeltaTypeInput?.Id ?? 0;
            _questionFilterPaginationModel._selectedDifficultyLevel = 0;

            _questionsListChangingKey++;

            StateHasChanged();
        }

        private async Task OnBlockTypeChanged(QuestionCategoryDto selectedBlocktype)
        {
            var previousBlockType = _selectedBlockType;

            if (_selectedQuestions.Any() &&
                selectedBlocktype != null &&
                (previousBlockType == null || previousBlockType.Id != selectedBlocktype.Id))
            {
                var confirmed = await ConfirmChangeAsync("Changing block type will remove all selected questions. Do you want to continue?");
                if (!confirmed) return;

                ClearSelectedQuestions("Selected questions have been removed due to block type change.");
            }

            _selectedBlockType = selectedBlocktype;
            _questionFilterPaginationModel._selectedCategory = _selectedBlockType?.Id ?? 0;
            _questionsListChangingKey++;

            StateHasChanged();
        }

        private void SelectedQuestionChanged(object selectedQuestion)
        {
            _selectedQuestions.Add((ApprovedQuestionsPaginationDto)selectedQuestion);

            StateHasChanged();
        }

        private async Task OpenGroupDialog()
        {
            var allowed = await AuthService.IsCurrentUserOwnerAsync(
                Model.Id,
                BlazBlockService.GetBlockGroupsAsync,
                dto => [dto.OwnerGroupId ?? Guid.Empty]
            );

            if (!allowed)
            {
                Snackbar.Add(string.Format(Resource.OnlyCreatorCanManageGroups, Resource.Block), Severity.Error);
                return;
            }

            var result = await DialogInteractionService.OpenSelectionDialogAsync(
                dialogService: DialogService,
                title: Resource.SelectGroup,
                preSelectedItems: SelectedGroups,
                endpointService: async pagination =>
                {
                    var allGroups = await BlazBlockService.GetUserBlockGroupsAsync();

                    if (!string.IsNullOrWhiteSpace(pagination.SearchKey))
                    {
                        allGroups = [.. allGroups.Where(x => x.Name.Contains(pagination.SearchKey, StringComparison.OrdinalIgnoreCase))];
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

            SelectedGroups = [.. result
                .GroupBy(x => x.Id)
                .Select(g => g.First())
            ];
        }

        private async Task DeleteGroupAsync(Guid groupId)
        {
            var response = await BlazGroupService.DeleteGroupAsync(groupId);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
                Snackbar.Add(response.Message, Severity.Success);
            else
                Snackbar.Add(response.Message, Severity.Error);
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
    }
}
