using Microsoft.AspNetCore.Components;
using MudBlazor;
using Newtonsoft.Json;
using OES.Blazor.Dialogs.Question.IloSelectionDialog;
using OES.Blazor.Dialogs.Question.ItemBankSelectionDialog;
using OES.Blazor.Dialogs.Question.QuestionTemplate;
using OES.Blazor.Dialogs.Question.TemplateDialog;
using OES.Blazor.Extentions.DialogHelpers;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.DifficultyProfile;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.ILOSection;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.QuestionCataegory;
using OES.Blazor.Services.Interfaces.QuestionMetaData;
using OES.Blazor.Services.Interfaces.QuestionType;
using OES.Blazor.Services.Interfaces.Subject;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.Dtos.FileUploadResponseSettings;
using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.Dtos.Subject;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Question
{
    public partial class QuestionMetaData
    {
        // INJECTIONS AND EXTERNAL PARAMS

        [Inject] private IBlazQuestionService _blazQuestionService { get; set; } = default!;
        [Inject] private IBlazQuestionMetaData BlazQuestionMetaDataService { get; set; } = default!;
        [Inject] private IBlazSubjectService _blazSubjectService { get; set; } = default!;
        [Inject] private IBlazDifficultyProfileService _BlazProfileService { get; set; } = default!;
        [Inject] private IBlazQuestionCategoryService _BlazCategoryService { get; set; } = default!;
        [Inject] private IBLazQuestionType _BLazQuestionType { get; set; } = default!;
        [Inject] private IBlazItemBankService BlazItemBankService { get; set; } = default!;
        [Inject] private IBlazILOService BlazIloService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; } = default!;
        [Inject] private IBlazQuestionService BlazQuestionService { get; set; }
        [Inject] private GlobalUserContext GlobalUserContext { get; set; }
        [Inject] private IBlazGroupService BlazGroupService { get; set; }
        [Inject] private IBlazAuthService AuthService { get; set; }

        [Parameter] public EventCallback<string> QuestionTypeChanged { get; set; }
        [Parameter] public EventCallback<long> OnQuestionCreation { get; set; }
        [Parameter] public EventCallback<long> QuestionTypeIdChanged { get; set; }
        [Parameter] public EventCallback OnDataLoaded { get; set; }


        // COMPONENT FIELDS AND PROPS:

        private List<SubjectDto> _subjects = [];
        private QuestionTypeDto _selectedQuestionType = new();
        private RootItemBankDto? _selectedItemBankRoot;
        private RootIloDto? _selectedIloRoot;
        private SelectedItemBankNodeFromDialogDto _selectedItemBankNodeFromDialogDto = new();
        private SelectedIloNodeFromDialogDto _selectedIloNodeFromDialogDto = new();
        private DifficultyLevelDto _difficultyLevel = new();
        private SubjectDto _selectedSubjectDto = new();
        private QuestionCategoryDto? _selectedCategory = new();
        private ProfileDto _profile = new();
        private List<QuestionTypeDto> _questionsTypes = [];
        private bool _isSubmitButtonHit = false;
        private bool _isQuestionTypeEligibleForUpdate = true;
        private bool _progressBarShown = false;
        private bool _deltaError = false;
        private string _deltaErrorText = string.Empty;
        private string _requiredErrorText = Resource.ThisFieldIsRequired;
        private const string INSERTION_MODE = nameof(INSERTION_MODE);
        private const string UPDATE_MODE = nameof(UPDATE_MODE);
        private string _currentOperationalMode = INSERTION_MODE;

        private bool IsSaveTemplateDisabled =>
          _selectedSubjectDto.Id == 0 ||
          _selectedQuestionType.Id == 0 ||
          _profile.Id == 0 ||
          _difficultyLevel.Id == 0 ||
          _selectedItemBankNodeFromDialogDto.Id == 0;
        public long? DisplayQuestionsExhaustionCount
        {
            get => _displayExhaustionCount;
            set
            {
                _displayExhaustionCount = value;
                NormalizeQuestionsExhaustionCount();
            }
        }
        public QuestionMetadataAdditionOrUpdateDto QuestionMetaDataObject { get; set; } = new();
        public QuestionCreationTemplateDto QuestionTemplateObject { get; set; } = new();
        private List<DifficultyLevelDto> DifficultyLevels { get; set; } = [];
        public List<RootItemBankDto> RootItemBankDtos { get; set; } = [];
        public List<RootIloDto> RootIloDtos { get; set; } = [];
        private List<ProfileDto> Profiles { get; set; } = [];
        private List<QuestionCategoryDto> Categories { get; set; } = [];
        public long CreatedQuestionMetaDataId { get; set; }
        public string TemplateName { get; set; }
        private List<GetOESGroupDto> SelectedGroups { get; set; } = [];

        private ISet<FileUploadExtension> _selectedExtensions { get; set; } = new HashSet<FileUploadExtension>
        {
            FileUploadExtension.PNG,
            FileUploadExtension.DOCX
        };

        private readonly IEnumerable<FileUploadExtension> _availableExtensions = Enum.GetValues<FileUploadExtension>();

        private long? _displayExhaustionCount = null;


        // DATA PROCESSING METHODS

        protected override async Task OnInitializedAsync()
        {
            bool isCreatePage = await BlazSessionStorageService.GetValue<bool>(MiscConstants.IsCreatePage);
            CreatedQuestionMetaDataId = await BlazSessionStorageService.GetValue<long>(MiscConstants.PerformEditBtnClick);

            if (!isCreatePage && CreatedQuestionMetaDataId > 0)
            {
                _currentOperationalMode = UPDATE_MODE;
                _progressBarShown = true;
            }

            StateHasChanged();

            var subjectsTask = _blazSubjectService.GetAllSubjectsAsync();
            var questionsTypesTask = _BLazQuestionType.GetAllQuestionType();
            var categoriesTask = _BlazCategoryService.GetCategories();
            var profilesTask = _BlazProfileService.GetProfiles();
            var rootItemBankDtosTask = BlazItemBankService.GetRootItemBanksNodesAsync();
            var rootIloDtosTask = BlazIloService.GetRootIlosNodesAsync();

            await Task.WhenAll(subjectsTask, questionsTypesTask, categoriesTask, profilesTask, rootItemBankDtosTask, rootIloDtosTask);

            _subjects = await subjectsTask;
            _questionsTypes = await questionsTypesTask;
            Categories = await categoriesTask;
            Profiles = await profilesTask;
            RootItemBankDtos = (await rootItemBankDtosTask)?.OrderByDescending(x => x.Id).ToList() ?? [];
            RootIloDtos = await rootIloDtosTask;

            if (_currentOperationalMode == UPDATE_MODE)
            {
                await PrepareStepOneForUpdateModeAsync();
                _progressBarShown = false;
            }

            await OnDataLoaded.InvokeAsync();

            StateHasChanged();
        }

        private void OnSelectedExtensionsChanged(IEnumerable<FileUploadExtension> newSelection)
        {
            _selectedExtensions = new HashSet<FileUploadExtension>(newSelection);
        }

        private void OnTypeChange(QuestionTypeDto selectedType)
        {
            if (selectedType != null)
            {
                _selectedQuestionType = selectedType;

                QuestionTypeChanged.InvokeAsync(_selectedQuestionType.Name);

                QuestionTypeIdChanged.InvokeAsync(_selectedQuestionType.Id);

                QuestionMetaDataObject.QuestionTypeId = _selectedQuestionType.Id;
            }

            StateHasChanged();
        }

        private void CaptureSelectedItemBankRoot(RootItemBankDto rootItemBankDto)
        {
            _selectedItemBankRoot = rootItemBankDto;

            _selectedItemBankNodeFromDialogDto = new();
        }

        private void CaptureSelectedIloRoot(RootIloDto rootIloDto)
        {
            _selectedIloRoot = rootIloDto;

            _selectedIloNodeFromDialogDto = new();
        }

        private void ClearIloSelection()
        {
            _selectedIloRoot = null;
            _selectedIloNodeFromDialogDto = new();
        }

        private void ClearItembankSelection()
        {
            _selectedItemBankRoot = null;
            _selectedItemBankNodeFromDialogDto = new();
        }

        private async Task OpenItemBankRootTreeDialogAsync()
        {
            var parameters = new DialogParameters<ItemBankSelectionDialog>
            {
                { p => p.ItemBankRootId,  _selectedItemBankRoot.Id },
                { p => p.SelectedItemBankNodeId, _selectedItemBankNodeFromDialogDto.Id }
            };

            var options = new DialogOptions()
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var result = await (await DialogService.ShowAsync<ItemBankSelectionDialog>(string.Empty, parameters, options)).Result;

            if (!result.Canceled)
            {
                _selectedItemBankNodeFromDialogDto = (SelectedItemBankNodeFromDialogDto)result.Data;
                StateHasChanged();
            }
        }

        private async Task OpenIloRootTreeDialogAsync()
        {
            var parameters = new DialogParameters<IloSelectionDialog>
            {
                { p => p.IloRootId,  _selectedIloRoot.Id },
                { p => p.SelectedIloNodeId, _selectedIloNodeFromDialogDto.Id }
            };

            var options = new DialogOptions()
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var result = await (await DialogService.ShowAsync<IloSelectionDialog>(string.Empty, parameters, options)).Result;

            if (!result.Canceled)
            {
                _selectedIloNodeFromDialogDto = (SelectedIloNodeFromDialogDto)result.Data;

                StateHasChanged();
            }
        }

        private void ValidateDifficultyValue()
        {
            if (decimal.TryParse(QuestionMetaDataObject.Delta.ToString(), out var deltaValue))
            {
                if (_difficultyLevel != null && (deltaValue < _difficultyLevel.FromDelta || deltaValue > _difficultyLevel.ToDelta))
                {
                    _deltaError = true;
                    _deltaErrorText = $"{Resource.DeltaValueMustBeBetween} {_difficultyLevel.FromDeltaFormatted} {Resource.And} {_difficultyLevel.ToDeltaFormatted}";
                }
                else
                {
                    _deltaError = false;
                    _deltaErrorText = string.Empty;
                }
            }
        }

        private async Task ChangeProfileAsync(ProfileDto profileDto)
        {
            _profile = profileDto;

            await GetDifficultyLevelBasedOnProfileId(_profile.Id);
        }

        private async Task GetDifficultyLevelBasedOnProfileId(long id)
        {
            _difficultyLevel = new();
            DifficultyLevels = await _BLazQuestionType.GetAllDifficultyLevels(id);
        }

        private async Task PrepareStepOneForUpdateModeAsync()
        {
            var response = await _blazQuestionService.GetQuestionMetadataByIdAsync(CreatedQuestionMetaDataId);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var questionMetadataDto = (QuestionMetadataRetrievalDto)response.Data;

                await FillQuestionMetaDataAsync(questionMetadataDto);
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private async Task FillStepOneFromTemplateAysnc(long questionTemplateId)
        {
            var response = await BlazQuestionService.GetQuestionTemplateById(questionTemplateId);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var questionMetadataDto = (GetQuestionTemplateResponseDto)response.Data;

                await FillQuestionMetaDataAsync(questionMetadataDto.QuestionMetadata);
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private async Task FillQuestionMetaDataAsync(QuestionMetadataRetrievalDto questionMetadataDto)
        {
            await ShouldQuestionTypeBeUpdatedAsync();

            QuestionMetaDataObject.Id = questionMetadataDto.Id;
            QuestionMetaDataObject.Code = questionMetadataDto.Code;

            _selectedSubjectDto = _subjects.Find(s => s.Id == questionMetadataDto.SubjectId) ?? new();

            var targetQuestionType = _questionsTypes.Find(t => t.Id == questionMetadataDto.QuestionTypeId);
            OnTypeChange(targetQuestionType);

            _selectedCategory = Categories.Find(c => c.Id == questionMetadataDto.QuestionCategoryId);

            var targetProfile = Profiles.Find(p => p.Id == questionMetadataDto.DifficultyProfileId) ?? new();
            await ChangeProfileAsync(targetProfile);

            _difficultyLevel = DifficultyLevels.Find(d => d.Id == questionMetadataDto.DifficultyLevelId) ?? new();

            QuestionMetaDataObject.MaximumAnswerTime = questionMetadataDto.MaximumAnswerTime;

            QuestionMetaDataObject.Delta = questionMetadataDto.Delta;

            QuestionMetaDataObject.IsRoot = questionMetadataDto.IsRoot;

            QuestionMetaDataObject.Author = questionMetadataDto.Author;


            var searchingRootItemBankId = questionMetadataDto.RootItemBankId is null ? questionMetadataDto.ChildItemBankDto.Id : questionMetadataDto.RootItemBankId;
            _selectedItemBankRoot = RootItemBankDtos.Find(i => i.Id == searchingRootItemBankId);
            _selectedItemBankNodeFromDialogDto = questionMetadataDto.ChildItemBankDto;


            var searchingRootIloId = questionMetadataDto.RootIloId is null ? questionMetadataDto.ChildIloDto?.Id : questionMetadataDto.RootIloId;
            _selectedIloRoot = RootIloDtos.Find(ilo => ilo.Id == searchingRootIloId);
            _selectedIloNodeFromDialogDto = questionMetadataDto.ChildIloDto ?? new();
            SelectedGroups = questionMetadataDto.OESGroupDtos?
                .Where(g => !g.AutoCreatedForUser)
                .ToList() ?? [];

            QuestionMetaDataObject.QuestionLayoutId = questionMetadataDto.QuestionLayoutId;

            _displayExhaustionCount = questionMetadataDto.QuestionsExhaustionCount == MiscConstants.CommonQuestionExhaustionCount
                ? null
                : questionMetadataDto.QuestionsExhaustionCount;
            NormalizeQuestionsExhaustionCount();

            QuestionMetaDataObject.ScientificEditorPanelEnabled = questionMetadataDto.ScientificEditorPanelEnabled;
            QuestionMetaDataObject.FileManagerEditorPanelEnabled = questionMetadataDto.FileManagerEditorPanelEnabled;

            if (questionMetadataDto.FileUploadSettings != null && questionMetadataDto.QuestionTypeId == (long)Helper.Enums.QuestionType.FileUploadResponse)
            {
                if (!string.IsNullOrEmpty(questionMetadataDto.FileUploadSettings.SupportedFileExtensions))
                {
                    HashSet<FileUploadExtension> parsed = System.Text.Json.JsonSerializer.Deserialize<HashSet<FileUploadExtension>>(questionMetadataDto.FileUploadSettings.SupportedFileExtensions);
                    _selectedExtensions = parsed ?? [];
                }

                QuestionMetaDataObject.FileUploadResponseSettings = new FileUploadSettingsDto
                {
                    Id = questionMetadataDto.FileUploadSettings.Id,
                    QuestionMetadataId = questionMetadataDto.Id,
                    ShowAnswerTextArea = questionMetadataDto.FileUploadSettings.ShowAnswerTextArea,
                    UploadedFilesCount = questionMetadataDto.FileUploadSettings.UploadedFilesCount,
                    SingleFileMaxSizeInMB = questionMetadataDto.FileUploadSettings.SingleFileMaxSizeInMB,
                    SupportedFileExtensions = System.Text.Json.JsonSerializer.Serialize(_selectedExtensions),
                };
            }
        }

        public void ResetForAnotherNewQuestion()
        {
            CreatedQuestionMetaDataId = 0;

            _currentOperationalMode = INSERTION_MODE;
        }

        // SEARCHING AND FILTRATION METHODS:

        private async Task<IEnumerable<QuestionTypeDto>> SearchTypeAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                 (_selectedQuestionType?.Name == null && string.IsNullOrWhiteSpace(value)) ||
                 _selectedQuestionType?.Name?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
            {
                return _questionsTypes;
            }
            return await FilterListAsync(_questionsTypes, p => p.Name, value);
        }

        private async Task<IEnumerable<ProfileDto>> SearchProfileAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                 (_profile?.Name == null && string.IsNullOrWhiteSpace(value)) ||
                 _profile?.Name?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
            {
                return Profiles;
            }
            return await FilterListAsync(Profiles, p => p.Name, value);
        }

        private async Task<IEnumerable<DifficultyLevelDto>> SearchDifficultyLevelAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                 (_difficultyLevel?.Name == null && string.IsNullOrWhiteSpace(value)) ||
                 _difficultyLevel?.Name?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
            {
                return DifficultyLevels;
            }
            return await FilterListAsync(DifficultyLevels, dl => dl.Name, value);
        }

        private async Task<IEnumerable<SubjectDto>> SearchSubjectAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                 (_selectedSubjectDto?.Name == null && string.IsNullOrWhiteSpace(value)) ||
                 _selectedSubjectDto?.Name?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
            {
                return _subjects;
            }

            return await FilterListAsync(_subjects, s => s.Name, value);
        }

        private async Task<IEnumerable<QuestionCategoryDto>> SearchCategoryAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value) || (_selectedCategory == null && string.IsNullOrWhiteSpace(value)) ||
                _selectedCategory?.Name?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
            {
                return Categories;
            }

            return await FilterListAsync(Categories, c => c.Name, value);
        }

        private async Task<IEnumerable<RootItemBankDto>> SearchItemBankRootsAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                 (_selectedItemBankRoot?.Name == null && string.IsNullOrWhiteSpace(value)) ||
                 _selectedItemBankRoot?.Name?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
            {
                return RootItemBankDtos;
            }
            return await FilterListAsync(RootItemBankDtos, ib => ib.Name, value);
        }

        private async Task<IEnumerable<RootIloDto>> SearchIloRootsAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                 (_selectedIloRoot?.Name == null && string.IsNullOrWhiteSpace(value)) ||
                 _selectedIloRoot?.Name?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
            {
                return RootIloDtos;
            }
            return await FilterListAsync(RootIloDtos, ilo => ilo.Name, value);
        }

        private async Task<IEnumerable<T>> FilterListAsync<T>(IEnumerable<T> list, Func<T, string> selector, string value)
        {
            await Task.Delay(250);

            if (string.IsNullOrEmpty(value))
                return list;

            return list.Where(item => selector(item).Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        // FORM CONFIRMATION METHOD:

        public async Task<bool> OnSubmitAsync()
        {
            _isSubmitButtonHit = true;

            bool canProceed = false;

            canProceed = ValidateQuestionMetadataForm();

            if (!canProceed)
            {
                Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);
                return false;
            }

            if (_currentOperationalMode == INSERTION_MODE)
            {
                canProceed = await InsertQuestionMetadataAsync();
            }
            else
            {
                canProceed = await UpdateQuestionMetadataAsync();
            }

            return canProceed;
        }

        private bool ValidateQuestionMetadataForm()
        {
            return !string.IsNullOrWhiteSpace(QuestionMetaDataObject.Code) &&
                   QuestionMetaDataObject.QuestionTypeId > 0 &&
                   _selectedSubjectDto.Id > 0 &&
                   _profile.Id > 0 &&
                   _difficultyLevel.Id > 0 &&
                   (QuestionMetaDataObject.Delta >= 0.0m && QuestionMetaDataObject.Delta <= 1.0m) &&
                   _selectedItemBankRoot != null &&
                   _selectedItemBankNodeFromDialogDto.Id > 0 &&
                   _selectedItemBankNodeFromDialogDto.IsActive &&
                   (QuestionMetaDataObject.QuestionTypeId != (long)Helper.Enums.QuestionType.FileUploadResponse || (_selectedExtensions?.Any() == true));
        }

        public async Task ShouldQuestionTypeBeUpdatedAsync()
        {
            if (_currentOperationalMode == UPDATE_MODE && CreatedQuestionMetaDataId > 0)
            {
                var checkingResult = await BlazQuestionMetaDataService.CheckIfQuestionMetadataHasAnyQuestionDetailsAsync(CreatedQuestionMetaDataId);

                _isQuestionTypeEligibleForUpdate = !checkingResult.HasAnyQuestionDetails;
            }
        }

        private async Task<bool> InsertQuestionMetadataAsync()
        {
            QuestionMetaDataObject.QuestionSubjectId = _selectedSubjectDto.Id;
            QuestionMetaDataObject.QuestionCategoryId = _selectedCategory?.Id > 0
                ? _selectedCategory.Id
                : null;
            QuestionMetaDataObject.DifficultyLevelId = _difficultyLevel.Id;
            QuestionMetaDataObject.DifficultyProfileId = _profile.Id;
            QuestionMetaDataObject.IloId = _selectedIloNodeFromDialogDto.Id == 0 ? null : _selectedIloNodeFromDialogDto.Id;
            QuestionMetaDataObject.ItemBankId = _selectedItemBankNodeFromDialogDto.Id;
            QuestionMetaDataObject.Author = GlobalUserContext.UserEmail ?? "System User";
            QuestionMetaDataObject.IsRoot = true;
            QuestionMetaDataObject.FileUploadResponseSettings = new FileUploadSettingsDto
            {
                QuestionMetadataId = QuestionMetaDataObject.Id,
                ShowAnswerTextArea = QuestionMetaDataObject.FileUploadResponseSettings.ShowAnswerTextArea,
                SupportedFileExtensions = System.Text.Json.JsonSerializer.Serialize(_selectedExtensions),
                UploadedFilesCount = QuestionMetaDataObject.FileUploadResponseSettings.UploadedFilesCount,
                SingleFileMaxSizeInMB = QuestionMetaDataObject.FileUploadResponseSettings.SingleFileMaxSizeInMB
            } ?? new();

            NormalizeQuestionsExhaustionCount();

            QuestionMetaDataObject.OESGroupDtos = SelectedGroups;

            var response = await _blazQuestionService.AddQuestionMetaData(QuestionMetaDataObject);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                CreatedQuestionMetaDataId = JsonConvert.DeserializeObject<long>(response.Data.ToString());

                await OnQuestionCreation.InvokeAsync(CreatedQuestionMetaDataId);

                Snackbar.Add(Resource.QuestionMetadataHasBeenCreatedSuccessfully, Severity.Success);

                _currentOperationalMode = UPDATE_MODE;

                return true;
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);

                return false;
            }
        }

        private async Task<bool> UpdateQuestionMetadataAsync()
        {
            QuestionMetaDataObject.Id = CreatedQuestionMetaDataId;
            QuestionMetaDataObject.QuestionSubjectId = _selectedSubjectDto.Id;
            QuestionMetaDataObject.QuestionCategoryId = _selectedCategory?.Id > 0
                ? _selectedCategory.Id
                : null;
            QuestionMetaDataObject.DifficultyLevelId = _difficultyLevel.Id;
            QuestionMetaDataObject.DifficultyProfileId = _profile.Id;
            QuestionMetaDataObject.IloId = _selectedIloNodeFromDialogDto.Id == 0 ? null : _selectedIloNodeFromDialogDto.Id;
            QuestionMetaDataObject.ItemBankId = _selectedItemBankNodeFromDialogDto.Id;
            QuestionMetaDataObject.FileUploadResponseSettings = new FileUploadSettingsDto
            {
                Id = QuestionMetaDataObject.FileUploadResponseSettings.Id,
                QuestionMetadataId = QuestionMetaDataObject.Id,
                ShowAnswerTextArea = QuestionMetaDataObject.FileUploadResponseSettings.ShowAnswerTextArea,
                SupportedFileExtensions = System.Text.Json.JsonSerializer.Serialize(_selectedExtensions),
                UploadedFilesCount = QuestionMetaDataObject.FileUploadResponseSettings.UploadedFilesCount,
                SingleFileMaxSizeInMB = QuestionMetaDataObject.FileUploadResponseSettings.SingleFileMaxSizeInMB
            };

            QuestionMetaDataObject.OESGroupDtos = SelectedGroups;

            NormalizeQuestionsExhaustionCount();

            var response = await _blazQuestionService.EditQuestionMetaData(QuestionMetaDataObject);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);

                return false;
            }
        }

        // TEMPALTE METHODS:

        private async Task OpenTemplateNameDialog()
        {
            var parameters = new DialogParameters();

            var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.Large };

            var dialog = await DialogService.ShowAsync<TemplateNameDialog>(Resource.QuestionTemplate, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is string templateName)
            {
                TemplateName = templateName;

                await SaveTemplateAsync();
            }
        }

        private async Task SaveTemplateAsync()
        {
            QuestionTemplateObject.Code = QuestionMetaDataObject.Code;
            QuestionTemplateObject.QuestionSubjectId = _selectedSubjectDto.Id;
            QuestionTemplateObject.QuestionTypeId = _selectedQuestionType.Id;
            QuestionTemplateObject.QuestionCategoryId = _selectedCategory?.Id;
            QuestionTemplateObject.DifficultyProfileId = _profile.Id;
            QuestionTemplateObject.DifficultyLevelId = _difficultyLevel.Id;
            QuestionTemplateObject.MaximumAnswerTime = QuestionMetaDataObject.MaximumAnswerTime;
            QuestionTemplateObject.Delta = QuestionMetaDataObject.Delta;
            QuestionTemplateObject.ItemBankId = _selectedItemBankNodeFromDialogDto.Id;
            QuestionTemplateObject.IloId = _selectedIloNodeFromDialogDto.Id == 0 ? null : _selectedIloNodeFromDialogDto.Id;
            QuestionTemplateObject.Name = TemplateName;
            QuestionTemplateObject.Author = GlobalUserContext.UserEmail ?? "System User";
            QuestionTemplateObject.IsRoot = true;
            QuestionTemplateObject.ScientificEditorPanelEnabled = QuestionMetaDataObject.ScientificEditorPanelEnabled;
            QuestionTemplateObject.FileManagerEditorPanelEnabled = QuestionMetaDataObject.FileManagerEditorPanelEnabled;
            QuestionTemplateObject.QuestionsExhaustionCount = QuestionMetaDataObject.QuestionsExhaustionCount;

            var response = await _blazQuestionService.AddTemplateQuestionAsync(QuestionTemplateObject);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    Snackbar.Add(Resource.TemplateDataHasBeenCreatedSuccessfully, Severity.Success);
                    break;

                case HttpStatusCode.Conflict:
                    Snackbar.Add(response.Message, Severity.Warning);
                    break;

                default:
                    Snackbar.Add(Resource.FailedToCreateATemplatePleaseTryEnterAValidData, Severity.Error);
                    break;
            }

            StateHasChanged();
        }

        private async Task ShowTemplateAsync()
        {
            var dialogOptions = new DialogOptions()
            {
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
            };

            var dialogParams = new DialogParameters<QuestionTemplate>
            {
                { p => p.IsFromQuestionAI, false }
            };

            var dialog = await DialogService.ShowAsync<QuestionTemplate>(string.Empty, dialogParams, dialogOptions);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var questionTemplateId = (long)result.Data;

                await FillStepOneFromTemplateAysnc(questionTemplateId);
            }
        }
        private async Task OpenGroupDialog()
        {
            if (_currentOperationalMode == UPDATE_MODE)
            {
                var allowed = await AuthService.IsCurrentUserOwnerAsync(
                    CreatedQuestionMetaDataId,
                    BlazQuestionService.GetQuestionGroupsAsync,
                    dto => [dto.OwnerGroupId ?? Guid.Empty]
                );

                if (!allowed)
                {
                    Snackbar.Add(
                        string.Format(Resource.OnlyCreatorCanManageGroups, Resource.Question),
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
                    var allGroups = await BlazQuestionService.GetAllQuestionGroupsAsync();

                    allGroups = [.. allGroups
                        .Concat(SelectedGroups)
                        .GroupBy(g => g.Id)
                        .Select(g => g.First())];

                    var filteredGroups = allGroups
                        .Where(g => !g.AutoCreatedForUser)
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
                resourceType: ResourceType.Questions,
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

        // OBJECT-STRING CONVERTERS

        private readonly Func<RootItemBankDto, string> ItemBankDtoToStringConverter = p => p.Name;

        private readonly Func<RootIloDto, string> IloDtoToStringConverter = p => p.Name;


        #region Helper Methods

        private void NormalizeQuestionsExhaustionCount()
        {
            if (!DisplayQuestionsExhaustionCount.HasValue || DisplayQuestionsExhaustionCount.Value <= 0)
            {
                QuestionMetaDataObject.QuestionsExhaustionCount = MiscConstants.CommonQuestionExhaustionCount;
            }

            else
            {
                QuestionMetaDataObject.QuestionsExhaustionCount = DisplayQuestionsExhaustionCount.Value;
            }
        }

        #endregion
    }
}