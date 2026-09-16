using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using OES.Blazor.Dialogs.Paper.ImportQuestionsWithItemBanksDialog;
using OES.Blazor.Dialogs.Paper.PaperTemplates;
using OES.Blazor.Dialogs.Question.TemplateDialog;
using OES.Blazor.Extentions.DialogHelpers;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.DifficultyProfile;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Blazor.Services.Interfaces.Paper.Transition;
using OES.Blazor.Services.Interfaces.QuestionCataegory;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Blazor.Services.Interfaces.Subject;
using OES.Blazor.Services.Interfaces.TransitionProfile;
using OES.Helper.Dtos;
using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Paper.Requests;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Dtos.Subject;
using OES.Helper.Dtos.TransitionProfile;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using SharedHelper.Enums;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.FirstStep
{
    public partial class PaperMetadata : ComponentBase
    {
        [Inject] private IBlazPaperService PaperService { get; set; }
        [Inject] private IBlazDifficultyProfileService BlazProfileService { get; set; }
        [Inject] private IBlazQuestionLanguageService BlazQuestionLanguageService { get; set; }
        [Inject] private IBlazSubjectService BlazSubjectService { get; set; }
        [Inject] private IBlazTransitionProfileService BlazTransitionProfile { get; set; }
        [Inject] private IBlazTransitionLevelService BlazTransitionLevel { get; set; }
        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] private IBlazQuestionCategoryService BLazQuestionCategory { get; set; }
        [Inject] private IBlazAuthService AuthService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Inject] private CRUD_Dto CrudDto { get; set; }
        [Inject] private IBlazGroupService BlazGroupService { get; set; }

        private bool TransitionProfileLevelsErrorExists => _transitionProfileHasError || (_isSubmitButtonHit && (_selectedTransitionProfile == null || _selectedTransitionProfile.Id == 0));
        private string TransitionProfileLevelsErrorMessage => _transitionProfileHasError ? _transitionProfileErrorMessage : Resource.TransitionProfileIsRequired;
        private bool IsCurrentFormsModeExcelSheetSingleManualForm => _paperStepperFormsMode == PaperStepperFormsMode.AddExcelSheetSingleManualForm;
        private bool IsCurrentFormsModeSingleManualForm => _paperStepperFormsMode is PaperStepperFormsMode.AddNormalSingleManualForm or PaperStepperFormsMode.AddExcelSheetSingleManualForm;
        private bool IsCurrentFormsModeSingleAutoForm => _paperStepperFormsMode == PaperStepperFormsMode.AddSingleAutoForm;
        private bool IsCurrentFormsModeContinuePendingForm => _paperStepperFormsMode == PaperStepperFormsMode.ContinuePendingForm;
        private bool IsCurrentFormsModeSingleForm => IsCurrentFormsModeSingleManualForm || IsCurrentFormsModeSingleAutoForm || IsCurrentFormsModeContinuePendingForm;
        private List<GetOESGroupDto> SelectedGroups { get; set; } = [];

        private QuestionIdsAndItemBankIdsDto _uploadedQuestions;
        private string _name;
        private string _description;
        private string _code;
        private string _abbreviation;
        private int _questionsCount;
        private float _duration;
        private double _totalMarks;
        private long _outputFormsCount = 1;
        private bool _allowInstantResult;
        private bool _usesExcelQuestionsImport;
        private IList<IBrowserFile> _files = [];
        private List<string> _errorListDto = [];
        private List<long> _availableLanguagesIdsToChooseFromWhenExcelImport = [];
        private bool _isPaperStandard;
        private StepperOperationalMode _thisComponentCurrentOperationalMode = StepperOperationalMode.InsertionMode;

        // Adaptive paper related fields
        private int _stagesCount = 3;
        private List<QuestionCategoryDto> _allCategories = [];
        private List<QuestionCategoryDto> _adaptiveCategories = [];
        private List<QuestionCategoryDto> _adaptiveMSTOrderedCategories = [];
        private readonly Dictionary<long, decimal[]> _adaptiveMSTCategoryPathValues = [];
        private readonly Dictionary<long, decimal> _categoryFixedDPaths = [];
        private DPathCalculationMode _selectedDPathCalculationMode = DPathCalculationMode.ManualFinalScore;
        private bool _transitionProfileHasError = false;
        private string _transitionProfileErrorMessage = string.Empty;
        private AdaptivePaperSubtype _selectedAdaptiveSubtype = AdaptivePaperSubtype.MST;
        private bool IsAdaptiveMST => _isAdaptive && _selectedAdaptiveSubtype == AdaptivePaperSubtype.MST;
        private bool IsAdaptiveStep => _isAdaptive && (_selectedAdaptiveSubtype == AdaptivePaperSubtype.STEP);
        private int StagesCountMax => IsAdaptiveStep ? 20 : 10;
        // Adaptive paper related fields

        private PaperType _selectedType;
        private QuestionSelectionType _selectedQuestionSelection;
        private QuestionContentType _selectedQuestionsContentType = QuestionContentType.TextOnly;
        internal AddOrUpdatePaperMetadataResponseDto _addPaperMetadataResponseDto = new();

        private List<LanguageDto> Languages = [];
        private List<ProfileDto> Profiles = [];
        private List<SubjectDto> Subjects = [];
        private List<TransitionProfileDto> transitionProfileDtos = [];

        private ProfileDto _selectedProfile = new();
        private LanguageDto _selectedLanguage = new();
        private List<SubjectDto> _selectedSubjects = [];
        private TransitionProfileDto _selectedTransitionProfile = new();
        private AddOrUpdatePaperMetadataRequestDto _currentPaperMetadataDto = new();
        private PaperMetadataSavingRequestDto _metadataTemplateDto = new();
        private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
        private bool _isSubmitButtonHit;
        private bool _isAdaptive;
        private bool _isAdaptiveWithInstant = false;
        private bool _isStepPlus;
        private bool _criticalFieldsDisabled = false;
        private bool _isExternalExcelSheetManualFormAddedSuccessfully = false;
        private QuestionDistributionTypeInForm _selectedQuestionDistributionTypeInForm = QuestionDistributionTypeInForm.TotallyDistinct;
        private PaperStepperFormsMode _paperStepperFormsMode;


        // DATA LOADING & CHANGING METHODS

        protected override async Task OnInitializedAsync()
        {
            _isPaperStandard = _selectedType == PaperType.Standard;
            _selectedQuestionsContentType = QuestionContentType.TextOnly;

            // Create all tasks
            var profilesTask = BlazProfileService.GetProfiles();
            var languagesTask = BlazQuestionLanguageService.GetAllLanguagesAsync();
            var subjectsTask = BlazSubjectService.GetAllSubjectsAsync();
            var categoriesTask = BLazQuestionCategory.GetCategories();

            // Run all tasks concurrently
            await Task.WhenAll(
                profilesTask,
                languagesTask,
                subjectsTask,
                categoriesTask
            );

            // Assign results
            Profiles = await profilesTask;
            Languages = await languagesTask;
            Subjects = await subjectsTask;
            _allCategories = await categoriesTask;
            _adaptiveCategories = [.. _allCategories.Where(c => c.StandardDeviation.HasValue)];

            CrudDto.IsCreatePage = await BlazSessionStorageService.GetValue<bool>("IsCreatePage");

            _addPaperMetadataResponseDto.PaperId = await BlazSessionStorageService.GetValue<long>("PerformEditBtnClick");

            var paperStepperFormsMode = await BlazSessionStorageService.GetValue<int>(nameof(PaperStepperFormsMode));
            _paperStepperFormsMode = (PaperStepperFormsMode)paperStepperFormsMode;

            if (!CrudDto.IsCreatePage && _addPaperMetadataResponseDto.PaperId > 0)
            {
                DisableCriticalPaperMetadataFields();
                _thisComponentCurrentOperationalMode = StepperOperationalMode.UpdateMode;
                await PrepareFirstStepForUpdateModeAsync(_addPaperMetadataResponseDto.PaperId);
            }
        }

        private void OnPaperNameChanged(string name)
        {
            _name = name;

            _code = name.Trim().Replace(" ", "-");

            StateHasChanged();
        }

        private void OnSubjectsChanged(IEnumerable<SubjectDto> subjectsDtos)
        {
            _selectedSubjects = [.. subjectsDtos];

            StateHasChanged();
        }

        private async Task OnTypeChangedAsync(PaperType paperType)
        {
            _selectedType = paperType;

            _isPaperStandard = _selectedType == PaperType.Standard;

            _isAdaptive = false;

            if (_selectedType == PaperType.Adaptive)
            {
                _isAdaptive = true;
                _selectedAdaptiveSubtype = AdaptivePaperSubtype.MST;
                transitionProfileDtos = await BlazTransitionProfile.GetProfiles();
                _allowInstantResult = true;
                _isAdaptiveWithInstant = true;
                _stagesCount = 3;
                _transitionProfileHasError = false;
                _transitionProfileErrorMessage = string.Empty;
                _adaptiveCategories = [.. _allCategories.Where(c => c.StandardDeviation.HasValue)];
                _selectedTransitionProfile = null;
                _adaptiveCategories.Clear();
                _adaptiveMSTCategoryPathValues.Clear();
                _categoryFixedDPaths.Clear();
            }
            else
            {
                _isAdaptiveWithInstant = false;
                _selectedTransitionProfile = null;
                _adaptiveMSTCategoryPathValues.Clear();
                _categoryFixedDPaths.Clear();
                _adaptiveCategories.Clear();
                _transitionProfileHasError = false;
                _transitionProfileErrorMessage = string.Empty;
            }

            StateHasChanged();
        }

        private void InitializeCategoryDictionaries()
        {
            _adaptiveMSTCategoryPathValues.Clear();

            _categoryFixedDPaths.Clear();

            foreach (var category in _adaptiveCategories)
            {
                _adaptiveMSTCategoryPathValues[category.Id] = new decimal[_stagesCount];

                _categoryFixedDPaths[category.Id] = 0.0m;
            }
        }

        private void OnAdaptiveSubtypeChanged(AdaptivePaperSubtype subtype)
        {
            _selectedAdaptiveSubtype = subtype;

            if (IsAdaptiveStep)
            {
                _isStepPlus = false;

                _stagesCount = 9;

                if (_selectedDPathCalculationMode == DPathCalculationMode.FullyManual)
                {
                    _selectedDPathCalculationMode = DPathCalculationMode.ManualFinalScore;
                }
            }
            else
            {
                _stagesCount = 3;

                _isStepPlus = false;
            }

            InitializeCategoryDictionaries();

            StateHasChanged();
        }

        private async Task OnTransitionProfileChanged(TransitionProfileDto profile)
        {
            _transitionProfileHasError = false;
            _transitionProfileErrorMessage = string.Empty;
            _selectedTransitionProfile = null;
            _adaptiveCategories.Clear();
            _adaptiveMSTCategoryPathValues.Clear();
            _categoryFixedDPaths.Clear();

            if (profile == null || profile.Id <= 0)
            {
                StateHasChanged();
                return;
            }

            var transitionLevels = await BlazTransitionLevel.GetTransitionLevelsByProfileId(profile.Id);

            if (transitionLevels == null || transitionLevels.Count == 0)
            {
                _transitionProfileHasError = true;
                _transitionProfileErrorMessage = Resource.NoLevelsAvailable;
                StateHasChanged();
                return;
            }

            var categoryDifficultyLevels = transitionLevels
                    .GroupBy(l => l.QuestionCategoryId)
                    .Select(g => new
                    {
                        CategoryId = g.Key,
                        CategoryName = g.First().QuestionCategoryName,
                        DifficultyLevelIds = g.Select(x => x.DifficultyLevelId).Order().ToList()
                    })
                    .ToList();

            var distinctCounts = categoryDifficultyLevels.Select(c => c.DifficultyLevelIds.Count).Distinct().ToList();

            if (distinctCounts.Count > 1 && _selectedAdaptiveSubtype == AdaptivePaperSubtype.MST)
            {
                _transitionProfileHasError = true;
                _transitionProfileErrorMessage = Resource.TransitionProfileCategoriesHaveUnequalLevels;
                StateHasChanged();
                return;
            }

            _selectedTransitionProfile = profile;

            var firstCategoryLevelIds = categoryDifficultyLevels[0].DifficultyLevelIds;
            var allHaveSameLevelIds = categoryDifficultyLevels.All(c => c.DifficultyLevelIds.SequenceEqual(firstCategoryLevelIds));

            if (!allHaveSameLevelIds && _selectedAdaptiveSubtype == AdaptivePaperSubtype.MST)
            {
                _transitionProfileHasError = true;
                _transitionProfileErrorMessage = Resource.TransitionProfileCategoriesMustHaveSameDifficultyLevels;
                StateHasChanged();
                return;
            }

            _selectedTransitionProfile = profile;

            var categoryIdsFromLevels = categoryDifficultyLevels.Select(c => c.CategoryId).ToHashSet();

            _adaptiveCategories = [.. _allCategories.Where(c => categoryIdsFromLevels.Contains(c.Id))];

            _adaptiveMSTOrderedCategories = [.. _adaptiveCategories];

            InitializeCategoryDictionaries();

            if (_selectedDPathCalculationMode == DPathCalculationMode.ManualFinalScore)
            {
                foreach (var category in _adaptiveCategories)
                {
                    _categoryFixedDPaths[category.Id] = 0.0m;
                }
            }

            StateHasChanged();
        }

        private void OnIsStepPlusChanged(bool isStepPlus)
        {
            _isStepPlus = isStepPlus;

            int newStagesCount = _isStepPlus ? _stagesCount + 1 : Math.Max(1, _stagesCount - 1);

            OnStagesCountChanged(newStagesCount);

            StateHasChanged();
        }

        private void OnStagesCountChanged(int count)
        {
            _stagesCount = count;

            foreach (var categoryId in _adaptiveMSTCategoryPathValues.Keys.ToList())
            {
                var currentArray = _adaptiveMSTCategoryPathValues[categoryId];

                if (currentArray.Length != _stagesCount)
                {
                    Array.Resize(ref currentArray, _stagesCount);

                    _adaptiveMSTCategoryPathValues[categoryId] = currentArray;
                }
            }
        }

        private void MoveCategory(int index, int direction)
        {
            if (index < 0 || index >= _adaptiveMSTOrderedCategories.Count) return;

            var newIndex = index + direction;

            if (newIndex < 0 || newIndex >= _adaptiveMSTOrderedCategories.Count) return;

            var item = _adaptiveMSTOrderedCategories[index];

            _adaptiveMSTOrderedCategories.RemoveAt(index);

            _adaptiveMSTOrderedCategories.Insert(newIndex, item);
        }

        private void OnQuestionSelectionChanged(QuestionSelectionType questionSelectionType)
        {
            _selectedQuestionSelection = questionSelectionType;

            _outputFormsCount = 1;
        }


        // FROM SUBMIT METHODS

        public async Task<bool> OnFirstStepPaperMetadataSubmitAsync()
        {
            SetSubmitButtonState(true);

            if (ValidatePaperMetadataFormArguments())
            {
                _currentPaperMetadataDto = PrepareCurrentPaperMetadataDto();

                if (IsCurrentFormsModeExcelSheetSingleManualForm && !_isExternalExcelSheetManualFormAddedSuccessfully)
                {
                    return await AddManualFormUsingExcelSheetAsync(_currentPaperMetadataDto);
                }

                if (_thisComponentCurrentOperationalMode == StepperOperationalMode.InsertionMode)
                {
                    return await AddPaperMetadataAsync(_currentPaperMetadataDto);
                }
                else if (_thisComponentCurrentOperationalMode == StepperOperationalMode.UpdateMode)
                {
                    return await UpdatePaperMetadataAsync(_currentPaperMetadataDto);
                }

                SetSubmitButtonState(false);
            }
            else
            {
                if (_usesExcelQuestionsImport && _uploadedQuestions?.ValidateExcelSheetQuestionsDto == null)
                {
                    Snackbar.Add(Resource.PleaseUploadTheExcelFileBeforeProceeding, Severity.Error);
                }
                else
                {
                    Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);
                }
            }

            StateHasChanged();

            return false;
        }

        private async Task<bool> AddPaperMetadataAsync(AddOrUpdatePaperMetadataRequestDto paperMetadataCreationRequestDto)
        {
            var response = await PaperService.AddPaperMetaDataAsync(paperMetadataCreationRequestDto);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _addPaperMetadataResponseDto = JsonSerializer.Deserialize<AddOrUpdatePaperMetadataResponseDto>(response.Data.ToString(), _jsonSerializerOptions);

                _thisComponentCurrentOperationalMode = StepperOperationalMode.UpdateMode;

                SetSubmitButtonState(false);

                Snackbar.Add(response.Message, Severity.Success);

                StateHasChanged();

                return true;
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            SetSubmitButtonState(false);

            return false;
        }

        private async Task<bool> AddManualFormUsingExcelSheetAsync(AddOrUpdatePaperMetadataRequestDto paperMetadataCreationRequestDto)
        {
            var response = await PaperService.AddManualFormUsingExcelSheetAsync(_addPaperMetadataResponseDto.PaperId, paperMetadataCreationRequestDto);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _addPaperMetadataResponseDto = JsonSerializer.Deserialize<AddOrUpdatePaperMetadataResponseDto>(response.Data.ToString(), _jsonSerializerOptions);

                _isExternalExcelSheetManualFormAddedSuccessfully = true;

                SetSubmitButtonState(false);

                Snackbar.Add(response.Message, Severity.Success);

                StateHasChanged();

                return true;
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            SetSubmitButtonState(false);

            return false;
        }

        private async Task<bool> UpdatePaperMetadataAsync(AddOrUpdatePaperMetadataRequestDto paperMetadataUpdateRequestDto)
        {
            var response = await PaperService.UpdatePaperMetadataAsync(_addPaperMetadataResponseDto.PaperId, paperMetadataUpdateRequestDto);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _addPaperMetadataResponseDto = JsonSerializer.Deserialize<AddOrUpdatePaperMetadataResponseDto>(response.Data.ToString(), _jsonSerializerOptions);

                SetSubmitButtonState(false);

                Snackbar.Add(response.Message, Severity.Success);

                StateHasChanged();

                return true;
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            SetSubmitButtonState(false);

            return false;
        }

        private AddOrUpdatePaperMetadataRequestDto PrepareCurrentPaperMetadataDto()
        {
            return new AddOrUpdatePaperMetadataRequestDto
            {
                Name = _name,
                Code = _code,
                Description = _description,
                Abbreviation = _abbreviation,
                SubjectsIds = _selectedSubjects.ConvertAll(s => s.Id),
                QuestionsCount = _questionsCount,
                Duration = _duration,
                TotalMarks = (long)_totalMarks,
                Type = _selectedType,
                AdaptiveSubtype = _isAdaptive ? _selectedAdaptiveSubtype : 0,
                IsStepPlus = _isAdaptive && IsAdaptiveStep && _isStepPlus,
                TransitionProfileId = _selectedTransitionProfile?.Id == 0 ? null : _selectedTransitionProfile?.Id,
                QuestionSelectionType = _selectedQuestionSelection,
                QuestionContentType = _selectedQuestionsContentType,
                OutputFormsCount = _outputFormsCount,
                AllowInstantResult = _allowInstantResult,
                LanguageId = _selectedLanguage?.Id ?? 0,
                DifficultyProfileId = (_selectedProfile?.Id > 0) ? _selectedProfile.Id : null,
                QuestionDistributionTypeInForm = _selectedQuestionDistributionTypeInForm,
                UsesExcelQuestionsImport = _usesExcelQuestionsImport,
                UploadedQuestions = _uploadedQuestions,
                StageCount = _isAdaptive ? _stagesCount : 0,
                CategoryStagePaths = _adaptiveMSTCategoryPathValues.ToDictionary(k => k.Key, v => v.Value.ToList()),
                DPathCalculationMode = _isAdaptive ? _selectedDPathCalculationMode : DPathCalculationMode.None,
                CategoryFixedDPaths = _selectedDPathCalculationMode == DPathCalculationMode.ManualFinalScore
                    ? _categoryFixedDPaths.ToDictionary(k => k.Key, v => v.Value)
                    : [],
                AdaptiveCategoryExecutionOrder = IsAdaptiveMST ? _adaptiveMSTOrderedCategories.ConvertAll(c => c.Id) : [],
                OESGroupDtos = SelectedGroups
            };
        }

        private async Task PrepareFirstStepForUpdateModeAsync(long paperMetadataId)
        {
            var response = await PaperService.GetPaperMetaDataAsync(paperMetadataId);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var paperMetadataDto = (GetPaperMetadataResponseDto)response.Data;

                _name = paperMetadataDto.Name;
                _code = paperMetadataDto.Code;
                _description = paperMetadataDto.Description;
                _abbreviation = paperMetadataDto.Abbreviation;
                _selectedSubjects = Subjects.IntersectBy(paperMetadataDto.SubjectsIds, s => s.Id).ToList();
                _questionsCount = paperMetadataDto.QuestionsCount;
                _duration = paperMetadataDto.Duration;
                _totalMarks = paperMetadataDto.TotalMarks;
                _selectedType = paperMetadataDto.Type;
                _isStepPlus = paperMetadataDto.IsStepPlus;
                SelectedGroups = [.. paperMetadataDto.OESGroupDtos.Where(g => !g.AutoCreatedForUser)];

                if (_selectedType == PaperType.Adaptive)
                {
                    await ConfigureAdaptivePaperSettingsAsync(
                        paperMetadataDto.Type,
                        paperMetadataDto.AdaptiveSubtype,
                        paperMetadataDto.DPathCalculationMode,
                        paperMetadataDto.TransitionProfileId,
                        paperMetadataDto.StageCount,
                        paperMetadataDto.CategoryStagePaths,
                        paperMetadataDto.CategoryFixedDPaths,
                        paperMetadataDto.AdaptiveCategoryExecutionOrder
                    );
                }

                _selectedQuestionSelection = paperMetadataDto.QuestionSelectionType;
                _selectedQuestionsContentType = paperMetadataDto.QuestionContentType;
                if (IsCurrentFormsModeSingleForm && !IsCurrentFormsModeContinuePendingForm)
                {
                    _outputFormsCount = paperMetadataDto.OutputFormsCount + 1;
                }
                else
                {
                    _outputFormsCount = paperMetadataDto.OutputFormsCount;
                }
                _allowInstantResult = paperMetadataDto.AllowInstantResult;
                _usesExcelQuestionsImport = IsCurrentFormsModeExcelSheetSingleManualForm;
                _selectedLanguage = Languages.Find(l => l.Id == paperMetadataDto.LanguageId) ?? new();
                _selectedProfile = _selectedType == PaperType.Standard ?
                    Profiles.Find(p => p.Id == paperMetadataDto.DifficultyProfileId) ?? new() : new();
                _selectedQuestionDistributionTypeInForm = paperMetadataDto.QuestionDistributionTypeInForm;
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        public void DisableCriticalPaperMetadataFields()
        {
            _criticalFieldsDisabled = true;

            StateHasChanged();
        }


        // MISC AND VALIDATION METHODS

        private void SetSubmitButtonState(bool state)
        {
            _isSubmitButtonHit = state;

            StateHasChanged();
        }

        private async Task FillStepOneFromTemplateAysnc(long paperMetadataTemplateId)
        {
            var response = await PaperService.GetPaperMetadataTemplateByIdAsync(paperMetadataTemplateId);

            if (response != null)
            {
                var template = response.Data as AddOrUpdatePaperMetadataRequestDto;

                _name = template.Name;
                _code = template.Code;
                _description = template.Description;
                _abbreviation = template.Abbreviation;
                _selectedSubjects = [.. Subjects.IntersectBy(template.SubjectsIds, s => s.Id)];
                _questionsCount = template.QuestionsCount;
                _duration = template.Duration;
                _totalMarks = template.TotalMarks;
                _selectedType = template.Type;
                _isStepPlus = template.IsStepPlus;

                await OnTypeChangedAsync(template.Type);

                if (_selectedType == PaperType.Adaptive)
                {
                    await ConfigureAdaptivePaperSettingsAsync(
                        template.Type,
                        template.AdaptiveSubtype,
                        template.DPathCalculationMode,
                        template.TransitionProfileId,
                        template.StageCount,
                        template.CategoryStagePaths,
                        template.CategoryFixedDPaths,
                        template.AdaptiveCategoryExecutionOrder
                    );
                }

                _selectedQuestionSelection = template.QuestionSelectionType;
                _selectedQuestionsContentType = template.QuestionContentType;
                _allowInstantResult = template.AllowInstantResult;
                _usesExcelQuestionsImport = template.UsesExcelQuestionsImport;
                _outputFormsCount = template.OutputFormsCount;
                _selectedQuestionDistributionTypeInForm = template.QuestionDistributionTypeInForm;
                _selectedProfile = Profiles.Find(p => p.Id == template.DifficultyProfileId) ?? new();
                _selectedLanguage = Languages.Find(l => l.Id == template.LanguageId) ?? new();

                SetSubmitButtonState(false);

                await InvokeAsync(StateHasChanged);
            }
            else
            {
                Snackbar.Add(Resource.TemplateIsEmpty, Severity.Error);
            }
        }

        private async Task ShowTemplateAsync()
        {
            var dialogOptions = new DialogOptions()
            {
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
            };

            var dialogParams = new DialogParameters();

            var dialog = await DialogService.ShowAsync<PaperMetadataTemplate>(string.Empty, dialogParams, dialogOptions);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var PaperTemplateId = (long)result.Data;

                await FillStepOneFromTemplateAysnc(PaperTemplateId);
            }
        }

        private async Task OpenTemplateNameDialog()
        {
            var parameters = new DialogParameters();

            var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.Large };

            var dialog = await DialogService.ShowAsync<TemplateNameDialog>(Resource.UsePaperTemplate, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is string templateName)
            {
                await SaveTemplateAsync(templateName);
            }
        }

        private async Task SaveTemplateAsync(string templateName)
        {
            _currentPaperMetadataDto = PrepareCurrentPaperMetadataDto();

            if (!string.IsNullOrWhiteSpace(templateName) && _currentPaperMetadataDto != null)
            {
                _metadataTemplateDto.Name = templateName;
                _metadataTemplateDto.AddOrUpdatePaperMetadataRequestDto = _currentPaperMetadataDto;

                var response = await PaperService.SaveTemplatePaperMetadataAsync(_metadataTemplateDto);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        Snackbar.Add(response.Message, Severity.Success);
                        break;

                    case HttpStatusCode.Conflict:
                        Snackbar.Add(response.Message, Severity.Warning);
                        break;

                    default:
                        Snackbar.Add(response.Message, Severity.Error);
                        break;
                }

                StateHasChanged();
            }
        }

        private bool ValidatePaperMetadataFormArguments()
        {
            var _isNameValid = !string.IsNullOrWhiteSpace(_name);
            var _isCodeValid = !string.IsNullOrWhiteSpace(_code);
            var _isDescriptionValid = !string.IsNullOrWhiteSpace(_description);
            var _isAbbreviationValid = !string.IsNullOrWhiteSpace(_abbreviation);
            var _isSubjectValid = _selectedSubjects?.Count > 0;
            var _isQuestionsCountValid = _questionsCount > 0;
            var _isDurationValid = _duration > 0;
            var _isTotalMarksValid = _isAdaptive || _totalMarks > 0;
            var _isTypeValid = Enum.IsDefined(typeof(PaperType), _selectedType);
            var _isTransitionProfileValid = !_isAdaptive || (_isAdaptive && _selectedTransitionProfile?.Id > 0);
            var _isStagePathsValid = !_isAdaptive ||
                (_isAdaptive && _selectedDPathCalculationMode != DPathCalculationMode.FullyManual) ||
                (_isAdaptive && _selectedDPathCalculationMode == DPathCalculationMode.FullyManual && _adaptiveMSTCategoryPathValues.Values.All(paths => paths.All(p => p > 0)));
            var _isFixedDPathsValid = !_isAdaptive ||
                _selectedDPathCalculationMode != DPathCalculationMode.ManualFinalScore ||
                (_categoryFixedDPaths.Count > 0 && _categoryFixedDPaths.Values.All(v => v > 0));
            var _isQuestionSelectionValid = _isPaperStandard || (!_isPaperStandard && Enum.IsDefined(typeof(QuestionSelectionType), _selectedQuestionSelection));
            var _isLanguageValid = _selectedLanguage?.Id > 0;
            var _isDifficultyProfileIdValid = _isAdaptive || (_selectedProfile?.Id > 0);
            var _isOutputFormsCountValid = _isAdaptive || (_isPaperStandard && _outputFormsCount >= 1);
            var _isExcelUploadValid = !_usesExcelQuestionsImport || (_uploadedQuestions?.ValidateExcelSheetQuestionsDto != null);

            return _isNameValid &&
                   _isCodeValid &&
                   _isDescriptionValid &&
                   _isAbbreviationValid &&
                   _isSubjectValid &&
                   _isQuestionsCountValid &&
                   _isDurationValid &&
                   _isTotalMarksValid &&
                   _isTypeValid &&
                   _isTransitionProfileValid &&
                   _isStagePathsValid &&
                    _isFixedDPathsValid &&
                   _isQuestionSelectionValid &&
                   _isOutputFormsCountValid &&
                   _isLanguageValid &&
                   _isDifficultyProfileIdValid &&
                   _isExcelUploadValid;
        }

        private async Task OpenImportQuestionsWithTheirItembanksDialog()
        {
            var dialogParams = new DialogParameters<ImportQuestionsWithItemBanksDialog>
            {
                { x => x.PaperAllowInstantResult,  _allowInstantResult },
                { x => x.CurrentQuestionsCount, _questionsCount }
            };

            var dialogOptions = new DialogOptions()
            {
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
                CloseButton = true
            };

            _files.Clear();

            _errorListDto.Clear();

            var dialog = await DialogService.ShowAsync<ImportQuestionsWithItemBanksDialog>(string.Empty, dialogParams, dialogOptions);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var receivedData = (QuestionIdsAndItemBankIdsDto)result.Data;

                _uploadedQuestions = receivedData;

                _selectedProfile = Profiles.Find(p => p.Id == receivedData.ValidateExcelSheetQuestionsDto.DeducedDifficultyProfileId) ?? new();

                var firstAvailableLanguageId = receivedData.ValidateExcelSheetQuestionsDto.AvailableLanguageIds.FirstOrDefault();
                _selectedLanguage = Languages.Find(l => l.Id == firstAvailableLanguageId) ?? new();

                _availableLanguagesIdsToChooseFromWhenExcelImport = [.. receivedData.ValidateExcelSheetQuestionsDto.AvailableLanguageIds];

                StateHasChanged();
            }

            StateHasChanged();
        }


        #region Helper Methods

        private async Task ConfigureAdaptivePaperSettingsAsync(
            PaperType type,
            AdaptivePaperSubtype adaptiveSubtype,
            DPathCalculationMode dPathCalculationMode,
            long? transitionProfileId,
            int stageCount,
            Dictionary<long, List<decimal>>? categoryStagePaths,
            Dictionary<long, decimal>? categoryFixedDPaths,
            List<long>? adaptiveCategoryExecutionOrder
        )
        {
            await OnTypeChangedAsync(type);

            _selectedAdaptiveSubtype = adaptiveSubtype == 0 ? AdaptivePaperSubtype.MST : adaptiveSubtype;

            _selectedDPathCalculationMode = dPathCalculationMode;

            var transitionProfile = transitionProfileDtos.Find(tp => tp.Id == transitionProfileId);

            if (transitionProfile != null)
            {
                await OnTransitionProfileChanged(transitionProfile);
            }

            _stagesCount = stageCount > 0 ? stageCount : 3;

            if (_selectedDPathCalculationMode == DPathCalculationMode.FullyManual && categoryStagePaths?.Count > 0)
            {
                OnStagesCountChanged(_stagesCount);

                foreach (var kvp in categoryStagePaths)
                {
                    if (_adaptiveMSTCategoryPathValues.TryGetValue(kvp.Key, out var pathValues))
                    {
                        for (int i = 0; i < kvp.Value.Count && i < _stagesCount; i++)
                        {
                            pathValues[i] = kvp.Value[i];
                        }
                    }
                }
            }
            else if (_selectedDPathCalculationMode == DPathCalculationMode.ManualFinalScore && categoryFixedDPaths?.Count > 0)
            {
                foreach (var kvp in categoryFixedDPaths)
                {
                    if (_categoryFixedDPaths.ContainsKey(kvp.Key))
                    {
                        _categoryFixedDPaths[kvp.Key] = kvp.Value;
                    }
                }
            }

            ConfigureAdaptiveCategoryExecutionOrder(adaptiveCategoryExecutionOrder);
        }

        private void ConfigureAdaptiveCategoryExecutionOrder(List<long>? adaptiveCategoryExecutionOrder)
        {
            if (adaptiveCategoryExecutionOrder?.Count > 0)
            {
                _adaptiveMSTOrderedCategories = adaptiveCategoryExecutionOrder
                    .Select(id => _adaptiveCategories.FirstOrDefault(c => c.Id == id))
                    .Where(c => c != null)
                    .ToList()!;

                var missingCategories = _adaptiveCategories
                    .Where(c => !_adaptiveMSTOrderedCategories.Any(oc => oc.Id == c.Id))
                    .ToList();

                _adaptiveMSTOrderedCategories.AddRange(missingCategories);
            }
            else
            {
                _adaptiveMSTOrderedCategories = [.. _adaptiveCategories];
            }
        }

        private async Task OpenGroupDialog()
        {
            if (_thisComponentCurrentOperationalMode == StepperOperationalMode.UpdateMode)
            {
                var allowed = await AuthService.IsCurrentUserOwnerAsync(
                    _addPaperMetadataResponseDto.PaperId,
                    PaperService.GetPaperGroupsAsync,
                    dto => [dto.OwnerGroupId ?? Guid.Empty]
                );

                if (!allowed)
                {
                    Snackbar.Add(
                        string.Format(Resource.OnlyCreatorCanManageGroups, Resource.Paper),
                        Severity.Error
                    );
                    return;
                }
            }

            SelectedGroups = await DialogInteractionService.OpenSelectionDialogAsync(
                dialogService: DialogService,
                title: Resource.SelectGroup,
                preSelectedItems: SelectedGroups.Where(g => !g.AutoCreatedForUser),
                endpointService: async pagination =>
                {
                    var allGroups = await PaperService.GetAllPaperGroupsCreatedByUser();

                    var filteredGroups = allGroups
                        .Concat(SelectedGroups)
                        .GroupBy(g => g.Id)
                        .Select(g => g.First())
                        .ToList();

                    if (!string.IsNullOrWhiteSpace(pagination.SearchKey))
                    {
                        filteredGroups = [.. filteredGroups.Where(g => g.Name.Contains(pagination.SearchKey, StringComparison.OrdinalIgnoreCase))];
                    }

                    return new CustomTableData<GetOESGroupDto>
                    {
                        Items = filteredGroups,
                        TotalItems = filteredGroups.Count
                    };
                },
                resourceType: ResourceType.Papers,
                onDelete: EventCallback.Factory.Create<Guid>(this, DeleteGroupAsync)
            );

            SelectedGroups = [.. SelectedGroups.Where(g => !g.AutoCreatedForUser)];

            StateHasChanged();
        }

        private async Task DeleteGroupAsync(Guid groupId)
        {
            var response = await BlazGroupService.DeleteGroupAsync(groupId);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
                Snackbar.Add(response.Message, Severity.Success);
            else
                Snackbar.Add(response.Message, Severity.Error);
        }

        public PaperType GetCurrentPaperType() => _selectedType;
    }

    #endregion
}
