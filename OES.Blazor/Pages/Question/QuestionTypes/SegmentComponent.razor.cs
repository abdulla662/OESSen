using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.TextEditor;
using OES.Blazor.Dialogs.FileManager;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Helper.Dtos.Folder.Response;
using OES.Helper.Dtos.Question.SegmentQuestionDtos;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using SharedHelper.Enums;
using SharedHelper.General;

namespace OES.Blazor.Pages.Question.QuestionTypes
{
    public partial class SegmentComponent
    {
        [Inject] private IBlazQuestionLanguageService BlazQuestionLanguage { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IDialogService DialogService { get; set; }

        [Parameter] public LanguageDto PreSelectedLanguage { get; set; }
        [Parameter] public EventCallback<SegmentQuestionDto> FormHandler { get; set; }
        [Parameter] public EventCallback DialogCancellationCallback { get; set; }
        [Parameter] public SegmentQuestionDto Model { get; set; } = new();
        [Parameter] public bool IsReplaceMode { get; set; }

        public TextEditorParams BodyEditorParams { get; set; }
        public TextEditorParams ModelAnswerEditorParams { get; set; }
        public TextEditorParams InstructionsEditorParams { get; set; }

        private bool _processing = false;
        private bool _isSubmitButtonHit = false;
        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;

        protected override void OnInitialized()
        {
            InitializeTextEditors();

            if (PreSelectedLanguage?.Id > 0)
            {
                Model.SegmentQuestionDetailsDto.LanguageId = PreSelectedLanguage.Id;
            }
        }

        private void OnResponseTypeChanged(SegmentQuestionResponseType newValue)
        {
            Model.SegmentQuestionConfigDto.SegmentQuestionResponseType = newValue;
            PrepareModelAnswerEditor();
            StateHasChanged();
        }

        private async Task UploadDocumentAsync()
        {
            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = true
            };

            var parameters = new DialogParameters<FileManagerDialog>
            {
                {
                    x => x.OnFileSelected,
                    EventCallback.Factory.Create<dynamic>(this, (file) => ReceiveSelectedFileAsync(file))
                }
            };

            await DialogService.ShowAsync<FileManagerDialog>(string.Empty, parameters, options);
        }

        private void RemoveAttachedFile()
        {
            Model.SegmentQuestionConfigDto.SegmentAudioUrl = string.Empty;
            Model.SegmentQuestionDetailsDto.MediaFileName = string.Empty;
            StateHasChanged();
        }

        public async Task OnSubmitAsync()
        {
            _processing = true;

            string bodyContent = await BodyEditorParams.GetTextEditorContentAsync();

            if (string.IsNullOrWhiteSpace(bodyContent) && string.IsNullOrWhiteSpace(Model.SegmentQuestionConfigDto.SegmentAudioUrl))
            {
                Snackbar.Add(Resource.QuestionContentRequired, Severity.Error);
                BodyEditorParams.ErrorShown = true;
                _processing = false;
                return;
            }

            _isSubmitButtonHit = true;

            var isFormValid = await ValidateFormAsync();

            if (!isFormValid)
            {
                Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);
                _processing = false;
                return;
            }

            Model.SegmentQuestionDetailsDto.Body = await BodyEditorParams.GetTextEditorContentAsync();

            if (Model.SegmentQuestionConfigDto.SegmentQuestionResponseType != SegmentQuestionResponseType.None)
            {
                if (Model.SegmentQuestionConfigDto.SegmentQuestionResponseType == SegmentQuestionResponseType.Essay)
                {
                    Model.SegmentQuestionDetailsDto.ModelAnswer =
                        await ModelAnswerEditorParams.GetTextEditorContentAsync();

                    Model.SegmentQuestionDetailsDto.Instructions =
                        await InstructionsEditorParams.GetTextEditorContentAsync();
                }

                Model.SegmentQuestionConfigDto.HasScore = true;
            }

            await FormHandler.InvokeAsync(Model);

            _processing = false;
        }

        private async Task CancelDialogAsync()
        {
            await DialogCancellationCallback.InvokeAsync();
        }

        #region Helper Method
        private void InitializeTextEditors()
        {
            BodyEditorParams = new()
            {
                Label = Resource.QuestionBody,
                InitialContent = Model.SegmentQuestionDetailsDto.Body,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                ErrorText = _requiredErrorText,
                RequiredAsteriskShown = true,
                IsReadOnly = false
            };

            InstructionsEditorParams = new()
            {
                Label = Resource.QuestionInstructionsOptional,
                InitialContent = Model.SegmentQuestionDetailsDto.Instructions,
                WithMathChemPanel = true,
                WithFileManagerPanel = true
            };

            PrepareModelAnswerEditor();

            StateHasChanged();
        }

        private void PrepareModelAnswerEditor()
        {
            ModelAnswerEditorParams = new()
            {
                Label = Resource.QuestionGuideAnswer,
                InitialContent = Model.SegmentQuestionDetailsDto.ModelAnswer,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                ErrorText = _requiredErrorText,
                RequiredAsteriskShown = Model.SegmentQuestionConfigDto.SegmentQuestionResponseType == SegmentQuestionResponseType.Essay,
                IsReadOnly = false
            };

            StateHasChanged();
        }

        private async Task ReceiveSelectedFileAsync(FolderDocumentListResponseDto insertedFile)
        {
            if (insertedFile.IsFolder || string.IsNullOrEmpty(insertedFile.Type) || !insertedFile.Type.StartsWith(MiscConstants.AudioTypePrefix))
            {
                Snackbar.Add(Resource.PleaseSelectAudioFile, Severity.Error);
                return;
            }

            Model.SegmentQuestionDetailsDto.MediaFileName = insertedFile.Id.ToString();
            Model.SegmentQuestionConfigDto.SegmentAudioUrl = $"{CentralizedUrlHelper.DocLibApiBaseUrl}/api/Document/DownloadStream?documentId={insertedFile.Id}";
        }

        private static string GetTruncatedAudioFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || fileName.Length <= 30)
                return fileName;

            return $"{fileName[..27]}...";
        }

        private async Task<bool> ValidateFormAsync()
        {
            bool isResponseTimeInvalid = false;
            bool isWordCountInvalid = false;
            bool isModelAnswerInvalid = false;

            if (Model.SegmentQuestionConfigDto.SegmentQuestionResponseType != SegmentQuestionResponseType.None)
            {
                isResponseTimeInvalid = Model.SegmentQuestionConfigDto?.ResponseTime.GetValueOrDefault() <= 0;

                if (Model.SegmentQuestionConfigDto.SegmentQuestionResponseType == SegmentQuestionResponseType.Essay)
                {
                    isWordCountInvalid = Model.SegmentQuestionConfigDto?.WordsCount <= 0;

                    string modelAnswerContent = await ModelAnswerEditorParams.GetTextEditorContentAsync();
                    ModelAnswerEditorParams.ErrorShown = string.IsNullOrWhiteSpace(modelAnswerContent);
                    isModelAnswerInvalid = ModelAnswerEditorParams.ErrorShown;
                }
            }

            bool isFormInvalid = isResponseTimeInvalid ||
                                 isWordCountInvalid ||
                                 isModelAnswerInvalid;

            StateHasChanged();

            return !isFormInvalid;
        }
        #endregion
    }
}