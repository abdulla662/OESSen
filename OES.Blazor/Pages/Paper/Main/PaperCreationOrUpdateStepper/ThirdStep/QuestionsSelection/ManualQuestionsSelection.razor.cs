using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.MainComponent;
using OES.Blazor.Services.Interfaces.DifficultyLevel;
using OES.Blazor.Services.Interfaces.Form;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.QuestionType;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.Dtos.Paper.Requests;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.ThirdStep.QuestionsSelection
{
    public partial class ManualQuestionsSelection
    {
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazDifficultyLevelService BlazDifficulyLevelService { get; set; }
        [Inject] private IBLazQuestionType BLazQuestionType { get; set; }
        [Inject] private IBlazQuestionService BlazQuestionService { get; set; }
        [Inject] private IBlazPaperService BlazPaperService { get; set; }
        [Inject] private IBlazFormService BlazFormService { get; set; }
        [Inject] private IBlazItemBankService BlazItemBankService { get; set; }
        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; }

        [Parameter] public PaperMetadataResultedParamsDto PaperMetadataResultedParamsDto { get; set; } = new();

        private Dictionary<long, HashSet<ManualQuestionsPaginationResponseDto>> _questionsByForm = [];
        private readonly HashSet<ManualQuestionsPaginationResponseDto> _tableSelectedItems = [];
        private List<DifficultyLevelDto> _difficultyLevels = [];
        private List<QuestionTypeDto> _questionTypes = [];
        private List<ItemBanksFromItemBankPointResponseDto> _itemBanks = [];
        private List<GetFormDto> _forms = [];
        private DifficultyLevelDto _selectedDifficultyLevel;
        private List<long> _oldSelectedQuestionIds = [];
        private QuestionTypeDto _selectedType;
        private ItemBanksFromItemBankPointResponseDto _selectedItemBank;
        private List<TreeItemResponseDto> _branches = [];
        private TreeItemResponseDto _selectedBranch;
        private long? _selectedFormId;
        private int _totalUsedCount;
        private int _questionsListChangingKey;
        private string _selectedQuestionsSearchString;
        private long? _activeFormId;
        private GetFormDto _activeForm;
        private bool _isAddingNewForm;
        private StepperOperationalMode _thisComponentCurrentOperationalMode = StepperOperationalMode.InsertionMode;
        PaperStepperFormsMode _paperStepperFormsMode;

        private decimal _deltaMin = 0;
        private decimal _deltaMax = 1;

        private bool IsCurrentPaperStepperModeUpdateMode => _thisComponentCurrentOperationalMode == StepperOperationalMode.UpdateMode;
        private bool IsCurrentFormsModeContinuePendingForm => _paperStepperFormsMode == PaperStepperFormsMode.ContinuePendingForm;
        private bool IsCurrentFormsModeSingleManualForm => _paperStepperFormsMode == PaperStepperFormsMode.AddNormalSingleManualForm || IsCurrentFormsModeContinuePendingForm;
        private bool IsCurrentFormsModeAddExcelSheetSingleManualForm => _paperStepperFormsMode == PaperStepperFormsMode.AddExcelSheetSingleManualForm;
        private long? DisplayedFormId => _selectedFormId ?? _activeFormId;

        public QuestionFilterPaginationModel QuestionFilterPaginationModel { get; set; } = new();

        public ItemBanksFromItemBankPointResponseDto SelectedItemBank
        {
            get => _selectedItemBank;
            set
            {
                if (_selectedItemBank != value)
                {
                    _selectedItemBank = value;

                    QuestionFilterPaginationModel._selectedItemBankSignature = _selectedItemBank?.ItemBankSignature;
                    QuestionFilterPaginationModel._selectedItemBank = 0;
                    _selectedBranch = null;
                    _branches = [];
                    QuestionFilterPaginationModel._selectedBranchId = 0;

                    if (_selectedItemBank != null)
                        _ = LoadBranchesAsync(_selectedItemBank.Id, _selectedItemBank.ItemBankSignature);

                    _questionsListChangingKey++;
                }
            }
        }

        public DifficultyLevelDto SelectedDifficultyLevel
        {
            get => _selectedDifficultyLevel;
            set
            {
                if (_selectedDifficultyLevel == value) return;

                _selectedDifficultyLevel = value;
                QuestionFilterPaginationModel._selectedDifficultyLevel = _selectedDifficultyLevel?.Id ?? 0;

                decimal Min = 0;
                decimal Max = 1;

                if (_selectedDifficultyLevel is not null)
                {
                    Min = Math.Min(_selectedDifficultyLevel.FromDelta, _selectedDifficultyLevel.ToDelta);
                    Max = Math.Max(_selectedDifficultyLevel.FromDelta, _selectedDifficultyLevel.ToDelta);
                }

                OnDeltaRangeChanged((Min, Max));

                _questionsListChangingKey++;
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
                    QuestionFilterPaginationModel._selectedType = _selectedType?.Id ?? 0;
                    _questionsListChangingKey++;
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
                    QuestionFilterPaginationModel._selectedBranchId = _selectedBranch?.Id ?? 0;
                    _questionsListChangingKey++;
                }
            }
        }

        private int MaxSelectableQuestions => (int)(PaperMetadataResultedParamsDto.QuestionsCount * PaperMetadataResultedParamsDto.OutputFormsCount);

        public long? SelectedFormId
        {
            get => _selectedFormId;
            set
            {
                if (_selectedFormId == value)
                {
                    return;
                }

                _selectedFormId = value;

                if (!_isAddingNewForm && !(IsCurrentFormsModeSingleManualForm || IsCurrentFormsModeAddExcelSheetSingleManualForm))
                {
                    _activeFormId = _selectedFormId ?? DetectActiveFormId();
                    _activeForm = _forms.Find(f => f.Id == _activeFormId);
                }

                if (_selectedFormId.HasValue)
                {
                    if (!_questionsByForm.ContainsKey(_selectedFormId.Value))
                    {
                        _ = LoadSelectedQuestionsForFormAsync(_selectedFormId.Value);
                    }
                }

                InvokeAsync(StateHasChanged);
            }
        }

        protected override async Task OnInitializedAsync()
        {
            var formsTask = BlazFormService.GetAllFormsByPaperIdAsync(PaperMetadataResultedParamsDto.PaperId);
            var paperStepperFormsModeTask = BlazSessionStorageService.GetValue<int>(nameof(PaperStepperFormsMode));
            var difficultyLevelsTask = BlazDifficulyLevelService.GetDifficultyLevelByProfileIdAsync(PaperMetadataResultedParamsDto.DifficultyProfileId);
            var questionTypesTask = BLazQuestionType.GetAllQuestionType();
            var itemBanksTask = BlazPaperService.GetAllItemBanksFromItemBankPointsAsync(PaperMetadataResultedParamsDto.PaperId);

            await Task.WhenAll(formsTask, paperStepperFormsModeTask, difficultyLevelsTask, questionTypesTask, itemBanksTask);

            _forms = formsTask.Result ?? [];
            _difficultyLevels = difficultyLevelsTask.Result;
            _questionTypes = questionTypesTask.Result;
            _itemBanks = [.. (itemBanksTask.Result ?? []).Where(x => string.IsNullOrWhiteSpace(x.FullPath) || !x.FullPath.Contains('>'))];

            _paperStepperFormsMode = (PaperStepperFormsMode)(int)paperStepperFormsModeTask.Result;

            _isAddingNewForm = _forms.Count < PaperMetadataResultedParamsDto.OutputFormsCount;

            _activeFormId = _isAddingNewForm ? -1 : DetectActiveFormId();

            _activeForm = _forms.Find(f => f.Id == _activeFormId);

            var scopedFormId = _isAddingNewForm ? null : _activeFormId;

            var result = await BlazPaperService.GetManuallySelectedQuestionsForUpdateAsync(
                PaperMetadataResultedParamsDto.PaperId,
                scopedFormId);

            if (result.Questions.Count > 0)
            {
                _thisComponentCurrentOperationalMode = StepperOperationalMode.UpdateMode;

                if (_activeFormId.HasValue)
                {
                    foreach (var q in result.Questions)
                    {
                        q.FormId ??= _activeFormId.Value;
                    }

                    _questionsByForm[_activeFormId.Value] = [.. result.Questions];
                }
            }

            _totalUsedCount = result.TotalUsedCount;

            if (result.Questions.Count > 0)
            {
                _thisComponentCurrentOperationalMode = StepperOperationalMode.UpdateMode;

                if (_activeFormId.HasValue)
                {
                    _questionsByForm[_activeFormId.Value] = [.. result.Questions];
                }
            }

            if (IsCurrentPaperStepperModeUpdateMode || IsCurrentFormsModeSingleManualForm || IsCurrentFormsModeAddExcelSheetSingleManualForm)
            {
                _oldSelectedQuestionIds = [.. _questionsByForm.Values.SelectMany(x => x).Select(x => x.PaperItemBankQuestionId)];
            }

            OnDeltaRangeChanged((_deltaMin, _deltaMax));
        }

        private long? DetectActiveFormId()
        {
            if (_forms.Count == 0) return null;
            return _forms.OrderBy(f => f.Id).Last().Id;
        }

        private async Task LoadBranchesAsync(long itemBankId, string signature)
        {
            _branches = await BlazItemBankService.GetChildrenByParentIdAsync(itemBankId, signature);
            await InvokeAsync(StateHasChanged);
        }

        private async Task<CustomTableData<ManualQuestionsPaginationResponseDto>> GetAllQuestions(PaginationSearchModel paginationSearchModel)
        {
            if (PaperMetadataResultedParamsDto.PaperId == 0)
                return new CustomTableData<ManualQuestionsPaginationResponseDto>([], 0);

            return await BlazPaperService.GetAvailableManualQuestionsForPaperAsync(PaperMetadataResultedParamsDto.PaperId, paginationSearchModel);
        }

        private void OnTableSelectionChanged(HashSet<ManualQuestionsPaginationResponseDto> _)
        {
            // You don't need this statement here _selectedQuestions = selectedQuestions;, since the reference of _selectedQuestions is updated internally with the newly selected/deselected items.

            StateHasChanged();
        }

        private void SelectedQuestionsChanged(ManualQuestionsPaginationResponseDto item)
        {
            if (item == null || IsSelectionDisabled(item))
            {
                Snackbar.Add(Resource.TotalQuestionsLimitReached, Severity.Warning);
                return;
            }

            bool alreadySelectedInSameForm = ActiveQuestions.Any(q =>
                q.QuestionMetadataId == item.QuestionMetadataId &&
                q.FormId == _activeFormId
            );

            if (alreadySelectedInSameForm)
            {
                Snackbar.Add(Resource.QuestionSelectedInTheSameForm, Severity.Warning);
                return;
            }

            var newSelection = new ManualQuestionsPaginationResponseDto
            {
                QuestionMetadataId = item.QuestionMetadataId,
                Body = item.Body,
                Code = item.Code,
                QuestionType = item.QuestionType,
                ItemBankPointId = item.ItemBankPointId,
                ItemBankName = item.ItemBankName,
                DifficultyLevelId = item.DifficultyLevelId,
                DifficultyLevelName = item.DifficultyLevelName,
                PaperQuestionStatus = item.PaperQuestionStatus,
                PaperItemBankQuestionId = item.PaperItemBankQuestionId,
                SectionId = item.SectionId,
                FormId = _activeFormId,
                UsedInFormsCount = item.UsedInFormsCount,
                SubQuestionsCount = item.SubQuestionsCount,
                InstanceId = Guid.NewGuid()
            };

            ActiveQuestions.Add(newSelection);

            if (newSelection.PaperQuestionStatus == PaperQuestionStatus.Used)
            {
                _totalUsedCount += newSelection.SubQuestionsCount;
            }

            Snackbar.Add(Resource.QuestionSelectedSuccessfully, Severity.Success, config =>
            {
                config.DuplicatesBehavior = SnackbarDuplicatesBehavior.Allow;
                config.VisibleStateDuration = 1000;
            });

            StateHasChanged();
        }

        private void RemoveQuestion(ManualQuestionsPaginationResponseDto question)
        {
            if (IsSelectedFormReadOnly) return;

            var formId = question.FormId ?? _activeFormId ?? DisplayedFormId;

            if (formId.HasValue && _questionsByForm.TryGetValue(formId.Value, out var questions))
            {
                questions.Remove(question);

                if (question.PaperQuestionStatus == PaperQuestionStatus.Used)
                {
                    _totalUsedCount -= question.SubQuestionsCount;
                }
            }
        }

        private void ClearAllQuestions()
        {
            if (IsSelectedFormReadOnly) return;

            var countToRemove = ActiveQuestions
                .Where(x => x.PaperQuestionStatus == PaperQuestionStatus.Used)
                .Sum(x => x.SubQuestionsCount);

            ActiveQuestions.Clear();

            _totalUsedCount -= countToRemove;
        }


        // FORM SUBMIT METHODS

        public async Task<bool> OnThirdStepQuestionsManualSelectionSubmitAsync()
        {
            if (ValidateSelectedQuestions())
            {
                var selectedQuestions = ActiveQuestions
                    .Select(question => new QuestionSaveDto(
                        question.PaperItemBankQuestionId,
                        question.QuestionMetadataId,
                        question.ItemBankPointId,
                        question.DifficultyLevelId,
                        question.SectionId,
                        question.PaperQuestionStatus,
                        question.SubQuestionsCount))
                    .ToList();

                var manuallySelectedQuestionsDto = new AddOrUpdateManualQuestionsRequestDto(selectedQuestions, PaperMetadataResultedParamsDto.QuestionsCount);

                if (_thisComponentCurrentOperationalMode == StepperOperationalMode.InsertionMode)
                {
                    return await AddManuallySelectedQuestionsAsync(manuallySelectedQuestionsDto);
                }
                else if (_thisComponentCurrentOperationalMode == StepperOperationalMode.UpdateMode)
                {
                    return await UpdateManuallySelectedQuestionsAsync(manuallySelectedQuestionsDto);
                }
            }
            else
            {
                Snackbar.Add(string.Format(Resource.SelectedQuestionsWithStatusUsed, PaperMetadataResultedParamsDto.QuestionsCount * PaperMetadataResultedParamsDto.OutputFormsCount), Severity.Error);
                return false;
            }

            return false;
        }

        private async Task<bool> AddManuallySelectedQuestionsAsync(AddOrUpdateManualQuestionsRequestDto addManualQuestionsRequestDto)
        {
            var response = await BlazPaperService.AddManuallySelectedQuestionsAsync(PaperMetadataResultedParamsDto.PaperId, addManualQuestionsRequestDto);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _thisComponentCurrentOperationalMode = StepperOperationalMode.UpdateMode;

                Snackbar.Add(response.Message, Severity.Success);

                return true;
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            return false;
        }

        private async Task<bool> UpdateManuallySelectedQuestionsAsync(AddOrUpdateManualQuestionsRequestDto updateManualQuestionsRequestDto)
        {
            var response = await BlazPaperService.UpdateManuallySelectedQuestionsAsync(updateManualQuestionsRequestDto, PaperMetadataResultedParamsDto.PaperId, _activeFormId == -1 ? null : _activeFormId);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);

                return true;
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            return false;
        }


        // MISC & VALIDATION METHODS

        private static string GetStatusIcon(PaperQuestionStatus status) => status switch
        {
            PaperQuestionStatus.Used => Icons.Material.Filled.CheckCircle,
            PaperQuestionStatus.Hanged => Icons.Material.Filled.Cancel,
            _ => Icons.Material.Filled.Help
        };

        private static Color GetStatusColor(PaperQuestionStatus status) => status switch
        {
            PaperQuestionStatus.Used => Color.Success,
            PaperQuestionStatus.Hanged => Color.Warning,
            _ => Color.Default
        };

        private string GetFormName(long? formId)
        {
            if (!formId.HasValue) return "";

            return _forms?.Find(f => f.Id == formId.Value)?.Name ?? "";
        }

        private void OnDeltaRangeChanged((decimal Min, decimal Max) r)
        {
            _deltaMin = Math.Round(r.Min, 2);
            _deltaMax = Math.Round(r.Max, 2);

            QuestionFilterPaginationModel._fromDelta = _deltaMin;
            QuestionFilterPaginationModel._toDelta = _deltaMax;
        }

        private bool FilterSelectedQuestions(ManualQuestionsPaginationResponseDto question)
        {
            if (_selectedFormId.HasValue)
            {
                if (question.FormId != _selectedFormId.Value)
                {
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(_selectedQuestionsSearchString))
            {
                return true;
            }

            return question.Code?.Contains(_selectedQuestionsSearchString, StringComparison.OrdinalIgnoreCase) == true;
        }

        private void ResetFilters()
        {
            if (SelectedItemBank is not null || SelectedType is not null ||
                SelectedDifficultyLevel is not null || SelectedBranch is not null ||
                _deltaMin != 0 || _deltaMax != 0)
            {
                SelectedItemBank = null;
                SelectedType = null;
                SelectedDifficultyLevel = null;

                OnDeltaRangeChanged((0, 1));

                _questionsListChangingKey++;
            }
        }

        private bool ValidateSelectedQuestions() => TotalUsedSelectedCount == MaxSelectableQuestions;

        private bool IsOldQuestion(ManualQuestionsPaginationResponseDto question)
        {
            return _oldSelectedQuestionIds.Contains(question.PaperItemBankQuestionId) && question.SectionId != null;
        }

        private async Task LoadSelectedQuestionsForFormAsync(long formId)
        {
            if (_questionsByForm.ContainsKey(formId))
            {
                await InvokeAsync(StateHasChanged);
                return;
            }

            var result = await BlazPaperService.GetManuallySelectedQuestionsForUpdateAsync(
                PaperMetadataResultedParamsDto.PaperId,
                formId);

            foreach (var q in result.Questions)
            {
                if (q.FormId == null)
                {
                    q.FormId = formId;
                }
            }

            _questionsByForm[formId] = [.. result.Questions];

            await InvokeAsync(StateHasChanged);
        }

        private int GetActiveFormUsageCount(long metadataId)
        {
            if (!_activeFormId.HasValue) return 0;

            if (_questionsByForm.TryGetValue(_activeFormId.Value, out var questions))
            {
                return questions.Count(x => x.QuestionMetadataId == metadataId);
            }

            return 0;
        }

        #region Helper properties

        private int TotalUsedSelectedCount => _totalUsedCount;

        private bool IsTotalLimitReached
            => TotalUsedSelectedCount >= MaxSelectableQuestions;


        private bool IsSelectedFormReadOnly => DisplayedFormId.HasValue && DisplayedFormId != _activeFormId;

        private HashSet<ManualQuestionsPaginationResponseDto> CurrentQuestions
        {
            get
            {
                if (!DisplayedFormId.HasValue)
                {
                    return [];
                }

                if (!_questionsByForm.ContainsKey(DisplayedFormId.Value))
                {
                    _questionsByForm[DisplayedFormId.Value] = [];
                }

                return _questionsByForm[DisplayedFormId.Value];
            }
        }

        private HashSet<ManualQuestionsPaginationResponseDto> ActiveQuestions
        {
            get
            {
                if (!_activeFormId.HasValue)
                {
                    return [];
                }

                if (!_questionsByForm.ContainsKey(_activeFormId.Value))
                {
                    _questionsByForm[_activeFormId.Value] = [];
                }

                return _questionsByForm[_activeFormId.Value];
            }
        }

        private int GetCurrentUsageCount(long metadataId)
        {
            return _questionsByForm.Values
                .SelectMany(x => x)
                .Count(x => x.QuestionMetadataId == metadataId);
        }

        private bool IsQuestionLimitReached(long metadataId)
            => GetCurrentUsageCount(metadataId) >= PaperMetadataResultedParamsDto.OutputFormsCount;

        private bool IsSelectionDisabled(ManualQuestionsPaginationResponseDto item)
            => IsTotalLimitReached || IsQuestionLimitReached(item.QuestionMetadataId) || IsAlreadySelectedInSameForm(item) || IsSelectedFormReadOnly;

        private bool IsAlreadySelectedInSameForm(ManualQuestionsPaginationResponseDto item)
        {
            return ActiveQuestions.Any(q =>
                q.QuestionMetadataId == item.QuestionMetadataId &&
                q.FormId == _activeFormId);
        }

        private string GetSelectionTooltip(ManualQuestionsPaginationResponseDto item)
        {
            if (IsSelectedFormReadOnly) return Resource.CannotRemoveQuestionFromSelectedPool;
            if (IsTotalLimitReached) return Resource.TotalQuestionsLimitReached;
            if (IsQuestionLimitReached(item.QuestionMetadataId)) return Resource.QuestionRepeatedInAllForms;
            return Resource.SelectQuestion;
        }

        #endregion
    }
}