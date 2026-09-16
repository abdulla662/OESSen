using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.Question.QuestionViewDialog;
using OES.Blazor.Extensions;
using OES.Blazor.Extentions.DialogHelpers;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.QuestionCataegory;
using OES.Blazor.Services.Interfaces.QuestionMetaData;
using OES.Blazor.Services.Interfaces.QuestionType;
using OES.Helper;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Question
{
    public partial class QuestionList : ComponentBase
    {
        [Inject] IBlazQuestionCategoryService BlazQuestionCategoryService { get; set; }
        [Inject] IBLazQuestionType BLazQuestionType { get; set; }
        [Inject] IBlazItemBankService BlazItemBankService { get; set; }
        [Inject] IBlazGroupService BlazGroupService { get; set; }
        [Inject] IBlazQuestionService BlazQuestionService { get; set; }
        [Inject] IBlazQuestionMetaData BlazQuestionMetaData { get; set; }
        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] IBlazAuthService AuthService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Inject] GlobalUserContext GlobalUserContext { get; set; }


        private List<QuestionCategoryDto> _categories = [];
        private List<QuestionTypeDto> _questionTypes = [];
        private List<RootItemBankDto> _itemBanks = [];
        private List<string> _statuses = [];
        private QuestionCategoryDto _selectedCategory;
        private QuestionTypeDto _selectedType;
        private RootItemBankDto _selectedItemBank;
        private string _selectedStatus = string.Empty;
        private int _questionsListChangingKey;
        private List<TreeItemResponseDto> _branches = [];
        private TreeItemResponseDto _selectedBranch;

        public QuestionFilterPaginationModel QuestionFilterPaginationModel { get; set; } = new QuestionFilterPaginationModel();

        public RootItemBankDto SelectedItemBank
        {
            get => _selectedItemBank;
            set
            {
                if (_selectedItemBank != value)
                {
                    _selectedItemBank = value;
                    QuestionFilterPaginationModel._selectedItemBankSignature = _selectedItemBank?.ItemBankSignature;

                    _selectedBranch = null;
                    _branches = [];
                    QuestionFilterPaginationModel._selectedBranchId = 0;

                    if (_selectedItemBank != null)
                        _ = LoadBranchesAsync(_selectedItemBank.Id, _selectedItemBank.ItemBankSignature);

                    _questionsListChangingKey += 1;
                }
            }
        }

        private async Task OpenGroupDialogAsync(QuestionMetadataPaginationDto question)
        {
            var allowed = await AuthService.IsCurrentUserOwnerAsync(
                question.Id,
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

            var questionGroupsDto = await BlazQuestionService.GetQuestionGroupsAsync(question.Id);
            var allGroups = await BlazQuestionService.GetAllQuestionGroupsAsync();

            var currentGroups = allGroups
                .Where(g => questionGroupsDto?.GroupsIds.Contains(g.Id) == true && !g.AutoCreatedForUser)
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

            if (selectedGroups == null)
                return;

            var newGroups = selectedGroups
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .ToList();

            var newGroupIds = newGroups.Select(g => g.Id).ToHashSet();

            if (oldGroupIds.SetEquals(newGroupIds))
                return;

            var questionMetadata = await BlazQuestionService.GetQuestionMetadataByIdAsync(question.Id);
            var metadata = questionMetadata?.Data as QuestionMetadataRetrievalDto;

            if (metadata is null)
            {
                Snackbar.Add(Resource.SomethingWentWrong, Severity.Error);
                return;
            }

            var updateDto = new QuestionMetadataAdditionOrUpdateDto
            {
                Id = question.Id,
                Code = metadata.Code,
                QuestionTypeId = metadata.QuestionTypeId,
                QuestionSubjectId = (long)metadata.SubjectId,
                QuestionCategoryId = metadata.QuestionCategoryId,
                DifficultyProfileId = metadata.DifficultyProfileId,
                DifficultyLevelId = metadata.DifficultyLevelId,
                Delta = metadata.Delta,
                IsRoot = metadata.IsRoot,
                MaximumAnswerTime = metadata.MaximumAnswerTime,
                Author = metadata.Author,
                IloId = metadata.ChildIloDto?.Id == 0 ? null : metadata.ChildIloDto?.Id,
                ItemBankId = metadata.ChildItemBankDto.Id,
                QuestionLayoutId = metadata.QuestionLayoutId,
                QuestionsExhaustionCount = metadata.QuestionsExhaustionCount,
                ScientificEditorPanelEnabled = metadata.ScientificEditorPanelEnabled,
                FileManagerEditorPanelEnabled = metadata.FileManagerEditorPanelEnabled,
                OESGroupDtos = newGroups
            };

            var response = await BlazQuestionService.EditQuestionMetaData(updateDto);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(Resource.Updated, Severity.Success);
                _questionsListChangingKey++;
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
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

        public TreeItemResponseDto SelectedBranch
        {
            get => _selectedBranch;
            set
            {
                if (_selectedBranch != value)
                {
                    _selectedBranch = value;
                    QuestionFilterPaginationModel._selectedBranchId = _selectedBranch?.Id ?? 0;
                    _questionsListChangingKey += 1;
                }
            }
        }

        public QuestionCategoryDto SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    QuestionFilterPaginationModel._selectedCategory = _selectedCategory?.Id ?? 0;
                    _questionsListChangingKey += 1;
                }
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
                    _questionsListChangingKey += 1;
                }
            }
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (_selectedStatus != value)
                {
                    _selectedStatus = value;

                    if (!string.IsNullOrEmpty(_selectedStatus))
                    {
                        QuestionFilterPaginationModel._selectedStatus = Enum.Parse<QuestionStatus>(_selectedStatus);
                    }
                    else
                    {
                        QuestionFilterPaginationModel._selectedStatus = new QuestionStatus();
                    }

                    _questionsListChangingKey += 1;
                }
            }
        }


        protected override async Task OnInitializedAsync()
        {
            var categoriesTask = BlazQuestionCategoryService.GetCategories();
            var questionTypesTask = BLazQuestionType.GetAllQuestionType();
            var itemBanksTask = BlazItemBankService.GetItemBanksListAsync();

            await Task.WhenAll(categoriesTask, questionTypesTask, itemBanksTask);

            _categories = categoriesTask.Result;
            _questionTypes = questionTypesTask.Result;
            _itemBanks = itemBanksTask.Result;
            _statuses = [.. Enum
                .GetNames<QuestionStatus>()
                .Where(s => s is not nameof(QuestionStatus.Suspended)
                              and not nameof(QuestionStatus.Closed)
                              and not nameof(QuestionStatus.InExam))
            ];
        }

        private static bool CanEditQuestion(object obj)
        {
            var question = (QuestionMetadataPaginationDto)obj;

            var status = Enum.TryParse(typeof(QuestionStatus), question.Status.ToString(), out var parsedStatus)
                ? (QuestionStatus)parsedStatus
                : QuestionStatus.MetadataAdded;

            return status == QuestionStatus.MetadataAdded ||
                   status == QuestionStatus.QuestionDetailsAdded ||
                   status == QuestionStatus.LayoutSelectedAndPending ||
                   status == QuestionStatus.ReturnToEdit;
        }

        private async Task OpenCopyQuestionDeeplyDialogAsync(object receivedQuestionObject)
        {
            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.Alert },
                { x => x.Content, Resource.CopyConfirmation },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.CopyIt },
                { x => x.SubmitButtonColor, Color.Primary },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.ContentCopy }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Small
            };

            var dialog = DialogService.Show<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await CopyQuestionDeeply(receivedQuestionObject);
            }
        }

        private async Task CopyQuestionDeeply(object receivedQuestionObject)
        {
            var question = (QuestionMetadataPaginationDto)receivedQuestionObject;

            var questionStatus = Enum.TryParse(
                typeof(QuestionStatus),
                question.Status.ToString(),
                out var parsedStatus)
                    ? (QuestionStatus)parsedStatus
                    : QuestionStatus.MetadataAdded;

            if (questionStatus == QuestionStatus.MetadataAdded ||
                questionStatus == QuestionStatus.QuestionDetailsAdded)
            {
                Snackbar.Add(Resource.CannotCopyQuestion, Severity.Error);
                return;
            }

            var authorized = await AuthService.IsCurrentUserAuthorizedForAnyRoleAsync(
                question.Id,
                [
                    OesTemplateRoleConstants.QuestionEditor,
                    OesTemplateRoleConstants.ItemBankQuestionEditor
                ],
                BlazQuestionService.GetQuestionGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!authorized)
            {
                var questionMetadata = await BlazQuestionService.GetQuestionMetadataByIdAsync(question.Id);

                var metadata = questionMetadata?.Data as QuestionMetadataRetrievalDto;

                if (metadata?.ChildItemBankDto?.Id > 0)
                {
                    var itemBankAuth = await BlazItemBankService.CanDoQuestionActionAsync(
                            metadata.ChildItemBankDto.Id,
                            OesTemplateRoleConstants.ItemBankQuestionEditor
                        );

                    authorized = itemBankAuth?.CustomCodeStatus == CustomCodeStatus.Success;
                }
            }

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToUpdateQuestion, Severity.Error);
                return;
            }

            var questionMetadataId =
                long.TryParse(question.Id.ToString(), out var parsedQuestionMetadataId)
                    ? parsedQuestionMetadataId
                    : 0;

            if (questionMetadataId > 0)
            {
                var res = await BlazQuestionMetaData.CopyOldQuestionDeeplyAsync(questionMetadataId);

                if (res.CustomCodeStatus == CustomCodeStatus.Success)
                {
                    Snackbar.Add(
                        Resource.TheQuestionwasCopiedSuccessfully,
                        Severity.Success);
                }
                else
                {
                    Snackbar.Add(
                        Resource.FailedtoCopyTheQuestionPleaseTryAgain,
                        Severity.Error);
                }

                _questionsListChangingKey++;
            }
        }
        private async Task EditQuestionAsync(QuestionMetadataPaginationDto question)
        {
            bool authorized;
            bool isReplace = question.Status == QuestionStatus.Approved;

            if (isReplace)
            {
                authorized = await AuthService.IsCurrentUserAuthorizedForAnyRoleAsync(
                    question.Id,
                    [
                        OesTemplateRoleConstants.QuestionReplacer,
                        OesTemplateRoleConstants.ItemBankQuestionReplacer
                    ],
                    BlazQuestionService.GetQuestionGroupsAsync,
                    dto => dto.GroupsIds,
                    g => g.GroupId
                );

                if (!authorized)
                {
                    var questionMetadata = await BlazQuestionService.GetQuestionMetadataByIdAsync(question.Id);
                    var metadata = questionMetadata?.Data as QuestionMetadataRetrievalDto;

                    if (metadata?.ChildItemBankDto?.Id > 0)
                    {
                        var itemBankAuth = await BlazItemBankService.CanDoQuestionActionAsync(
                            metadata.ChildItemBankDto.Id,
                            OesTemplateRoleConstants.ItemBankQuestionReplacer
                        );

                        authorized = itemBankAuth?.CustomCodeStatus == CustomCodeStatus.Success;
                    }
                }

                if (!authorized)
                {
                    Snackbar.Add(Resource.NotAuthorizedToReplaceQuestion, Severity.Error);
                    return;
                }
            }
            else
            {
                authorized = await AuthService.IsCurrentUserAuthorizedForAnyRoleAsync(
                    question.Id,
                    [
                        OesTemplateRoleConstants.QuestionEditor,
                        OesTemplateRoleConstants.ItemBankQuestionEditor
                    ],
                    BlazQuestionService.GetQuestionGroupsAsync,
                    dto => dto.GroupsIds,
                    g => g.GroupId
                );

                if (!authorized)
                {
                    var questionMetadata = await BlazQuestionService.GetQuestionMetadataByIdAsync(question.Id);

                    var metadata = questionMetadata?.Data as QuestionMetadataRetrievalDto;

                    if (metadata?.ChildItemBankDto?.Id > 0)
                    {
                        var itemBankAuth = await BlazItemBankService.CanDoQuestionActionAsync(
                            metadata.ChildItemBankDto.Id,
                            OesTemplateRoleConstants.ItemBankQuestionEditor
                        );

                        authorized = itemBankAuth?.CustomCodeStatus == CustomCodeStatus.Success;
                    }
                }

                if (!authorized)
                {
                    Snackbar.Add(Resource.NotAuthorizedToUpdateQuestion, Severity.Error);
                    return;
                }
            }

            await BlazSessionStorageService.RemoveValue(
                $"{MiscConstants.IsApprovedQuestion}_{question.Id}");

            await BlazSessionStorageService.SetCrudSessionAsync(
                question.Id,
                MiscConstants.PerformEditBtnClick);

            await BlazSessionStorageService.SetValue(
                nameof(QuestionStatus),
                (int)question.Status);

            NavigationManager.NavigateTo("/QuestionCreation");
        }

        private async Task DeleteQuestionAsync(long questionId)
        {
            var authorized = await AuthService.IsCurrentUserAuthorizedForAnyRoleAsync(
                questionId,
                [
                    OesTemplateRoleConstants.QuestionDeleter,
                    OesTemplateRoleConstants.ItemBankQuestionDeleter
                ],
                BlazQuestionService.GetQuestionGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!authorized)
            {
                var questionMetadata = await BlazQuestionService.GetQuestionMetadataByIdAsync(questionId);

                var metadata = questionMetadata?.Data as QuestionMetadataRetrievalDto;

                if (metadata?.ChildItemBankDto?.Id > 0)
                {
                    var itemBankAuth = await BlazItemBankService.CanDoQuestionActionAsync(
                        metadata.ChildItemBankDto.Id,
                        OesTemplateRoleConstants.ItemBankQuestionDeleter
                    );

                    authorized = itemBankAuth?.CustomCodeStatus == CustomCodeStatus.Success;
                }
            }

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToDeleteQuestion, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>
            {
                { p => p.Title, Resource.ConfirmDelete },
                { p => p.Content, Resource.AreYouSureYouWantToDeleteThisQuestion },
                { p => p.SubmitText, Resource.Delete },
                { p => p.CancelText, Resource.Cancel },
                { p => p.SubmitButtonColor, Color.Error },
                { p => p.SubmitButtonStartIcon, Icons.Material.Filled.Delete }
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
                var response = await BlazQuestionService.DeleteQuestionMetadataByIdAsync(questionId);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        Snackbar.Add(Resource.QuestionHasBeenDeletedSuccessfully, Severity.Success);
                        _questionsListChangingKey++;
                        StateHasChanged();
                        break;

                    case HttpStatusCode.Conflict:
                        Snackbar.Add(response.Message, Severity.Warning);
                        break;

                    default:
                        Snackbar.Add(Resource.SomethingWentWrongwhileDeletingQuestionDtails, Severity.Error);
                        break;
                }
            }
        }

        private async Task ViewQuestionAsync(long questionId)
        {
            if (questionId <= 0)
            {
                Snackbar.Add(Resource.FailedToLoadQuestionDetails, Severity.Error);
                return;
            }

            var authorized = await AuthService.IsCurrentUserAuthorizedForAnyRoleAsync(
                questionId,
                [
                    OesTemplateRoleConstants.Questionviewer,
                    OesTemplateRoleConstants.ItemBankQuestionViewer
                ],
                BlazQuestionService.GetQuestionGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!authorized)
            {
                var questionMetadata = await BlazQuestionService.GetQuestionMetadataByIdAsync(questionId);

                var metadata = questionMetadata?.Data as QuestionMetadataRetrievalDto;

                if (metadata?.ChildItemBankDto?.Id > 0)
                {
                    var itemBankAuth = await BlazItemBankService.CanDoQuestionActionAsync(
                        metadata.ChildItemBankDto.Id,
                        OesTemplateRoleConstants.ItemBankQuestionViewer
                    );

                    authorized = itemBankAuth?.CustomCodeStatus == CustomCodeStatus.Success;
                }
            }

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToViewQuestion, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetValue(MiscConstants.PerformViewBtnClick, questionId);

            var parameters = new DialogParameters<QuestionViewDialog>();

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<QuestionViewDialog>(string.Empty, parameters, options);

            await dialog.Result;
        }

        private async Task LoadQuestionInMiddlePanelAsync(object obj)
        {
            var value = (QuestionMetadataPaginationDto)obj;

            var authorized = await AuthService.IsCurrentUserAuthorizedForAnyRoleAsync(
                value.Id,
                [
                    OesTemplateRoleConstants.QuestionExamViewer,
                ],
                BlazQuestionService.GetQuestionGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!authorized)
            {
                var questionMetadata = await BlazQuestionService.GetQuestionMetadataByIdAsync(value.Id);

                var metadata = questionMetadata?.Data as QuestionMetadataRetrievalDto;

                if (metadata?.ChildItemBankDto?.Id > 0)
                {
                    var itemBankAuth = await BlazItemBankService.CanDoQuestionActionAsync(
                        metadata.ChildItemBankDto.Id,
                        OesTemplateRoleConstants.QuestionExamViewer
                    );

                    authorized = itemBankAuth?.CustomCodeStatus == CustomCodeStatus.Success;
                }
            }

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToViewQuestion, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetValue("MiddlePanel_QuestionId", value.Id);

            await JSRuntime.InvokeVoidAsync("openInNewTab", "/StaticExam");
        }

        private Task<IEnumerable<RootItemBankDto>> SearchItemBanks(string searchText, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return Task.FromResult(_itemBanks.AsEnumerable());

            return Task.FromResult(_itemBanks.Where(x => x.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)));
        }

        private Task<IEnumerable<QuestionCategoryDto>> SearchCategories(string searchText, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return Task.FromResult(_categories.AsEnumerable());

            return Task.FromResult(_categories.Where(x => x.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)));
        }

        private Task<IEnumerable<QuestionTypeDto>> SearchTypes(string searchText, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return Task.FromResult(_questionTypes.AsEnumerable());

            return Task.FromResult(_questionTypes.Where(x => x.Name.ToLocalizedString<OES.Helper.Enums.QuestionType>().Contains(searchText, StringComparison.OrdinalIgnoreCase)));
        }

        private Task<IEnumerable<string>> SearchStatuses(string searchText, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return Task.FromResult(_statuses.AsEnumerable());

            return Task.FromResult(_statuses.Where(x => x.ToLocalizedString<QuestionStatus>().Contains(searchText, StringComparison.OrdinalIgnoreCase)));
        }

        private async Task LoadBranchesAsync(long itemBankId, string signature)
        {
            _branches = await BlazItemBankService.GetChildrenByParentIdAsync(itemBankId, signature);
            await InvokeAsync(StateHasChanged);
        }

        private Task<IEnumerable<TreeItemResponseDto>> SearchBranches(string searchText, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return Task.FromResult(_branches.AsEnumerable());

            return Task.FromResult(
                _branches.Where(x => x.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            );
        }
    }
}
