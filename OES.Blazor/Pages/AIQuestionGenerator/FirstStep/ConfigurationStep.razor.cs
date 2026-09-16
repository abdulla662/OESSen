using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using OES.Blazor.Dialogs.Question.IloSelectionDialog;
using OES.Blazor.Dialogs.Question.ItemBankSelectionDialog;
using OES.Blazor.Dialogs.Question.QuestionTemplate;
using OES.Blazor.Dialogs.Question.TemplateDialog;
using OES.Blazor.Services.Interfaces.DifficultyProfile;
using OES.Blazor.Services.Interfaces.ILOSection;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.QuestionCataegory;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Blazor.Services.Interfaces.QuestionType;
using OES.Blazor.Services.Interfaces.Subject;
using OES.Helper.Dtos.AIQuestionGenerator.Request;
using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.Dtos.Subject;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.AIQuestionGenerator.FirstStep
{
    public partial class ConfigurationStep
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IBlazQuestionLanguageService BlazLanguageService { get; set; } = default!;
        [Inject] private IBlazILOService BlazIloService { get; set; } = default!;
        [Inject] private IBlazDifficultyProfileService BlazDifficultyProfileService { get; set; } = default!;
        [Inject] private IBlazItemBankService BlazItemBankService { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private IBLazQuestionType BLazQuestionType { get; set; } = default!;
        [Inject] private IBlazSubjectService BLazSubjectService { get; set; } = default!;
        [Inject] private IBlazQuestionCategoryService BLazQuestionCategoryService { get; set; } = default!;
        [Inject] private IBlazQuestionService BlazQuestionService { get; set; } = default!;

        [Parameter] public bool IsConfigurationLocked { get; set; }
        [Parameter] public bool IsSubmitButtonHit { get; set; }

        private List<LanguageDto> Languages { get; set; } = [];
        private List<RootItemBankDto> RootItemBankDtos { get; set; } = [];
        private List<QuestionCategoryDto> Categories { get; set; } = [];
        private List<SubjectDto> Subjects { get; set; } = [];
        public List<RootIloDto> RootIloDtos { get; set; } = [];
        private List<ProfileDto> DifficultyProfiles { get; set; } = [];
        private List<QuestionTypeDto> QuestionTypes { get; set; } = [];
        private Dictionary<long, int> QuestionTypeCounts { get; set; } = [];
        private IBrowserFile? File { get; set; }
        public string TemplateName { get; set; }

        private int TotalQuestions => QuestionTypeCounts?.Values.Sum() ?? 0;
        private readonly Func<RootItemBankDto, string> ItemBankDtoToStringConverter = p => p?.Name ?? string.Empty;
        private readonly Func<RootIloDto, string> IloDtoToStringConverter = p => p?.Name ?? string.Empty;

        private const int MaxTotalQuestions = 25;
        private static readonly string AllowedExtensionsString = string.Join(", ", Enum.GetValues<DocumentFileType>()
           .Where(e => e != DocumentFileType.Unknown)
           .Select(e => e.ToString().ToUpperInvariant()));

        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;

        private ProfileDto _selectedDifficultyProfile = new();
        private SelectedIloNodeFromDialogDto _selectedIloNodeFromDialogDto = new();
        private SubjectDto _selectedSubject = new();
        private RootIloDto? _selectedIloRoot;
        private QuestionCategoryDto _selectedCategory = new();
        private LanguageDto _selectedLanguage = new();
        private RootItemBankDto? _selectedItemBankRoot;
        private SelectedItemBankNodeFromDialogDto _selectedItemBankNodeFromDialogDto = new();

        protected override async Task OnInitializedAsync()
        {
            var languagesTask = BlazLanguageService.GetAllLanguagesAsync();
            var difficultyProfilesTask = BlazDifficultyProfileService.GetProfiles();
            var rootItemBankDtosTask = BlazItemBankService.GetRootItemBanksNodesAsync();
            var categoriesTask = BLazQuestionCategoryService.GetCategories();
            var subjectsTask = BLazSubjectService.GetAllSubjectsAsync();
            var rootIloDtosTask = BlazIloService.GetRootIlosNodesAsync();

            await Task.WhenAll(
                languagesTask,
                difficultyProfilesTask,
                rootItemBankDtosTask,
                categoriesTask,
                subjectsTask
            );

            Languages = await languagesTask;
            DifficultyProfiles = await difficultyProfilesTask;
            RootItemBankDtos = await rootItemBankDtosTask;
            Categories = await categoriesTask;
            Subjects = await subjectsTask;
            RootIloDtos = await rootIloDtosTask;

            var allQuestionTypes = await BLazQuestionType.GetAllQuestionType() ?? [];

            QuestionTypes = [.. allQuestionTypes.Where(qt =>
                qt.Name.Contains(nameof(Helper.Enums.QuestionType.MCQ), StringComparison.OrdinalIgnoreCase) ||
                qt.Name.Contains(nameof(Helper.Enums.QuestionType.TrueAndFalse), StringComparison.OrdinalIgnoreCase) ||
                qt.Name.Contains(nameof(Helper.Enums.QuestionType.Essay), StringComparison.OrdinalIgnoreCase)
            )];

            foreach (var questionType in QuestionTypes)
            {
                QuestionTypeCounts[questionType.Id] = 0;
            }

            StateHasChanged();
        }

        private void OnUploadFiles(IBrowserFile file)
        {
            var extension = Path.GetExtension(file.Name)?.TrimStart('.').ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || !Enum.TryParse<DocumentFileType>(extension, ignoreCase: true, out var docType) || docType == DocumentFileType.Unknown)
            {
                Snackbar.Add($"{Resource.UnsupportedFileType}. {Resource.SupportedFileExtensions}: {AllowedExtensionsString}", Severity.Error);
                File = null;
                return;
            }

            if (file.Size > MiscConstants.AIQuestionsGenerationMaxFileSizeInBytes)
            {
                Snackbar.Add(Resource.FileSizeExceedsLimit, Severity.Error);
                return;
            }

            File = file;
        }

        private async Task<IEnumerable<RootItemBankDto>> SearchItemBankRootsAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value) || (_selectedItemBankRoot?.Name == null && string.IsNullOrWhiteSpace(value)) || _selectedItemBankRoot?.Name?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
            {
                return RootItemBankDtos;
            }

            await Task.Delay(250, cancellationToken);

            return RootItemBankDtos.Where(ib => ib.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        private async Task OpenItemBankRootTreeDialogAsync()
        {
            var parameters = new DialogParameters<ItemBankSelectionDialog>
            {
                { p => p.ItemBankRootId, _selectedItemBankRoot.Id },
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

        private void ClearItembankSelection()
        {
            _selectedItemBankRoot = null;
            _selectedItemBankNodeFromDialogDto = new();
        }

        private void CaptureSelectedItemBankRoot(RootItemBankDto selectedItemBank)
        {
            _selectedItemBankRoot = selectedItemBank;
            _selectedItemBankNodeFromDialogDto = new();
        }

        private async Task<IEnumerable<RootIloDto>> SearchIloRootsAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value) || (_selectedIloRoot?.Name == null && string.IsNullOrWhiteSpace(value)) || _selectedIloRoot?.Name?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
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

        private async Task<IEnumerable<SubjectDto>> SearchSubjectAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                (_selectedSubject?.Name == null && string.IsNullOrWhiteSpace(value)) ||
                _selectedSubject?.Name?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
            {
                return Subjects;
            }

            return await FilterListAsync(Subjects, s => s.Name, value);
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

        private async Task<IEnumerable<LanguageDto>> SearchLanguageAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                (_selectedLanguage?.Name == null && string.IsNullOrWhiteSpace(value)) ||
                _selectedLanguage?.Name?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
            {
                return Languages;
            }

            return await FilterListAsync(Languages, l => l.Name, value);
        }

        private async Task<IEnumerable<ProfileDto>> SearchDifficultyProfileAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                (_selectedDifficultyProfile?.Name == null && string.IsNullOrWhiteSpace(value)) ||
                _selectedDifficultyProfile?.Name?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
            {
                return DifficultyProfiles;
            }

            return await FilterListAsync(DifficultyProfiles, p => p.Name, value);
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

        private void ClearIloSelection()
        {
            _selectedIloRoot = null;
            _selectedIloNodeFromDialogDto = new();
        }

        private void OnQuestionCountChanged(long questionTypeId, int value)
        {
            if (QuestionTypeCounts == null)
                return;

            QuestionTypeCounts[questionTypeId] = value;

            StateHasChanged();
        }

        private async Task ClearFile()
        {
            File = null;
            await InvokeAsync(StateHasChanged);
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = ["B", "KB", "MB", "GB"];
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }

        private void CaptureSelectedIloRoot(RootIloDto rootIloDto)
        {
            _selectedIloRoot = rootIloDto;
            _selectedIloNodeFromDialogDto = new();
        }

        public AIQuestionGenerationStepperTransferableDto? OnFirstStepSubmit()
        {
            IsSubmitButtonHit = true;

            bool isValid = ValidateForm();

            StateHasChanged();

            if (!isValid)
            {
                Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);
                return null;
            }

            var questionTypeRequests = QuestionTypeCounts
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => new AIQuestionTypeCountDto
                {
                    QuestionTypeId = kvp.Key,
                    QuestionTypeName = QuestionTypes.FirstOrDefault(qt => qt.Id == kvp.Key)?.Name ?? string.Empty,
                    Count = kvp.Value
                })
                .ToList();

            var configuration = new AIQuestionGenerationStepperTransferableDto
            {
                File = File,
                SelectedItemBank = _selectedItemBankNodeFromDialogDto,
                SelectedIlo = _selectedIloNodeFromDialogDto?.Id > 0 ? _selectedIloNodeFromDialogDto : null,
                SelectedSubject = _selectedSubject,
                SelectedCategory = _selectedCategory,
                SelectedLanguage = _selectedLanguage,
                SelectedDifficultyProfile = _selectedDifficultyProfile,
                QuestionTypeRequests = questionTypeRequests
            };

            return configuration;
        }

        private bool ValidateForm()
        {
            return _selectedSubject?.Id > 0 &&
                   _selectedCategory?.Id > 0 &&
                   _selectedLanguage?.Id > 0 &&
                   _selectedDifficultyProfile?.Id > 0 &&
                   _selectedItemBankRoot != null &&
                   _selectedItemBankNodeFromDialogDto?.Id > 0 &&
                   File != null &&
                   TotalQuestions > 0 &&
                   TotalQuestions <= MaxTotalQuestions;
        }

        private async Task ShowTemplateAsync()
        {
            var dialogOptions = new DialogOptions()
            {
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
            };

            var parameters = new DialogParameters<QuestionTemplate>
            {
                { p => p.IsFromQuestionAI, true }
            };

            var dialog = await DialogService.ShowAsync<QuestionTemplate>(string.Empty, parameters, dialogOptions);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var questionTemplateId = (long)result.Data;

                await FillStepOneFromTemplateAysnc(questionTemplateId);
            }
        }

        private async Task FillStepOneFromTemplateAysnc(long questionTemplateId)
        {
            var response = await BlazQuestionService.GetQuestionTemplateById(questionTemplateId);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var questionMetadataDto = (GetQuestionTemplateResponseDto)response.Data;

                await FillQuestionMetaDataAsync(questionMetadataDto.AIQuestionTemplate);
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private async Task FillQuestionMetaDataAsync(AIQuestionTemplateCreationDto questionMetadataDto)
        {
            _selectedSubject = Subjects.Find(s => s.Id == questionMetadataDto.SelectedSubject?.Id) ?? new();

            _selectedCategory = Categories.Find(c => c.Id == questionMetadataDto.SelectedCategory?.Id);

            _selectedLanguage = Languages.Find(l => l.Id == questionMetadataDto.SelectedLanguage?.Id) ?? new();

            _selectedDifficultyProfile = DifficultyProfiles.Find(p => p.Id == questionMetadataDto.SelectedDifficultyProfile?.Id) ?? new();

            var searchingRootItemBankId = questionMetadataDto.SelectedItemBankRoot?.Id is null ? questionMetadataDto.SelectedItemBankRoot.Id : questionMetadataDto.SelectedItemBankRoot?.Id;

            _selectedItemBankRoot = RootItemBankDtos.Find(i => i.Id == searchingRootItemBankId);

            _selectedItemBankNodeFromDialogDto = questionMetadataDto.SelectedItemBankNodeFromDialogDto;

            var searchingRootIloId = questionMetadataDto.SelectedIloRoot?.Id is null ? questionMetadataDto.SelectedIloRoot.Id : questionMetadataDto.SelectedIloRoot?.Id;

            _selectedIloRoot = questionMetadataDto.SelectedIloRoot;

            _selectedIloNodeFromDialogDto = questionMetadataDto.SelectedIloNodeFromDialogDto ?? new();

            QuestionTypeCounts = questionMetadataDto.QuestionTypeRequests.ToDictionary(q => q.QuestionTypeId, q => q.Count);
        }

        private async Task SaveTemplateAsync()
        {
            var questionTypeRequests = QuestionTypeCounts
               .Where(kvp => kvp.Value > 0)
               .Select(kvp => new AIQuestionTypeCountDto
               {
                   QuestionTypeId = kvp.Key,
                   QuestionTypeName = QuestionTypes.FirstOrDefault(qt => qt.Id == kvp.Key)?.Name ?? string.Empty,
                   Count = kvp.Value
               })
               .ToList();

            var template = new AIQuestionTemplateCreationDto
            {
                SelectedItemBankRoot = _selectedItemBankRoot,
                SelectedItemBankNodeFromDialogDto = _selectedItemBankNodeFromDialogDto,
                SelectedIloNodeFromDialogDto = _selectedIloNodeFromDialogDto?.Id > 0 ? _selectedIloNodeFromDialogDto : new(),
                SelectedIloRoot = _selectedIloRoot,
                SelectedSubject = _selectedSubject,
                SelectedCategory = _selectedCategory,
                SelectedLanguage = _selectedLanguage,
                SelectedDifficultyProfile = _selectedDifficultyProfile,
                QuestionTypeRequests = questionTypeRequests,
                Name = TemplateName,
                IsFromAI = true
            };

            var response = await BlazQuestionService.AddTemplateAIQuestionAsync(template);

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
    }
}
