using Ganss.Xss;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Dialogs.FileManager;
using OES.Blazor.Dialogs.Question.IloSelectionDialog;
using OES.Blazor.Dialogs.Question.ItemBankSelectionDialog;
using OES.Blazor.Dialogs.Question.TemplateDialog;
using OES.Blazor.Dialogs.Question.UploadFileTemplate;
using OES.Blazor.Services.Interfaces.DifficultyProfile;
using OES.Blazor.Services.Interfaces.ILOSection;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Blazor.Services.Interfaces.QuestionCataegory;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Blazor.Services.Interfaces.QuestionType;
using OES.Blazor.Services.Interfaces.QuestionUploadTemplate;
using OES.Blazor.Services.Interfaces.Subject;
using OES.Blazor.Services.Interfaces.UploadFile;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.Dtos.FileDetails;
using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Dtos.QuestionUploadTemplateDto.OES.Helper.Dtos.UploadFiles;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.Dtos.Subject;
using OES.Helper.Dtos.UploadFiles;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.ResourceFiles;
using OES.Helper.Static;
using SharedHelper.RolesNames;
using SharedHelper.Services;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OES.Blazor.Pages.UploadFiles
{
    public partial class UploadFile : ComponentBase
    {
        // INJECTIONS:
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazUploadFileService _blazUploadFileService { get; set; }
        [Inject] private IBlazSubjectService _blazSubjectService { get; set; } = default!;
        [Inject] private IBlazDifficultyProfileService _BlazProfileService { get; set; } = default!;
        [Inject] private IBlazQuestionCategoryService _BlazCategoryService { get; set; } = default!;
        [Inject] private IBLazQuestionType _BLazQuestionType { get; set; } = default!;
        [Inject] private IBlazItemBankService BlazItemBankService { get; set; } = default!;
        [Inject] private IBlazQuestionLanguageService BlazQuestionLanguageService { get; set; } = default!;
        [Inject] private IBlazILOService BlazIloService { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private GlobalUserContext GlobalUserContext { get; set; } = default!;
        [Inject] private IBlazQuestionUploadTemplateService UploadFileTemplateService { get; set; } = default!;

        // EXTERNAL PARAMS
        [Parameter] public long CreatedQuestionMetaDataId { get; set; }
        [Parameter] public EventCallback<string> QuestionTypeChanged { get; set; }
        [Parameter] public EventCallback<long> OnQuestionCreation { get; set; }
        [Parameter] public EventCallback<long> QuestionTypeIdChanged { get; set; }

        // COMPONENT PROPS:
        private IFormFile _formFile { get; set; }
        public IEnumerable<QuestionTypeDto> SelectedTypes { get; set; }
        private List<DifficultyLevelDto> DifficultyLevels { get; set; } = [];
        private List<RootItemBankDto> RootItemBankDtos { get; set; } = [];
        public List<RootIloDto> RootIloDtos { get; set; } = [];
        private List<ProfileDto> Profiles { get; set; } = [];
        private List<QuestionCategoryDto> Categories { get; set; } = [];
        private static string[] BypassedSystemRoles { get; } =
        [
            AdminRoles.Admin,
            AdminRoles.Entity_Admin,
            AdminRoles.SuperAdmin
        ];
        public int? DisplayQuestionsExhaustionCount
        {
            get => _displayExhaustionCount;
            set
            {
                _displayExhaustionCount = value;

                questionMetadataAdditionWithUpload.QuestionsExhaustionCount =
                    (!value.HasValue || value.Value <= 0)
                    ? MiscConstants.CommonQuestionExhaustionCount
                    : value.Value;
            }
        }

        // COMPONENT FIELDS:
        private static readonly Dictionary<string, Func<UploadQuestionDetailsDto, int, string?>> QuestionValidators = new(StringComparer.OrdinalIgnoreCase)
        {
            ["mcq"] = ValidateMcq,
            ["multiplechoice"] = ValidateMcq,
            ["multiplecorrectanswers"] = ValidateMultipleCorrectAnswers,
            ["truefalse"] = ValidateTrueFalse,
            ["trueandfalse"] = ValidateTrueFalse,
            ["essay"] = (q, num) => string.IsNullOrWhiteSpace(q.ModelAnswer) ? string.Format(Resource.EssayNoModelAnswer, num) : null,
            ["segmentwithaudioanswer"] = (q, num) => string.IsNullOrWhiteSpace(q.ModelAnswer) ? string.Format(Resource.SegmentNoModelAnswer, num) : null,
            ["segmentwithvideoanswer"] = (q, num) => string.IsNullOrWhiteSpace(q.ModelAnswer) ? string.Format(Resource.SegmentNoModelAnswer, num) : null,
        };
        private IReadOnlyList<IBrowserFile>? _selectedFiles;
        private MudFileUpload<IReadOnlyList<IBrowserFile>>? _fileUpload;
        private List<string> _warnings = [];
        private RootItemBankDto? _selectedItemBankRoot;
        private RootIloDto? _selectedIloRoot;
        private SelectedItemBankNodeFromDialogDto _selectedItemBankNodeFromDialogDto = new();
        private SelectedIloNodeFromDialogDto _selectedIloNodeFromDialogDto = new();
        private DifficultyLevelDto _difficultyLevel = new();
        private SubjectDto _selectedSubjectDto = new();
        private QuestionCategoryDto _selectedCategory = new();
        private LanguageDto _selectedLanguage = new();
        private ProfileDto _profile = new();
        private readonly List<string> _fileNames = [];
        private bool _isSubmitButtonHit = false;
        private bool showData = false;
        private bool _processing;
        private bool _deltaError = false;
        private bool _scientificEditorPanelEnabled = false;
        private bool _fileManagerEditorPanelEnabled = false;
        private string _deltaErrorText = string.Empty;
        private string _dragClass = DefaultDragClass;
        private string _requiredErrorText = Resource.ThisFieldIsRequired;
        private int _questionNumber = 1;
        private const string DefaultDragClass = "relative rounded-lg border-2 border-dashed pa-4 mt-4 mud-width-full mud-height-full";
        private const string INSERTION_MODE = nameof(INSERTION_MODE);
        private const string UPDATE_MODE = nameof(UPDATE_MODE);
        private static readonly Regex _mediaTagsRegex = new(@"<(audio|video|img)\b[^>]?(src\s*=\s*(['""]).*?\3)?[^>]*?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private QuestionMetadataAdditionWithUploadFile questionMetadataAdditionWithUpload = new();
        private const long MaxFileSizeBytes = 100 * 1024 * 1024;
        private UploadMode _mode = UploadMode.ManualSelectItemBank;
        private List<QuestionTypeDto> _questionsTypes = [];
        private List<SubjectDto> _subjects = [];
        private List<LanguageDto> _languages = [];
        private HashSet<UploadQuestionDetailsDto> selectedItems = [];
        private List<UploadQuestionDetailsDto> QuestionToDataBase = [];
        private List<UploadQuestionDetailsDto> remainingQuestions = [];
        private List<UploadQuestionDetailsDto> hasWarningQuestions = [];
        private MudTableElementComparer Comparer = new();
        private List<QuestionTypeDto> _filteredQuestionsTypes = [];
        private Dictionary<string, (long Id, string Name)> _typeMap = new();
        private bool _isQtiFile = false;
        private int? _displayExhaustionCount;

        protected override async Task OnInitializedAsync()
        {
            _subjects = await _blazSubjectService.GetAllSubjectsAsync();
            _questionsTypes = await _BLazQuestionType.GetAllQuestionType();
            _filteredQuestionsTypes = [.. _questionsTypes.Where(t => IsAllowedQuestionType(t.Id))];
            Categories = await _BlazCategoryService.GetCategories();
            Profiles = await _BlazProfileService.GetProfiles();
            RootItemBankDtos = [.. (await BlazItemBankService.GetRootItemBanksNodesAsync()).OrderByDescending(x => x.Id)];
            RootIloDtos = await BlazIloService.GetRootIlosNodesAsync();
            _languages = await BlazQuestionLanguageService.GetAllLanguagesAsync();

            _typeMap = _questionsTypes
                .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                .GroupBy(t => Normalize(t.Name))
                .ToDictionary(g => g.Key, g => (g.First().Id, g.First().Name));

            StateHasChanged();
        }

        private void CaptureSelectedItemBankRoot(RootItemBankDto rootItemBankDto)
        {
            _selectedItemBankRoot = rootItemBankDto;
            _selectedItemBankNodeFromDialogDto = new();
        }

        private static void SetWarning(UploadQuestionDetailsDto q, string message)
        {
            q.HasWarning = true;
            q.Warning = message;
        }

        public static string SanitizeQuestionHtml(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var sanitizer = new HtmlSanitizer();

            sanitizer.AllowedTags.Clear();

            sanitizer.AllowedTags.Add("b");
            sanitizer.AllowedTags.Add("strong");
            sanitizer.AllowedTags.Add("i");
            sanitizer.AllowedTags.Add("em");
            sanitizer.AllowedTags.Add("u");
            sanitizer.AllowedTags.Add("span");
            sanitizer.AllowedTags.Add("p");
            sanitizer.AllowedTags.Add("br");
            sanitizer.AllowedTags.Add("ul");
            sanitizer.AllowedTags.Add("ol");
            sanitizer.AllowedTags.Add("li");
            sanitizer.AllowedTags.Add("img");
            sanitizer.AllowedTags.Add("audio");
            sanitizer.AllowedTags.Add("video");
            sanitizer.AllowedTags.Add("source");

            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedAttributes.Add("class");
            sanitizer.AllowedAttributes.Add("style");
            sanitizer.AllowedAttributes.Add("src");
            sanitizer.AllowedAttributes.Add("controls");

            sanitizer.AllowedCssProperties.Clear();
            sanitizer.AllowedCssProperties.Add("color");
            sanitizer.AllowedCssProperties.Add("background-color");
            sanitizer.AllowedCssProperties.Add("font-weight");
            sanitizer.AllowedCssProperties.Add("font-style");
            sanitizer.AllowedCssProperties.Add("text-decoration");
            sanitizer.AllowedCssProperties.Add("text-align");
            sanitizer.AllowedCssProperties.Add("direction");
            sanitizer.AllowedCssProperties.Add("font-family");
            sanitizer.AllowedCssProperties.Add("font-size");
            sanitizer.AllowedCssProperties.Add("margin");
            sanitizer.AllowedCssProperties.Add("padding");
            sanitizer.AllowedCssProperties.Add("width");
            sanitizer.AllowedCssProperties.Add("height");
            sanitizer.AllowedCssProperties.Add("border");
            sanitizer.AllowedCssProperties.Add("vertical-align");
            sanitizer.AllowedCssProperties.Add("white-space");

            var tagsToFullyStrip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "script", "style", "noscript", "template", "iframe", "object", "embed", "applet"
            };

            sanitizer.RemovingTag += (s, e) =>
            {
                if (tagsToFullyStrip.Contains(e.Tag.TagName))
                {
                    e.Tag.TextContent = string.Empty;
                }
            };

            return sanitizer.Sanitize(input);
        }

        private void CaptureSelectedIloRoot(RootIloDto rootIloDto)
        {
            _selectedIloRoot = rootIloDto;
            _selectedIloNodeFromDialogDto = new();
        }

        private async Task OpenItemBankRootTreeDialogAsync()
        {
            var parameters = new DialogParameters<ItemBankSelectionDialog>
            {
                { p => p.ItemBankRootId,  _selectedItemBankRoot?.Id ?? 0 },
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

        private Task OnModeChanged(UploadMode mode)
        {
            _mode = mode;
            StateHasChanged();
            return Task.CompletedTask;
        }

        private async Task OpenIloRootTreeDialogAsync()
        {
            var parameters = new DialogParameters<IloSelectionDialog>
            {
                { p => p.IloRootId,  _selectedIloRoot?.Id ?? 0 },
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

        private async Task ChangeProfile(ProfileDto profileDto)
        {
            _profile = profileDto;
            await GetDifficultyLevelBasedOnProfileId(_profile.Id);
        }

        private async Task GetDifficultyLevelBasedOnProfileId(long id)
        {
            _difficultyLevel = new();
            DifficultyLevels = await _BLazQuestionType.GetAllDifficultyLevels(id) ?? [];
        }


        // SEARCHING AND FILTRATION METHODS:

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

        private async Task<IEnumerable<LanguageDto>> SearchLanguageAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                (_selectedLanguage?.Name == null && string.IsNullOrWhiteSpace(value)) ||
                _selectedLanguage?.Name?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
            {
                return _languages;
            }

            return await FilterListAsync(_languages, s => s.Name, value);
        }

        private async Task<IEnumerable<QuestionCategoryDto>> SearchCategoryAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                (_selectedCategory?.Name == null && string.IsNullOrWhiteSpace(value)) ||
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

        private static async Task<IEnumerable<T>> FilterListAsync<T>(IEnumerable<T> list, Func<T, string> selector, string value)
        {
            await Task.Delay(250);

            if (string.IsNullOrEmpty(value)) return list;

            await Task.CompletedTask;

            return list.Where(item => selector(item).Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }


        // FORM SUBMIT AND VALIDATION METHODS:

        public async Task OnSubmitAsync()
        {
            _isSubmitButtonHit = true;
            var errs = ValidateForm(_mode);

            if (errs.Any())
            {
                ShowValidationErrors(errs);
                return;
            }

            if (!QuestionToDataBase.Any())
            {
                ShowValidationErrors([ValidationError.NoQuestionsSelected]);
                return;
            }

            await AddQuestions(QuestionToDataBase);

            _questionNumber = 1;
        }

        public async Task OnValidateQuestionsFileAsync()
        {
            _isSubmitButtonHit = true;
            var errs = ValidateForm(_mode);

            if (errs.Any())
            {
                ShowValidationErrors(errs);
                return;
            }

            await ValidateQuestionsFile();
        }

        private void ShowValidationErrors(IEnumerable<ValidationError> errs)
        {
            if (!errs.Any()) return;

            var messages = errs.Select(e => e switch
            {
                //ValidationError.MissingCode => Resource.QuestionCodePrefix,
                ValidationError.MissingSubject => Resource.Selectsubject,
                ValidationError.MissingCategory => Resource.SelectCategory,
                ValidationError.MissingProfile => Resource.SelectDifficultyProfile,
                ValidationError.MissingDifficultyLevel => Resource.SelectDiffcultyLevel,
                ValidationError.MissingLanguage => Resource.Language,
                ValidationError.MissingFiles => Resource.Draganddropfileshereorclick,
                ValidationError.MissingTypes => Resource.SelectType,
                ValidationError.MissingDelta => Resource.DeltaValueIsRequired,
                ValidationError.DeltaOutOfRange => $"{Resource.DeltaValueMustBeBetween}{_difficultyLevel.FromDelta} {Resource.And} {_difficultyLevel.ToDelta}",
                ValidationError.MissingItemBank_Manual => Resource.SelectNodeBeforeProceeding,
                ValidationError.NoQuestionsSelected => Resource.Selectonequestion,
                _ => Resource.Unknown
            });

            Snackbar.Add(string.Join(" • ", messages.Distinct()), Severity.Error);
        }

        private IEnumerable<ValidationError> ValidateForm(UploadMode mode)
        {
            var errs = new List<ValidationError>();

            //if (string.IsNullOrWhiteSpace(questionMetadataAdditionWithUpload.Code)) errs.Add(ValidationError.MissingCode);

            if ((_selectedSubjectDto?.Id ?? 0) == 0) errs.Add(ValidationError.MissingSubject);
            if ((_selectedCategory?.Id ?? 0) == 0) errs.Add(ValidationError.MissingCategory);
            if ((_profile?.Id ?? 0) == 0) errs.Add(ValidationError.MissingProfile);
            if ((_difficultyLevel?.Id ?? 0) == 0) errs.Add(ValidationError.MissingDifficultyLevel);
            if ((_selectedLanguage?.Id ?? 0) == 0) errs.Add(ValidationError.MissingLanguage);

            if (_selectedFiles == null || !_selectedFiles.Any())
                errs.Add(ValidationError.MissingFiles);

            if (SelectedTypes == null || !SelectedTypes.Any())
                errs.Add(ValidationError.MissingTypes);

            var delta = questionMetadataAdditionWithUpload.Delta;
            var hasDelta = delta >= 0.0m && delta <= 1.0m;

            if (!hasDelta)
                errs.Add(ValidationError.MissingDelta);
            else if (_difficultyLevel != null && (delta < _difficultyLevel.FromDelta || delta > _difficultyLevel.ToDelta))
                errs.Add(ValidationError.DeltaOutOfRange);

            if (mode == UploadMode.ManualSelectItemBank && _selectedItemBankNodeFromDialogDto.Id == 0)
                errs.Add(ValidationError.MissingItemBank_Manual);

            return errs;
        }

        private async Task ClearAsync()
        {
            await (_fileUpload?.ClearAsync() ?? Task.CompletedTask);
            _fileNames.Clear();
            showData = false;
            _selectedFiles = null;
            _isSubmitButtonHit = false;
            _isQtiFile = false;
            ClearDragClass();
            _questionNumber = 1;
        }

        private async Task OnInputFileChanged(InputFileChangeEventArgs e)
        {
            var files = e?.GetMultipleFiles() ?? [];

            ClearDragClass();
            _fileNames.Clear();
            _selectedFiles = files;

            if (_selectedFiles == null || !_selectedFiles.Any()) return;

            var browserFile = _selectedFiles.FirstOrDefault();

            if (browserFile != null)
            {
                var fileName = browserFile.Name.ToLower();

                _isQtiFile = fileName.EndsWith(".txt");

                var (isValid, error) = await DocLibFileUploadValidationService.ValidateAsync(browserFile, MaxFileSizeBytes);

                if (!isValid)
                {
                    Snackbar.Add(error ?? Resource.InvalidFileFormat, Severity.Error);
                    _selectedFiles = null;
                    return;
                }

                var ms = new MemoryStream();
                await browserFile.OpenReadStream(MaxFileSizeBytes).CopyToAsync(ms);
                ms.Position = 0;

                _formFile = new FormFile(ms, 0, ms.Length, "formFile", browserFile.Name)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = browserFile.ContentType
                };
            }

            foreach (var file in _selectedFiles)
                _fileNames.Add(file.Name);
        }

        private async Task<List<UploadQuestionDetailsDto>> ValidateQuestionsFile()
        {
            _processing = true;

            //questionMetadataAdditionWithUpload.Code = QuestionMetaDataDto.Code;
            questionMetadataAdditionWithUpload.QuestionSubjectId = _selectedSubjectDto.Id;
            questionMetadataAdditionWithUpload.QuestionCategoryId = _selectedCategory?.Id;
            questionMetadataAdditionWithUpload.SelectedTypeNames = SelectedTypes.Select(type => type.Name).ToList();
            questionMetadataAdditionWithUpload.DifficultyLevelId = _difficultyLevel.Id;
            questionMetadataAdditionWithUpload.DifficultyProfileId = _profile.Id;
            questionMetadataAdditionWithUpload.IloId = _selectedIloNodeFromDialogDto.Id == 0 ? null : _selectedIloNodeFromDialogDto.Id;
            questionMetadataAdditionWithUpload.ItemBankId = _mode == UploadMode.ManualSelectItemBank
              ? _selectedItemBankNodeFromDialogDto.Id
              : 0;

            questionMetadataAdditionWithUpload.LanguageId = _selectedLanguage.Id;
            questionMetadataAdditionWithUpload.FileManagerEditorPanelEnabled = _fileManagerEditorPanelEnabled;
            questionMetadataAdditionWithUpload.ScientificEditorPanelEnabled = _scientificEditorPanelEnabled;
            questionMetadataAdditionWithUpload.Author = GlobalUserContext.UserEmail ?? MiscConstants.DefaultSystemUser;
            questionMetadataAdditionWithUpload.IsRoot = true;
            questionMetadataAdditionWithUpload.FormFile = _formFile;
            questionMetadataAdditionWithUpload.QuestionsExhaustionCount = questionMetadataAdditionWithUpload.QuestionsExhaustionCount; // This makes sure that the QuestionsExhaustionCount property gets the latest updated value ..

            var result = await _blazUploadFileService.ValidateQuestionsFile(
                questionMetadataAdditionWithUpload.FormFile,
                questionMetadataAdditionWithUpload.SelectedTypeNames
            );

            if (result.CustomCodeStatus == Helper.Enums.CustomCodeStatus.Success && result.Data is not null)
            {
                JsonElement dataElement;

                try
                {
                    dataElement = JsonSerializer.Deserialize<JsonElement>(result.Data.ToString());
                }
                catch
                {
                    Snackbar.Add(Resource.FailedToCreateQuestionMetadata, Severity.Error);
                    _processing = false;
                    return [];
                }

                var validQuestionsJson = dataElement.TryGetProperty("questions", out var questionsProperty) ? questionsProperty.ToString() : "[]";
                var warningsJson = dataElement.TryGetProperty("warnings", out var warningsProperty) ? warningsProperty.ToString() : "[]";
                var fileDetailsProbabilitiesJson = dataElement.TryGetProperty("fileDetailsProbabilities", out var fileDetailsProbabilitiesProperty) ? fileDetailsProbabilitiesProperty.ToString() : null;

                _warnings = JsonSerializer.Deserialize<List<string>>(warningsJson) ?? new();

                var questions = JsonSerializer.Deserialize<List<UploadQuestionDetailsDto>>(validQuestionsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                if (!ValidateFileAgainstUploadMode(questions))
                {
                    showData = false;
                    _processing = false;
                    return [];
                }

                var fileProbabilities = string.IsNullOrWhiteSpace(fileDetailsProbabilitiesJson)
                    ? null
                    : JsonSerializer.Deserialize<UploadedFileAndSimilarities>(fileDetailsProbabilitiesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (fileProbabilities?.FileNameAndProbabilities?.Any() == true)
                {
                    var resultDialog = await OpenFileDetailsDialogAsync(fileProbabilities);
                    if (resultDialog)
                    {
                        ProcessQuestions(questions);
                        _processing = false;
                        return questions;
                    }
                    else
                    {
                        showData = false;
                        Snackbar.Add(Resource.YouCancelThisFile, Severity.Warning);
                    }
                }
                else
                {
                    ProcessQuestions(questions);
                }
            }
            else
            {
                Snackbar.Add(Resource.FileProcessingError, Severity.Error);
            }

            _processing = false;

            return [];
        }

        private bool ValidateFileAgainstUploadMode(List<UploadQuestionDetailsDto> questions)
        {
            bool hasAnyItemBankCode = questions.Any(q => !string.IsNullOrWhiteSpace(q.ItemBankCode));

            if (_mode == UploadMode.AutoSelectItemBank && !hasAnyItemBankCode)
            {
                Snackbar.Add(
                    Resource.AutoModeRequiresItemBankCode,
                    Severity.Error
                );

                return false;
            }

            if (_mode == UploadMode.ManualSelectItemBank && hasAnyItemBankCode)
            {
                Snackbar.Add(
                    Resource.ManualModeMustNotContainItemBankCode,
                    Severity.Error
                );

                return false;
            }

            return true;
        }

        private static string? ValidateMcq(UploadQuestionDetailsDto q, int num)
        {
            var choices = q.Choices?.ToList() ?? new();

            if (choices.Count < 2)
                return string.Format(Resource.MCQMinChoices, num);


            if (!choices.Any(c => c.IsCorrectAnswer))
                return string.Format(Resource.MCQOneCorrectAnswer, num);


            if (choices.Count(c => c.IsCorrectAnswer) > 1)
                return string.Format(Resource.MCQMultipleCorrectAnswers, num);

            return null;
        }

        private static string? ValidateMultipleCorrectAnswers(UploadQuestionDetailsDto q, int num)
        {
            var choices = q.Choices?.ToList() ?? new();

            if (choices.Count < 2)
                return string.Format(Resource.MultipleAnswersMinChoices, num);

            if (!choices.Any(c => c.IsCorrectAnswer))
                return string.Format(Resource.MultipleAnswersOneCorrect, num);

            return null;
        }

        private static string? ValidateTrueFalse(UploadQuestionDetailsDto q, int num)
        {
            var choices = q.Choices?.ToList() ?? new();
            var correctCount = choices.Count(c => c.IsCorrectAnswer);

            if (choices.Count != 2)
                return string.Format(Resource.TrueFalseTwoChoices, num);

            int validChoiceCount = 0;
            foreach (var choice in choices)
            {
                var cleanText = Regex.Replace(choice.ChoiceText ?? "", @"<[^>]+>", "").Trim();
                cleanText = Regex.Replace(cleanText, @"[^\w\s]", "").Trim();

                if (TrueFalseValidationConstants.AllValidKeywords.Contains(cleanText))
                {
                    validChoiceCount++;
                }
            }

            if (validChoiceCount != 2)
                return string.Format(Resource.TrueFalseOnlyTrueFalse, num);

            if (correctCount != 1)
                return string.Format(Resource.TrueFalseOneCorrect, num);

            return null;
        }

        private void ProcessQuestions(List<UploadQuestionDetailsDto> questions)
        {
            showData = true;
            StateHasChanged();

            foreach (var q in questions)
            {
                var valueBeforeProcessing = q.Body?.ToString() ?? string.Empty;

                q.Body = SanitizeQuestionHtml(valueBeforeProcessing);
                q.ModelAnswer = SanitizeQuestionHtml(q.ModelAnswer ?? string.Empty);
                q.DisplayOrder = _questionNumber;

                _questionNumber++;

                if (q.Choices != null)
                {
                    foreach (var choice in q.Choices)
                    {
                        choice.ChoiceText = SanitizeQuestionHtml(choice.ChoiceText ?? string.Empty);
                    }
                }

                if (q.Id == 0)
                    q.Id = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0);

                var key = Normalize(q.QuestionType ?? "");

                if (!string.IsNullOrWhiteSpace(key) && _typeMap.TryGetValue(key, out var info))
                {
                    q.QuestionType = info.Name;
                    q.QuestionTypeId = info.Id;
                }
                else
                {
                    q.QuestionTypeId = 0;
                    q.HasWarning = true;
                    q.Warning ??= string.Format(Resource.UndefinedQuestionType, q.QuestionType);
                }

                if (!string.IsNullOrWhiteSpace(q.QuestionCode))
                {
                    q.QuestionCode = q.QuestionCode.Trim();
                }

                if (!string.IsNullOrWhiteSpace(q.QuestionType))
                {
                    if (QuestionValidators.TryGetValue(key, out var validator))
                    {
                        var message = validator(q, _questionNumber - 1);
                        if (!string.IsNullOrWhiteSpace(message))
                            SetWarning(q, message);
                    }
                    else
                    {
                        SetWarning(q, string.Format(Resource.UndefinedQuestionType, _questionNumber - 1, q.QuestionType));

                        if (string.IsNullOrWhiteSpace(q.ModelAnswer) &&
                            (q.Choices == null || !q.Choices.Any()))
                        {
                            SetWarning(q, string.Format(Resource.NoAnswerProvided, _questionNumber - 1));
                        }
                    }
                }
            }

            hasWarningQuestions = [.. questions.Where(q => q.HasWarning)];
            remainingQuestions = [.. questions.Where(q => !q.HasWarning)];

            if (hasWarningQuestions.Count > 0)
                _warnings = hasWarningQuestions.ConvertAll(q => q.Warning ?? Resource.InvalidQuestion);

            QuestionToDataBase = [];

            if (_isQtiFile && _mode == UploadMode.AutoSelectItemBank)
            {
                Snackbar.Add(Resource.FilesWordOrQTIMustBeUploadedInManualMode, Severity.Warning);
                return;
            }
        }
        private void OnSelectedItemsChanged(HashSet<UploadQuestionDetailsDto> selectedItems1)
        {
            var validSelectedItems = (_mode == UploadMode.ManualSelectItemBank)
                ? [.. selectedItems1.Where(item => !item.HasWarning)]
                : selectedItems1.Where(item =>
                {
                    var bad = GetIssueTypeForAutoMode(item);
                    return !item.HasWarning && !ShouldExclude(bad) && item.QuestionTypeId > 0;
                }).ToList();

            if (validSelectedItems.Count > 0)
            {
                var newItems = validSelectedItems.ConvertAll(item =>
                {
                    var clone = new UploadQuestionDetailsDto
                    {
                        Body = item.Body ?? string.Empty,
                        ModelAnswer = item.ModelAnswer,
                        QuestionType = item.QuestionType,
                        HasWarning = item.HasWarning,
                        Choices = item.Choices,
                        Id = 0,
                        QuestionTypeId = item.QuestionTypeId,
                        QuestionCode = item.QuestionCode,
                        UploadMode = _mode
                    };

                    if (_mode == UploadMode.AutoSelectItemBank)
                    {
                        clone.ItemBankCode = item.ItemBankCode;
                        clone.ItemBankId = item.ItemBankId;
                    }

                    return clone;
                });

                QuestionToDataBase = newItems;
            }
            else
            {
                QuestionToDataBase.Clear();
            }
        }

        private async Task AddQuestions(List<UploadQuestionDetailsDto> QuestionToDataBase)
        {
            var partitions = QuestionToDataBase;

            var exhaustionCount = (!DisplayQuestionsExhaustionCount.HasValue || DisplayQuestionsExhaustionCount.Value <= 0)
                  ? MiscConstants.CommonQuestionExhaustionCount
                  : DisplayQuestionsExhaustionCount.Value;

            var meta = new QuestionMetadataAdditionWithUploadFile
            {
                QuestionSubjectId = _selectedSubjectDto.Id,
                QuestionCategoryId = _selectedCategory?.Id > 0 ? _selectedCategory.Id : null,
                SelectedTypeNames = [.. SelectedTypes.Select(t => t.Name)],
                DifficultyLevelId = _difficultyLevel.Id,
                DifficultyProfileId = _profile.Id,
                IloId = _selectedIloNodeFromDialogDto.Id == 0 ? null : _selectedIloNodeFromDialogDto.Id,
                ItemBankId = _selectedItemBankNodeFromDialogDto.Id,
                LanguageId = _selectedLanguage.Id,
                Author = GlobalUserContext.UserEmail ?? MiscConstants.DefaultSystemUser,
                IsRoot = true,
                QuestionStatus = QuestionStatus.LayoutSelectedAndPending,
                QuestionsExhaustionCount = exhaustionCount,
                Delta = questionMetadataAdditionWithUpload.Delta,
                MaximumAnswerTime = questionMetadataAdditionWithUpload.MaximumAnswerTime,
                Code = questionMetadataAdditionWithUpload.Code,
                UploadMode = _mode
            };

            var payload = JsonSerializer.Serialize(partitions);
            var resp = await _blazUploadFileService.AddQuestionsAndMetaData(payload, meta);

            if (resp.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add($"{string.Format(resp.Message)}", Severity.Success);

                QuestionToDataBase.Clear();
                selectedItems.Clear();
                _fileNames.Clear();
                showData = false;
                await ClearAsync();
            }
            else
            {
                Snackbar.Add($"{Resource.NothingWasAdded}", Severity.Error);
            }

            return;
        }

        private static string GetChoiceStyle(UploadChoicesDto choice) => choice.IsCorrectAnswer ? "color: green; font-weight: bold;" : "";

        private void SetDragClass() => _dragClass = $"{DefaultDragClass} mud-border-primary";

        private void ClearDragClass() => _dragClass = DefaultDragClass;

        private static string Normalize(string s) => (s ?? "").Trim().ToLower();


        private void ValidateDifficultyValue()
        {
            var deltaValue = questionMetadataAdditionWithUpload.Delta;

            if (_difficultyLevel != null &&
                (deltaValue < _difficultyLevel.FromDelta || deltaValue > _difficultyLevel.ToDelta))
            {
                _deltaError = true;
                _deltaErrorText = $"{Resource.DeltaValueMustBeBetween} {_difficultyLevel.FromDelta} {Resource.And} {_difficultyLevel.ToDelta}";
            }
            else
            {
                _deltaError = false;
                _deltaErrorText = string.Empty;
            }
        }

        private async Task<bool> OpenFileDetailsDialogAsync(UploadedFileAndSimilarities fileProbabilities)
        {
            var parameters = new DialogParameters<FileDetailsProbabilitiesDialog>
            {
                { p => p.FileProbabilities, fileProbabilities }
            };

            var options = new DialogOptions()
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<FileDetailsProbabilitiesDialog>(@Resource.FileDetailsProbabilities, parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                bool proceedClicked = (bool)result.Data;
                return proceedClicked;
            }

            return false;
        }

        private async Task Download(DownloadFileType filetype)
        {
            (string url, string fileName) = filetype switch
            {
                DownloadFileType.WordManualItemBanks => ($"{MiscConstants.TemplatesFolder}/QuestionsUploadFile-ManualTemplate.docx", Resource.WordManualTemplateFileName),
                DownloadFileType.WordAutoItemBanks => ($"{MiscConstants.TemplatesFolder}/QuestionsUploadFile-AutoTemplate.docx", Resource.WordAutoTemplateFileName),
                DownloadFileType.ExcelManualItemBanks => ($"{MiscConstants.TemplatesFolder}/QuestionsUploadFile-ManualTemplate.xlsx", Resource.ExcelManualTemplateFileName),
                DownloadFileType.ExcelAutoItemBanks => ($"{MiscConstants.TemplatesFolder}/QuestionsUploadFile-AutoTemplate.xlsx", Resource.ExcelAutoTemplateFileName),
                DownloadFileType.Pdf => ($"{MiscConstants.TemplatesFolder}/QuestionsUploadFile-Template.pdf", Resource.PDFTemplateFileName),
                DownloadFileType.XML => ($"{MiscConstants.TemplatesFolder}/QuestionsUploadFile-Template.xml", Resource.QtiTemplateFileName),
                _ => ("", "")
            };

            await JS.InvokeVoidAsync("downloadFileFromUrl", url, fileName);
        }

        private static IssueType GetIssueTypeForAutoMode(UploadQuestionDetailsDto q)
        {
            if (string.IsNullOrWhiteSpace(q?.QuestionType) || q.QuestionTypeId <= 0) return IssueType.UnknownType;
            if (string.IsNullOrWhiteSpace(q?.ItemBankCode)) return IssueType.MissingItemBankName;
            return IssueType.None;
        }

        private async Task SaveCurrentTemplateAsync()
        {
            var options = new DialogOptions()
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<TemplateNameDialog>(Resource.SaveAsTemplate, options);

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is string templateName)
            {
                var dto = new QuestionUploadTemplateDto
                {
                    Name = templateName,
                    Code = questionMetadataAdditionWithUpload.Code,
                    SubjectId = _selectedSubjectDto.Id,
                    CategoryId = _selectedCategory.Id,
                    ProfileId = _profile.Id,
                    DifficultyLevelId = _difficultyLevel.Id,
                    LanguageId = _selectedLanguage.Id,
                    SelectedTypeIds = SelectedTypes?.Select(x => x.Id).ToList() ?? [],
                    QuestionsExhaustionCount = (int)questionMetadataAdditionWithUpload.QuestionsExhaustionCount,
                    Delta = questionMetadataAdditionWithUpload.Delta,
                    MaximumAnswerTime = questionMetadataAdditionWithUpload.MaximumAnswerTime,
                    ItemBankNodeId = _selectedItemBankNodeFromDialogDto.Id > 0 ? _selectedItemBankNodeFromDialogDto.Id : null,
                    IloNodeId = _selectedIloNodeFromDialogDto.Id > 0 ? _selectedIloNodeFromDialogDto.Id : null,
                    FileManagerEditorPanelEnabled = _fileManagerEditorPanelEnabled,
                    ScientificEditorPanelEnabled = _scientificEditorPanelEnabled
                };

                var response = await UploadFileTemplateService.AddTemplateAsync(dto);

                if (response.CustomCodeStatus == CustomCodeStatus.AlreadyExist)
                    Snackbar.Add(response.Message, Severity.Error);
                else if (response.CustomCodeStatus == CustomCodeStatus.Success)
                    Snackbar.Add(response.Message, Severity.Success);
                else
                    Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private async Task OpenSavedTemplatesDialogAsync()
        {
            var options = new DialogOptions()
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<UploadFileTemplateDialog>(string.Empty, options);

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is QuestionUploadTemplateDto selectedTemplate)
            {
                questionMetadataAdditionWithUpload.Code = selectedTemplate.Code;
                _selectedSubjectDto = _subjects.FirstOrDefault(s => s.Id == selectedTemplate.SubjectId) ?? new();
                _selectedCategory = Categories.FirstOrDefault(c => c.Id == selectedTemplate.CategoryId) ?? new();
                _profile = Profiles.FirstOrDefault(p => p.Id == selectedTemplate.ProfileId) ?? new();
                await GetDifficultyLevelBasedOnProfileId(_profile.Id);
                _difficultyLevel = DifficultyLevels.FirstOrDefault(d => d.Id == selectedTemplate.DifficultyLevelId) ?? new DifficultyLevelDto();
                _selectedLanguage = _languages.FirstOrDefault(l => l.Id == selectedTemplate.LanguageId) ?? new();
                SelectedTypes = [.. _questionsTypes.Where(q => selectedTemplate.SelectedTypeIds.Contains(q.Id))];
                questionMetadataAdditionWithUpload.QuestionsExhaustionCount = selectedTemplate.QuestionsExhaustionCount;
                questionMetadataAdditionWithUpload.Delta = selectedTemplate.Delta;
                questionMetadataAdditionWithUpload.MaximumAnswerTime = selectedTemplate.MaximumAnswerTime ?? 0;

                if (selectedTemplate.ItemBankNodeId.HasValue && selectedTemplate.ItemBankNodeId.Value > 0)
                {
                    var itemBankDto = await BlazItemBankService.GetItemBankByID(selectedTemplate.ItemBankNodeId.Value);
                    if (itemBankDto != null)
                    {
                        _selectedItemBankNodeFromDialogDto = new SelectedItemBankNodeFromDialogDto
                        {
                            Id = itemBankDto.Id,
                            Name = itemBankDto.Name
                        };

                        _selectedItemBankRoot = new RootItemBankDto
                        {
                            Id = itemBankDto.Id,
                            Name = itemBankDto.Name
                        };

                        if (!RootItemBankDtos.Any(x => x.Id == itemBankDto.Id))
                        {
                            RootItemBankDtos.Add(_selectedItemBankRoot);
                        }
                    }
                    else
                    {
                        _selectedItemBankNodeFromDialogDto = new();
                        _selectedItemBankRoot = new();
                    }
                }

                if (selectedTemplate.IloNodeId.HasValue && selectedTemplate.IloNodeId.Value > 0)
                {
                    var iloResponse = await BlazIloService.GetILORoot(selectedTemplate.IloNodeId.Value);
                    if (iloResponse?.Data is ILODto iloDto)
                    {
                        _selectedIloNodeFromDialogDto = new SelectedIloNodeFromDialogDto
                        {
                            Id = iloDto.Id,
                            Name = iloDto.Name
                        };

                        _selectedIloRoot = new RootIloDto
                        {
                            Id = iloDto.Id,
                            Name = iloDto.Name
                        };

                        if (!RootIloDtos.Any(x => x.Id == iloDto.Id))
                        {
                            RootIloDtos.Add(_selectedIloRoot);
                        }
                    }
                    else
                    {
                        _selectedIloNodeFromDialogDto = new();
                        _selectedIloRoot = new();
                    }
                }

                StateHasChanged();
            }
        }

        private static bool ShouldExclude(IssueType t)
            => t == IssueType.UnknownType || t == IssueType.UnresolvedItemBank || t == IssueType.MissingItemBankName || t == IssueType.NotAssignedToItemBank;

        private static bool IsAllowedQuestionType(long typeId)
        {
            var allowedTypes = new[]
            {
                (long)Helper.Enums.QuestionType.Essay,
                (long)Helper.Enums.QuestionType.MCQ,
                (long)Helper.Enums.QuestionType.MultipleCorrectAnswers,
                (long)Helper.Enums.QuestionType.TrueAndFalse
            };

            return allowedTypes.Contains(typeId);
        }

        private readonly Func<RootItemBankDto, string> ItemBankDtoToStringConverter = p => p.Name;

        private readonly Func<RootIloDto, string> IloDtoToStringConverter = p => p.Name;
    }
}
