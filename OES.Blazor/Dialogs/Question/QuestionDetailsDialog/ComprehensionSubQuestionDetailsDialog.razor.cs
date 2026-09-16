using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Blazor.Services.Interfaces.QuestionType;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.ComprehensionSubQuestionDtos;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text.Json;


namespace OES.Blazor.Dialogs.Question.QuestionDetailsDialog
{
    public partial class ComprehensionSubQuestionDetailsDialog : ComponentBase
    {
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazQuestionService BlazQuestionService { get; set; }
        [Inject] private IBLazQuestionType BLazQuestionTypeService { get; set; } = default!;
        [Inject] private IBlazQuestionLanguageService BlazQuestionLanguage { get; set; }

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; }
        [Parameter] public long ParentMetadataId { get; set; }
        [Parameter] public QuestionMetadataRetrievalDto SubQuestionMetadataRetrievalDto { get; set; } = new();
        [Parameter] public ComprehensionSubQuestionDto ReceivedComprehensionSubQuestionDto { get; set; } = new();
        [Parameter] public string DefaultCode { get; set; }
        [Parameter] public bool IsReplaceMode { get; set; }

        private QuestionTypeDto SelectedQuestionType { get; set; } = new();
        private List<QuestionTypeDto> QuestionsTypesExceptComprehension { get; set; } = [];
        private List<LanguageDto> LanguagesList { get; set; } = [];
        private LanguageDto SelectedLanguage { get; set; } = new();

        private bool _isSubmitButtonHit = false;

        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;

        private const string INSERTION_MODE = nameof(INSERTION_MODE);

        private const string UPDATE_MODE = nameof(UPDATE_MODE);

        private string _currentOperationalMode = INSERTION_MODE;

        private bool _questionTypeErrorShown = false;

        private ISet<FileUploadExtension> _selectedExtensionsEnum { get; set; } = new HashSet<FileUploadExtension>
        {
            FileUploadExtension.PNG,
            FileUploadExtension.DOCX
        };

        private static IEnumerable<FileUploadExtension> _availableExtensionsEnum => Enum.GetValues<FileUploadExtension>();


        protected override async Task OnInitializedAsync()
        {
            if (ReceivedComprehensionSubQuestionDto.SubQuestionDetailsDto.Id > 0)
            {
                _currentOperationalMode = UPDATE_MODE;

                var response = await BlazQuestionService.GetQuestionByMetaDataIdAndLanguageId(
                    SubQuestionMetadataRetrievalDto.Id,
                    ReceivedComprehensionSubQuestionDto.SubQuestionDetailsDto.LanguageId
                );

                if (response?.Data is QuestionDataDto questionData)
                {
                    ReceivedComprehensionSubQuestionDto.SubQuestionDetailsDto.MaxWords = questionData.MaxWords;

                    ReceivedComprehensionSubQuestionDto.SubQuestionDetailsDto.AttachmentFileName = questionData.AttachmentFileName;
                }
            }

            if (!string.IsNullOrWhiteSpace(SubQuestionMetadataRetrievalDto.Code))
            {
                ReceivedComprehensionSubQuestionDto.SubQuestionMetadataDto.Code = SubQuestionMetadataRetrievalDto.Code;
            }
            else if (!string.IsNullOrWhiteSpace(DefaultCode))
            {
                ReceivedComprehensionSubQuestionDto.SubQuestionMetadataDto.Code = DefaultCode;
            }

            if (SubQuestionMetadataRetrievalDto.Id > 0)
            {
                ReceivedComprehensionSubQuestionDto.SubQuestionMetadataDto.Delta = SubQuestionMetadataRetrievalDto.Delta;
            }

            var allQuestionsTypes = await BLazQuestionTypeService.GetAllQuestionType();

            allQuestionsTypes.RemoveAll(type => type.Name.Contains(nameof(QuestionType.Comprehension)));

            QuestionsTypesExceptComprehension = allQuestionsTypes;

            LanguagesList = await BlazQuestionLanguage.GetAllQuestionDetailsLanguagesGroupAsync(ParentMetadataId);

            SelectedLanguage = LanguagesList.Find(l => l.Id == ReceivedComprehensionSubQuestionDto.SubQuestionDetailsDto.LanguageId) ?? new();

            if (SubQuestionMetadataRetrievalDto.QuestionTypeId > 0)
            {
                SelectedQuestionType = QuestionsTypesExceptComprehension.Find(type => type.Id == SubQuestionMetadataRetrievalDto.QuestionTypeId);

                if (SelectedQuestionType?.Id == (long)QuestionType.FileUploadResponse && SubQuestionMetadataRetrievalDto.FileUploadSettings != null && !string.IsNullOrWhiteSpace(SubQuestionMetadataRetrievalDto.FileUploadSettings.SupportedFileExtensions))
                {
                    var parsedExtensions = JsonSerializer.Deserialize<HashSet<FileUploadExtension>>(SubQuestionMetadataRetrievalDto.FileUploadSettings.SupportedFileExtensions);

                    _selectedExtensionsEnum = parsedExtensions ?? [];
                }
            }

            StateHasChanged();
        }

