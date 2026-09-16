using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Dialogs.Question.QuestionLayoutShowingDialog;
using OES.Blazor.Dialogs.Question.ScreenSizeDialog;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Blazor.Services.Interfaces.QuestionLayout;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Dtos.QuestionLayout;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.QuestionLayout
{
    public partial class QuestionLayoutComponent : ComponentBase
    {
        [Inject] private IBlazQuestionService BlazQuestionService { get; set; }
        [Inject] private IBlazQuestionLayoutService BlazQuestionLayoutService { get; set; }
        [Inject] private IBlazQuestionLanguageService BlazQuestionLanguageService { get; set; }
        [Inject] private IBlazAuthService AuthService { get; set; }
        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        [Parameter] public string QuestionType { get; set; }
        [Parameter] public long QuestionMetadataId { get; set; }
        [Parameter] public long QuestionTypeId { get; set; }
        [Parameter] public long? QuestionLayoutId { get; set; }

        private List<LayoutDto> FilteredQuestionOptions { get; set; } = new();
        private List<LanguageDto> LanguagesList { get; set; } = new();
        private LanguageDto SelectedLanguage { get; set; } = new();
        private LayoutDto SelectedLayout { get; set; } = new();
        private Breakpoint SelectedScreenSize { get; set; }
        private string SelectScreenSizeClass { get; set; }
        private LayoutOrientation Orientation { get; set; } = LayoutOrientation.Horizontal;
        private QuestionDataDto Model { get; set; } = new();

        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;


        private async Task<IEnumerable<LanguageDto>> SearchLanguages(string value, CancellationToken cancellationToken)
        {
            await Task.Delay(250, cancellationToken);

            if (string.IsNullOrWhiteSpace(value))
                return LanguagesList;

            return LanguagesList.Where(x => x.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        public async Task LoadQuestionOptions()
        {
            LanguagesList = await BlazQuestionLanguageService.GetAllQuestionDetailsLanguagesGroupAsync(QuestionMetadataId);

            if (QuestionTypeId > 0)
            {
                FilteredQuestionOptions = await BlazQuestionLayoutService.GetAllLayoutsByQuestionTypeId(QuestionTypeId);

                if (QuestionType == nameof(Helper.Enums.QuestionType.Email))
                {
                    FilteredQuestionOptions = [.. FilteredQuestionOptions.Where(l => GetLayoutOrientation(l.Name) == LayoutOrientation.Vertical)];
                }

                if (QuestionLayoutId is not null && QuestionLayoutId > 0)
                {
                    SelectedLanguage = LanguagesList.FirstOrDefault() ?? new(); // In case of update, always choose the first language in the list.
                    SelectedLayout = FilteredQuestionOptions.Find(l => l.Id == QuestionLayoutId) ?? new();
                }

                StateHasChanged();
            }
            else
            {
                FilteredQuestionOptions.Clear();
            }
        }

        public bool ValidateStepThree()
        {
            if (SelectedLanguage.Id == 0)
            {
                Snackbar.Add(Resource.SelectAQuestionLanguageInOrderToEnableLayoutSelection, Severity.Error);
                return false;
            }
            else if (SelectedLayout.Id == 0)
            {
                Snackbar.Add(Resource.CannotProceedWithoutASelectedLayout, Severity.Error);
                return false;
            }

            return true;
        }

        private async Task OnLayoutChangeAsync(LayoutDto selectedLayout)
        {
            if (SelectedLanguage.Id == 0)
            {
                Snackbar.Add(Resource.SelectAQuestionLanguageInOrderToEnableLayoutSelection, Severity.Error);
                return;
            }

            SelectedLayout = selectedLayout;

            var response = await BlazQuestionLayoutService.AssignLayoutToQuestionMetadataAsync(QuestionMetadataId, SelectedLayout.Id);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        public static LayoutOrientation GetLayoutOrientation(string name)
        {
            if (string.IsNullOrEmpty(name)) return LayoutOrientation.Vertical;

            if (Enum.TryParse<LayoutOrientation>(name, true, out var orientation))
            {
                return orientation;
            }

            return LayoutOrientation.Vertical;
        }

        private async Task OpenScreenSizeDialogAsync()
        {
            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var result = await DialogService.Show<ScreenSizeDialog>(string.Empty, options).Result;

            if (!result.Canceled)
            {
                var response = await BlazQuestionService.GetQuestionByMetaDataIdAndLanguageId(QuestionMetadataId, SelectedLanguage.Id);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Snackbar.Add(Resource.SomethingWentWrongWhileFetchingTheQuestionTryAgain, Severity.Error);
                    return;
                }

                Model = (QuestionDataDto)response.Data;
                SelectedScreenSize = (Breakpoint)result.Data;
                SelectScreenSizeClass = SelectedScreenSize switch
                {
                    Breakpoint.Xs => "w-50",
                    Breakpoint.Sm => "w-75",
                    _ => "w-100"
                };

                Orientation = GetLayoutOrientation(SelectedLayout.Name);
                OpenQuestionLayoutShowingDialog();
            }
        }

        private void OpenQuestionLayoutShowingDialog()
        {
            var parameters = new DialogParameters<QuestionLayoutShowingDialog>
            {
                { p => p.Model, Model },
                { p => p.QuestionType, QuestionType },
                { p => p.Orientation, Orientation },
                { p => p.SelectedScreenSize, SelectedScreenSize },
                { p => p.SelectScreenSizeClass, SelectScreenSizeClass },
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            DialogService.Show<QuestionLayoutShowingDialog>(string.Empty, parameters, options);
        }

        private async Task LoadQuestionInMiddlePanelAsync()
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.QuestionExamViewer);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToViewQuestion, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetValue(
                "MiddlePanel_QuestionId",
                QuestionMetadataId);

            await JSRuntime.InvokeVoidAsync(
                "openInNewTab",
                "/StaticExam");
        }

        public void ResetForAnotherNewQuestion()
        {
            QuestionMetadataId = 0;

            QuestionLayoutId = 0;

            SelectedLanguage = new();

            SelectedLayout = new();
        }
    }
}