using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.Form;
using OES.Blazor.Dialogs.Paper;
using OES.Blazor.Dialogs.Paper.SuspendPaper;
using OES.Blazor.Extensions;
using OES.Blazor.Extentions.DialogHelpers;
using OES.Blazor.Services.Implementation;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.Form;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Paper.Requests;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.Sync;
using OES.Helper.Dtos.UserPapersDto;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.ResourceFiles;
using SharedHelper.Enums;
using SharedHelper.RolesNames;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Pages.Paper.Main.UserPapersList
{
    public partial class UserPapersList
    {
        [Inject] IDialogService DialogService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] IBlazPaperService BlazPaperService { get; set; }

        [Inject] IBlazFormService BlazFormService { get; set; }

        [Inject] GlobalUserContext GlobalUserContext { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }

        [Inject] IBlazAuthService AuthService { get; set; }

        [Inject] IBlazGroupService BlazGroupService { get; set; }

        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }

        [Inject] private SyncDashboardState SyncDashboardState { get; set; }

        private int listItemReloadingKey;

        private string _selectedPaperCreationStatus = string.Empty;

        private string _selectedPaperType = string.Empty;

        private string _selectedQuestionSelectionType = string.Empty;

        private PaperStatus _selectedStatusFilter = PaperStatus.None;

        private readonly List<string> _paperTypes = [.. Enum.GetNames<PaperType>()];

        private readonly List<string> _paperCreationStatuses = [.. Enum.GetNames<PaperCreationStatus>()];

        private readonly List<string> _questionSelectionTypes = [.. Enum.GetNames<QuestionSelectionType>()];

        private readonly List<PaperStatus> _completionStatuses = [PaperStatus.None, PaperStatus.Complete, PaperStatus.Incomplete];


        public PaperFilterPaginationModel PaperFilterPaginationModel { get; set; } = new PaperFilterPaginationModel();

        public string SelectedPaperType
        {
            get => _selectedPaperType;
            set
            {
                if (_selectedPaperType != value)
                {
                    _selectedPaperType = value;

                    if (!string.IsNullOrWhiteSpace(_selectedPaperType))
                    {
                        PaperFilterPaginationModel.SelectedPaperType = Enum.Parse<PaperType>(_selectedPaperType);
                    }
                    else
                    {
                        PaperFilterPaginationModel.SelectedPaperType = null;
                    }

                    if (_selectedPaperType != nameof(PaperType.Standard))
                    {
                        SelectedQuestionSelectionType = string.Empty;
                    }

                    listItemReloadingKey += 1;
                }
            }
        }

        private bool IsQuestionSelectionDisabled =>
            string.IsNullOrWhiteSpace(SelectedPaperType) ||
            SelectedPaperType != nameof(PaperType.Standard);

        public string SelectedPaperCreationStatus
        {
            get => _selectedPaperCreationStatus;
            set
            {
                if (_selectedPaperCreationStatus != value)
                {
                    _selectedPaperCreationStatus = value;

                    if (!string.IsNullOrWhiteSpace(_selectedPaperCreationStatus))
                    {
                        PaperFilterPaginationModel.SelectedPaperCreationStatus = Enum.Parse<PaperCreationStatus>(_selectedPaperCreationStatus);
                    }
                    else
                    {
                        PaperFilterPaginationModel.SelectedPaperCreationStatus = null;
                    }

                    listItemReloadingKey += 1;
                }
            }
        }

        public PaperStatus SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (_selectedStatusFilter != value)
                {
                    _selectedStatusFilter = value;

                    if (_selectedStatusFilter == PaperStatus.None)
                    {
                        PaperFilterPaginationModel.IsComplete = null;
                    }
                    else if (_selectedStatusFilter == PaperStatus.Complete)
                    {
                        PaperFilterPaginationModel.IsComplete = true;
                    }
                    else if (_selectedStatusFilter == PaperStatus.Incomplete)
                    {
                        PaperFilterPaginationModel.IsComplete = false;
                    }

                    listItemReloadingKey += 1;
                }
            }
        }

        public string SelectedQuestionSelectionType
        {
            get => _selectedQuestionSelectionType;
            set
            {
                if (_selectedQuestionSelectionType != value)
                {
                    _selectedQuestionSelectionType = value;

                    if (!string.IsNullOrWhiteSpace(_selectedQuestionSelectionType))
                    {
                        PaperFilterPaginationModel.QuestionSelectionType = Enum.Parse<QuestionSelectionType>(_selectedQuestionSelectionType);
                    }
                    else
                    {
                        PaperFilterPaginationModel.QuestionSelectionType = null;
                    }

                    listItemReloadingKey += 1;
                }
            }
        }

        protected override async Task OnInitializedAsync()
        {
            SelectedStatusFilter = PaperStatus.None;

            await Task.CompletedTask;
        }

        public async Task AddPaperAsync()
        {
            await BlazSessionStorageService.SetValue(nameof(PaperStepperFormsMode), (int)PaperStepperFormsMode.Default);
            NavigationManager.NavigateTo("/CreateOrUpdatePaper");
        }

        public async Task ContinuePendingPaperFormAsync(UserPapersListDto targetPaperDto)
        {
            var authorized = await AuthService.IsCurrentUserAuthorizedAsync(
                targetPaperDto.Id,
                OesTemplateRoleConstants.PaperEditor,
                BlazPaperService.GetPaperGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(
                targetPaperDto.Id,
                MiscConstants.PerformEditBtnClick);

            await BlazSessionStorageService.SetValue(
                nameof(PaperStepperFormsMode),
                (int)PaperStepperFormsMode.ContinuePendingForm);

            NavigationManager.NavigateTo("/CreateOrUpdatePaper");
        }

        public async Task ResetPaperFormsCounterAsync(UserPapersListDto targetPaperDto)
        {
            var authorized = await AuthService.IsCurrentUserAuthorizedAsync(
                targetPaperDto.Id,
                OesTemplateRoleConstants.PaperExamReset,
                BlazPaperService.GetPaperGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var response = await BlazFormService.ResetPaperFormsCounterAsync(targetPaperDto.Id);

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

        private async Task DeletePaperAsync(long id)
        {
            long paperId = long.Parse(id.ToString());

            var authorized = await IsCurrentUserAuthorizedAgainstSelectedPaperAsync(
                paperId,
                OesTemplateRoleConstants.PaperDeleter
            );

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToDeletePaper, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>
            {
                { p => p.Title, Resource.ConfirmDelete },
                { p => p.Content, Resource.PaperDeletionWarningMessage },
                { p => p.SubmitText, Resource.Delete },
                { p => p.CancelText, Resource.Cancel },
                { p => p.SubmitButtonColor, Color.Error },
                { p => p.SubmitButtonStartIcon, Icons.Material.Filled.Delete },
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
                var response = await BlazPaperService.DeletePaperAsync(id);

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

        public async Task CopyPaperDeeply(UserPapersListDto paper)
        {
            var paperId = long.TryParse(paper.Id.ToString(), out var parsedPaperId) ? parsedPaperId : 0;

            if (paperId > 0)
            {
                var response = await BlazPaperService.CopyPaperAsync(paperId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Snackbar.Add(response.Message, Severity.Success);

                    listItemReloadingKey++;
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }
            }
        }

        private async Task OpenCopyPaperDeeplyDialogAsync(UserPapersListDto userPaperList)
        {
            if (!await IsPaperHasCreatedStatusAsync(userPaperList.Id))
            {
                Snackbar.Add(Resource.CannotOperateOnPaperBecauseItIsNotInCreatedStatus, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.Alert },
                { x => x.Content, Resource.AreYouSureYouWantToCopyThisPaper },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Copy },
                { x => x.SubmitButtonColor, Color.Primary },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.ContentCopy }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Small
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await CopyPaperDeeply(userPaperList);
            }
        }

        private async Task ViewPaperFormsAsync(UserPapersListDto userPaperList)
        {
            var authorized = await AuthService.IsCurrentUserAuthorizedAsync(
                userPaperList.Id,
                OesTemplateRoleConstants.Paperviewer,
                BlazPaperService.GetPaperGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToViewPaper, Severity.Error);
                return;
            }

            if (userPaperList.Type == PaperType.Adaptive)
            {
                var parameters = new DialogParameters<ViewAdaptivePaperDialog>
                {
                    { x => x.PaperId, userPaperList.Id },
                    { x => x.PaperName, userPaperList.Name },
                    { x => x.PaperTypeDisplay, userPaperList.TypeDisplay },
                    { x => x.SubTypeDisplay, userPaperList.SubTypeDisplay },
                    { x => x.PaperType, userPaperList.Type },
                    { x => x.SubType, userPaperList.SubType }
                };

                var formsPaper = await BlazFormService.GetAllFormsWithTheirQuestionsByPaperIdAsync(userPaperList.Id);

                if (formsPaper == null)
                {
                    Snackbar.Add(Resource.FailedToLoadFormsDetails, Severity.Error);
                    return;
                }

                var options = new DialogOptions
                {
                    CloseButton = true,
                    MaxWidth = MaxWidth.Medium,
                    FullWidth = true
                };

                await DialogService.ShowAsync<ViewAdaptivePaperDialog>(
                    Resource.PaperStructureSummary,
                    parameters,
                    options);
            }
            else
            {
                var paper = await BlazFormService.GetAllFormsWithTheirQuestionsByPaperIdAsync(userPaperList.Id);

                if (paper == null)
                {
                    Snackbar.Add(Resource.FailedToLoadFormsDetails, Severity.Error);
                    return;
                }

                var parameters = new DialogParameters<ViewFormsPaperDialog>
                {
                    { x => x.Paper, paper }
                };

                var options = new DialogOptions
                {
                    CloseButton = true,
                    MaxWidth = MaxWidth.Medium,
                    FullWidth = true
                };

                var dialog = await DialogService.ShowAsync<ViewFormsPaperDialog>(
                    string.Empty,
                    parameters,
                    options);

                await dialog.Result;
            }
        }

        private async Task ViewPaginatedFormsAsync(UserPapersListDto userPaperList)
        {
            var authorized = await AuthService.IsCurrentUserAuthorizedAsync(
                userPaperList.Id,
                OesTemplateRoleConstants.PaperViewFormDetails,
                BlazPaperService.GetPaperGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToViewPaper, Severity.Error);
                return;
            }

            var isPaperHasCreatedStatus = await IsPaperHasCreatedStatusAsync(userPaperList.Id);

            var parameters = new DialogParameters<PaperFormsDialog>
            {
                { x => x.PaperId, userPaperList.Id },
                { x => x.PaperSubType, userPaperList.SubType },
                { x => x.CanAddNewForm, userPaperList.IsOutputFormsCountEqualsActualFormsCount && isPaperHasCreatedStatus }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            await DialogService.ShowAsync<PaperFormsDialog>(
                Resource.PaperFormList,
                parameters,
                options);
        }

        private async Task OpenPaperGroupDialogAsync(UserPapersListDto paper)
        {
            var allowed = await AuthService.IsCurrentUserOwnerAsync(
                paper.Id,
                BlazPaperService.GetPaperGroupsAsync,
                dto => [dto.OwnerGroupId ?? Guid.Empty]
            );

            if (!allowed)
            {
                Snackbar.Add(string.Format(Resource.OnlyCreatorCanManageGroups, Resource.Paper), Severity.Error);
                return;
            }

            var metadataResponse = await BlazPaperService.GetPaperMetaDataAsync(paper.Id);

            if (metadataResponse.StatusCode != HttpStatusCode.OK)
            {
                Snackbar.Add(metadataResponse.Message, Severity.Error);
                return;
            }

            var paperMetadata = (GetPaperMetadataResponseDto)metadataResponse.Data;

            var paperGroupsDto = await BlazPaperService.GetPaperGroupsAsync(paper.Id);
            var allGroups = await BlazPaperService.GetAllPaperGroupsCreatedByUser();

            var currentGroups = allGroups
                .Where(g => paperGroupsDto?.GroupsIds.Contains(g.Id) == true && !g.AutoCreatedForUser)
                .ToList();

            var oldGroupIds = currentGroups.Select(g => g.Id).ToHashSet();

            var selectedGroups = await DialogInteractionService.OpenSelectionDialogAsync(
                dialogService: DialogService,
                title: Resource.SelectGroup,
                preSelectedItems: currentGroups,
                endpointService: async pagination =>
                {
                    var filteredGroups = allGroups
                        .Concat(currentGroups)
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

            if (selectedGroups == null)
                return;

            var newGroups = selectedGroups
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .ToList();

            var newGroupIds = newGroups.Select(g => g.Id).ToHashSet();

            if (oldGroupIds.SetEquals(newGroupIds))
                return;

            var updateDto = new AddOrUpdatePaperMetadataRequestDto
            {
                Name = paperMetadata.Name,
                Code = paperMetadata.Code,
                Description = paperMetadata.Description,
                Abbreviation = paperMetadata.Abbreviation,
                SubjectsIds = paperMetadata.SubjectsIds,
                QuestionsCount = paperMetadata.QuestionsCount,
                Duration = paperMetadata.Duration,
                TotalMarks = (long)paperMetadata.TotalMarks,
                Type = paperMetadata.Type,
                QuestionSelectionType = paperMetadata.QuestionSelectionType,
                QuestionContentType = paperMetadata.QuestionContentType,
                OutputFormsCount = paperMetadata.OutputFormsCount,
                AllowInstantResult = paperMetadata.AllowInstantResult,
                LanguageId = paperMetadata.LanguageId,
                DifficultyProfileId = paperMetadata.DifficultyProfileId,
                TransitionProfileId = paperMetadata.TransitionProfileId,
                QuestionDistributionTypeInForm = paperMetadata.QuestionDistributionTypeInForm,
                StageCount = paperMetadata.StageCount,
                CategoryStagePaths = paperMetadata.CategoryStagePaths,
                CategoryFixedDPaths = paperMetadata.CategoryFixedDPaths,
                DPathCalculationMode = paperMetadata.DPathCalculationMode,
                AdaptiveCategoryExecutionOrder = paperMetadata.AdaptiveCategoryExecutionOrder,
                OESGroupDtos = newGroups
            };

            var originalStatus = paperMetadata.PaperCreationStatus;

            var updateResponse = await BlazPaperService.UpdatePaperMetadataAsync(paper.Id, updateDto);

            if (updateResponse.StatusCode == HttpStatusCode.OK)
            {
                if (originalStatus != PaperCreationStatus.MetadataAdded)
                {
                    await BlazPaperService.UpdatePaperCreationStatusAsync(new UpdatePaperCreationStatusRequestDto(paper.Id, originalStatus));
                }

                Snackbar.Add(Resource.Updated, Severity.Success);
                listItemReloadingKey++;
            }
            else
            {
                Snackbar.Add(updateResponse.Message, Severity.Error);
            }

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

        private static bool CanEditGroupsForPaper(UserPapersListDto paper)
        {
            return paper.PaperStatus != AvailabilityStatus.InComplete;
        }

        private async Task OpenSuspendPaperDialogAsync(UserPapersListDto userPaperList)
        {
            var authorized = await AuthService.IsCurrentUserAuthorizedAsync(
                userPaperList.Id,
                OesTemplateRoleConstants.PaperSuspensionManger,
                BlazPaperService.GetPaperGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<SuspendPaperDialog>
            {
                { x => x.PaperId, userPaperList.Id }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<SuspendPaperDialog>(
                Resource.ManageSuspensions,
                parameters,
                options
            );

            var dialogResult = await dialog.Result;

            if (!dialogResult.Canceled)
            {
                listItemReloadingKey++;
                StateHasChanged();

                if (dialogResult.Data != null)
                {
                    try
                    {
                        List<SyncJobStatusDto> initialJobs = null;

                        if (dialogResult.Data is List<SyncJobStatusDto> typedList)
                        {
                            initialJobs = typedList;
                        }
                        else if (dialogResult.Data is JsonElement jsonElement)
                        {
                            initialJobs = jsonElement.Deserialize<List<SyncJobStatusDto>>(
                                new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });
                        }
                        else
                        {
                            var jsonStr = JsonSerializer.Serialize(dialogResult.Data);

                            initialJobs = JsonSerializer.Deserialize<List<SyncJobStatusDto>>(
                                jsonStr,
                                new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });
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

        public async Task EditSuspendedPaperAsync(UserPapersListDto userPaperList)
        {
            if (userPaperList.PaperStatus == AvailabilityStatus.Synced)
            {
                Snackbar.Add(Resource.CannotOperateOnPaperBecauseItIsAlreadySynced, Severity.Error);
                return;
            }

            var authorized = await IsCurrentUserAuthorizedAgainstSelectedPaperAsync(
                userPaperList.Id,
                OesTemplateRoleConstants.PaperEditor
            );

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToEditPaper, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(userPaperList.Id, MiscConstants.PerformEditBtnClick);
            await BlazSessionStorageService.RemoveValue(nameof(PaperStepperFormsMode));
            NavigationManager.NavigateTo("/CreateOrUpdatePaper");
        }

        private Task<IEnumerable<string>> SearchPaperTypes(string searchText, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return Task.FromResult(_paperTypes.AsEnumerable());

            return Task.FromResult(_paperTypes.Where(x => x.ToLocalizedString<PaperType>().Contains(searchText, StringComparison.OrdinalIgnoreCase)));
        }

        private Task<IEnumerable<string>> SearchPaperCreationStatuses(string searchText, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return Task.FromResult(_paperCreationStatuses.AsEnumerable());

            return Task.FromResult(_paperCreationStatuses.Where(x => x.ToLocalizedString<PaperCreationStatus>().Contains(searchText, StringComparison.OrdinalIgnoreCase)));
        }

        private Task<IEnumerable<string>> SearchQuestionSelectionTypes(string searchText, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return Task.FromResult(_questionSelectionTypes.AsEnumerable());

            return Task.FromResult(_questionSelectionTypes.Where(x => x.ToLocalizedString<QuestionSelectionType>().Contains(searchText, StringComparison.OrdinalIgnoreCase)));
        }

        private Task<IEnumerable<PaperStatus>> SearchCompletionStatuses(string searchText, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return Task.FromResult(_completionStatuses.AsEnumerable());

            return Task.FromResult(_completionStatuses.Where(x => x.ToLocalizedString().Contains(searchText, StringComparison.OrdinalIgnoreCase)));
        }

        private async Task<bool> IsCurrentUserAuthorizedAgainstSelectedPaperAsync(
            long paperId,
            string targetRoleName
        )
        {
            // Super admin bypass
            if (GlobalUserContext.SsoUserRoles.Exists(x => x.Name == AdminRoles.SuperAdmin || x.Name == AdminRoles.Entity_Admin))
            {
                return true;
            }

            var paperGroupsDto = await BlazPaperService.GetPaperGroupsAsync(paperId);

            var userGroups = GlobalUserContext.OesUserGroupsAndRoles;

            // Compare
            var intersectedGroups = userGroups.IntersectBy(
                paperGroupsDto.GroupsIds,
                x => x.GroupId
            );

            var userRolesNames = intersectedGroups
                .SelectMany(x => x.GroupRoles)
                .Select(r => r.Name);

            return userRolesNames.Contains(targetRoleName);
        }

        private async Task<bool> IsPaperHasCreatedStatusAsync(long paperId)
        {
            var authorized = await IsCurrentUserAuthorizedAgainstSelectedPaperAsync(
                paperId,
                OesTemplateRoleConstants.Paperviewer
            );

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToViewPaper, Severity.Error);
                return false;
            }

            var apiResponse = await BlazPaperService.GetCurrentPaperCreationStatusAsync(paperId);

            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                var receivedDto = (GetCurrentPaperCreationStatusResponseDto)apiResponse.Data ?? new(PaperCreationStatus.MetadataAdded);

                return receivedDto.CurrentStatus == PaperCreationStatus.PaperCreated;
            }
            else
            {
                return false;
            }
        }

        private static bool CanContinuePaper(UserPapersListDto paper)
        {
            return paper.PaperCreationStatus != PaperCreationStatus.PaperCreated;
        }

        private static bool CanResetPaper(UserPapersListDto paper)
        {
            bool canResetPaper =
                paper.FormsStatusCounts != null &&
                paper.FormsStatusCounts.Count > 1 &&
                paper.FormsStatusCounts[^1].Status == AvailabilityStatus.InComplete;

            return canResetPaper;
        }
    }
}