        private void OnSelectedExtensionsEnumChanged(IEnumerable<FileUploadExtension> newSelection)
        {
            _selectedExtensionsEnum = new HashSet<FileUploadExtension>(newSelection);
        }

        private async Task HandleSubQuestionDetailsFormAsync(ComprehensionSubQuestionDto comprehensionSubQuestionDto)
        {
            comprehensionSubQuestionDto.SubQuestionMetadataDto.ParentId = ParentMetadataId;

            comprehensionSubQuestionDto.SubQuestionMetadataDto.QuestionTypeId = SelectedQuestionType.Id;

            comprehensionSubQuestionDto.SubQuestionMetadataDto.Code = ReceivedComprehensionSubQuestionDto.SubQuestionMetadataDto.Code;

            comprehensionSubQuestionDto.SubQuestionMetadataDto.Delta = ReceivedComprehensionSubQuestionDto.SubQuestionMetadataDto.Delta;

            if (SelectedLanguage.Id == 0)
            {
                _isSubmitButtonHit = true;

                StateHasChanged();

                Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);

                return;
            }

            comprehensionSubQuestionDto.SubQuestionDetailsDto.LanguageId = SelectedLanguage.Id;

            if (SelectedQuestionType?.Id == (long)QuestionType.FileUploadResponse)
            {
                comprehensionSubQuestionDto.SubQuestionMetadataDto.FileUploadSettings ??= new();
                comprehensionSubQuestionDto.SubQuestionMetadataDto.FileUploadSettings.QuestionMetadataId = SubQuestionMetadataRetrievalDto.Id;
                comprehensionSubQuestionDto.SubQuestionMetadataDto.FileUploadSettings.Id = SubQuestionMetadataRetrievalDto.FileUploadSettings.Id;
                comprehensionSubQuestionDto.SubQuestionMetadataDto.FileUploadSettings.UploadedFilesCount = SubQuestionMetadataRetrievalDto.FileUploadSettings.UploadedFilesCount;
                comprehensionSubQuestionDto.SubQuestionMetadataDto.FileUploadSettings.SingleFileMaxSizeInMB = SubQuestionMetadataRetrievalDto.FileUploadSettings.SingleFileMaxSizeInMB;
                comprehensionSubQuestionDto.SubQuestionMetadataDto.FileUploadSettings.ShowAnswerTextArea = SubQuestionMetadataRetrievalDto.FileUploadSettings.ShowAnswerTextArea;
                comprehensionSubQuestionDto.SubQuestionMetadataDto.FileUploadSettings.SupportedFileExtensions = JsonSerializer.Serialize(_selectedExtensionsEnum);
            }

            if (_currentOperationalMode == INSERTION_MODE)
            {
                await HandleSubQuestionDetailsInsertionAsync(comprehensionSubQuestionDto);
            }
            else
            {
                await HandleSubQuestionDetailsUpdateAsync(comprehensionSubQuestionDto);
            }
        }

        private async Task HandleSubQuestionDetailsInsertionAsync(ComprehensionSubQuestionDto comprehensionSubQuestionDto)
        {
            ApiResponse result;

            if (SubQuestionMetadataRetrievalDto.QuestionTypeId == 0) // Primitive Case: This means this is the first sub-question to be inserted of the selected language.
            {
                result = await BlazQuestionService.AddComprehensionSubQuestionAsync(comprehensionSubQuestionDto);
            }
            else
            {
                // Warning: Messing with this line can result in corrupted data in the database.
                comprehensionSubQuestionDto.SubQuestionDetailsDto.QuestionMetadataId = SubQuestionMetadataRetrievalDto.Id;

                result = await BlazQuestionService.AddComprehensionSubQuestionLanguageVariantAsync(comprehensionSubQuestionDto);
            }

            if (result.StatusCode == HttpStatusCode.OK)
            {
                MudDialog.Close(DialogResult.Ok(true));

                SubQuestionMetadataRetrievalDto = new(); // Reset this parameter to its default value

                Snackbar.Add(result.Message, Severity.Success);
            }
            else
            {
                Snackbar.Add(result.Message, Severity.Error);
            }
        }

        private async Task HandleSubQuestionDetailsUpdateAsync(ComprehensionSubQuestionDto subQuestionLanguageVariantDto)
        {
            var result = await BlazQuestionService.UpdateComprehensionSubQuestionLanguageVariantAsync(subQuestionLanguageVariantDto);

            if (result.StatusCode == HttpStatusCode.OK)
            {
                MudDialog.Close(DialogResult.Ok(true));

                Snackbar.Add(result.Message, Severity.Success);
            }
            else
            {
                Snackbar.Add(result.Message, Severity.Error);
            }
        }

        private void OnQuestionTypeChange(QuestionTypeDto questionTypeDto)
        {
            SelectedQuestionType = questionTypeDto;

            StateHasChanged();
        }

        private async Task<IEnumerable<QuestionTypeDto>> SearchQuestionTypeAsync(string value, CancellationToken cancellationToken)
        {
            await Task.Delay(250);

            if (string.IsNullOrEmpty(value))
                return QuestionsTypesExceptComprehension;

            return QuestionsTypesExceptComprehension.Where(item => item.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        private void CancelDialog() => MudDialog.Cancel();
    }
}
