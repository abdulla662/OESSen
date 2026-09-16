using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.Form.SuspendForm;
using OES.Blazor.Services.Implementation;
using OES.Blazor.Services.Interfaces.Form;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper.Dtos;
using OES.Helper.Dtos.Form;
using OES.Helper.Dtos.Sync;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Dialogs.Form
{
    public partial class PaperFormsDialog
    {
        [Inject] private IBlazFormService BlazFormService { get; set; } = default!;

        [Inject] private IBlazPaperService BlazPaperService { get; set; } = default!;

        [Inject] IDialogService DialogService { get; set; }

        [Inject] IBlazSessionStorageService SessoinStorageService { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] CRUD_Dto CrudDto { get; set; }

        [Inject] private SyncDashboardState SyncDashboardState { get; set; } = default!;

        [CascadingParameter] public MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public long PaperId { get; set; }

        [Parameter] public string PaperSubType { get; set; }

        [Parameter] public bool CanAddNewForm { get; set; }

        private int listItemReloadingKey;

        private bool _isPaperAssignedToSchedule;


        protected override async Task OnInitializedAsync()
        {
            _isPaperAssignedToSchedule = await BlazPaperService.IsPaperAssignedToScheduleAsync(PaperId);
        }

        public async Task<CustomTableData<FormListDto>> GetPaperFormsAsync(PaginationSearchModel PaginationSearchModel)
        {
            return await BlazFormService.GetPaginatedFormsByPaperIdAsync(PaginationSearchModel, PaperId);
        }

        private async Task AddNewPaperFormAsync(PaperStepperFormsMode? paperStepperFormsMode)
        {
            if (!CanAddNewForm)
            {
                Snackbar.Add(Resource.CannotAddNewFormBecauseThereIsPendingFormOrPaperNotCreatedYet, Severity.Error);
                return;
            }

            CrudDto.Id = PaperId;
            CrudDto.IsCreatePage = false;

            await SessoinStorageService.SetValue("IsCreatePage", false);
            await SessoinStorageService.SetValue("PerformEditBtnClick", PaperId.ToString());

            var parsedSuccessfully = Enum.TryParse<QuestionSelectionType>(PaperSubType, out var questionSelectionType);

            if (!parsedSuccessfully) return;

            switch (questionSelectionType)
            {
                case QuestionSelectionType.Manual:
                    await SessoinStorageService.SetValue(nameof(PaperStepperFormsMode), (int)paperStepperFormsMode);
                    break;
                case QuestionSelectionType.Auto:
                    await SessoinStorageService.SetValue(nameof(PaperStepperFormsMode), (int)paperStepperFormsMode);
                    break;
            }

            NavigationManager.NavigateTo("/CreateOrUpdatePaper");
        }

        private async Task OpenSuspendFormDialogAsync(FormListDto form)
        {
            var parameters = new DialogParameters
            {
                { nameof(SuspendFormDialog.FormId), form.Id }
            };

            var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true };
            var dialog = await DialogService.ShowAsync<SuspendFormDialog>(Resource.SuspendForm, parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                listItemReloadingKey++;
                StateHasChanged();

                if (result.Data != null)
                {
                    try
                    {
                        List<SyncJobStatusDto> initialJobs = null;

                        if (result.Data is List<SyncJobStatusDto> typedList)
                        {
                            initialJobs = typedList;
                        }
                        else if (result.Data is JsonElement jsonElement)
                        {
                            initialJobs = jsonElement.Deserialize<List<SyncJobStatusDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        }
                        else
                        {
                            var jsonStr = JsonSerializer.Serialize(result.Data);
                            initialJobs = JsonSerializer.Deserialize<List<SyncJobStatusDto>>(jsonStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        }

                        if (initialJobs?.Count > 0)
                        {
                            SyncDashboardState.StartSync(initialJobs);
                        }
                    }
                    catch (Exception ex)
                    {
                        Snackbar.Add(ex.Message, Severity.Error);
                    }
                }
            }
        }

        private async Task EditForm(FormListDto receivedPaperObject)
        {
            if (receivedPaperObject.IsSuspendedInAnyVenue || receivedPaperObject.FormStatus != AvailabilityStatus.Synced)
            {
                var parameters = new DialogParameters<EditFormDialog>
                {
                   { x => x.PaperId, PaperId },
                   { p => p.FormId, receivedPaperObject.Id }
                };

                var options = new DialogOptions
                {
                    CloseButton = true,
                    MaxWidth = MaxWidth.Large,
                    FullWidth = true
                };

                var dialog = await DialogService.ShowAsync<EditFormDialog>(Resource.EditForm, parameters, options);

                await dialog.Result;

                listItemReloadingKey++;
            }
            else
            {
                Snackbar.Add(Resource.CannotOperateOnFormBecauseItIsAlreadySynced, Severity.Error);
                return;
            }
        }

        private async Task EditSection(FormListDto receivedPaperObject)
        {
            if (receivedPaperObject.IsSuspendedInAnyVenue || receivedPaperObject.FormStatus != AvailabilityStatus.Synced)
            {
                var parameters = new DialogParameters<EditFormSectionsDialog>
                {
                   { p => p.FormId, receivedPaperObject.Id },
                   { p => p.PaperSubType, receivedPaperObject.PaperSubType }
                };

                var options = new DialogOptions
                {
                    CloseButton = true,
                    MaxWidth = MaxWidth.Medium,
                    FullWidth = true
                };

                var dialog = await DialogService.ShowAsync<EditFormSectionsDialog>(Resource.FormSectionsAndQuestions, parameters, options);

                await dialog.Result;
            }
            else
            {
                Snackbar.Add(Resource.CannotOperateOnFormBecauseItIsAlreadySynced, Severity.Error);
                return;
            }
        }

        private async Task SoftDeleteFormAsync(long formId)
        {
            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.Alert },
                { x => x.Content, Resource.FormDeletionWarningMessage },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Delete },
                { x => x.SubmitButtonColor, Color.Error },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.Delete }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await BlazFormService.SoftDeleteFormAsync(formId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    listItemReloadingKey++;
                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }

                StateHasChanged();
            }
        }

        public void Close() => MudDialog?.Close();
    }
}
