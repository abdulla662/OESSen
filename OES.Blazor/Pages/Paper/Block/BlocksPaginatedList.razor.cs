using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.Blocks;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.Block;
using OES.Blazor.Services.Interfaces.DifficultyLevel;
using OES.Blazor.Services.Interfaces.DifficultyProfile;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.QuestionCataegory;
using OES.Helper.Dtos.Block.Responses;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Paper.Block
{
    public partial class BlocksPaginatedList : ComponentBase
    {
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Inject] IBlazBlockService BlazBlockService { get; set; }
        [Inject] IBlazQuestionCategoryService QuestionCategoryService { get; set; }
        [Inject] IBlazDifficultyLevelService BlazDifficultyLevelService { get; set; }
        [Inject] IBlazDifficultyProfileService BlazProfileService { get; set; }
        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] IBlazAuthService AuthService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }


        private ProfileDto _selectedDifficultyProfileInput;
        private BlockFilterPaginationModel _blockFilterPaginationModel = new();
        private DifficultyLevelDto _selectedDifficultyLevelInput;
        private QuestionCategoryDto _selectedBlockType;
        private List<DifficultyLevelDto> _difficultyLevels = [];
        private List<QuestionCategoryDto> _blockTypes = [];
        private List<ProfileDto> _difficultyProfiles = [];
        private int listItemReloadingKey;
        private bool disabled = true;


        private List<DifficultyLevelDto> DifficultyLevels { get; set; }
        public DifficultyLevelDto SelectedDifficultyLevel
        {
            get => _selectedDifficultyLevelInput;
            set
            {
                if (_selectedDifficultyLevelInput != value)
                {
                    _selectedDifficultyLevelInput = value;
                    _blockFilterPaginationModel._SelectedDifficultyLevel = _selectedDifficultyLevelInput?.Id ?? 0;
                    listItemReloadingKey += 1;
                }
            }
        }
        public QuestionCategoryDto SelectedBlockType
        {
            get => _selectedBlockType;
            set
            {
                if (_selectedBlockType != value)
                {
                    _selectedBlockType = value;
                    _blockFilterPaginationModel._SelectedBlockType = _selectedBlockType?.Id ?? 0;
                    listItemReloadingKey += 1;
                }
            }
        }
        public ProfileDto SelectedDifficultyProfileInput
        {
            get => _selectedDifficultyProfileInput;
            set
            {
                if (_selectedDifficultyProfileInput != value)
                {
                    OnDifficultyProfileChanged(value);
                }
            }
        }


        protected override async void OnInitialized()
        {
            var profileTask = BlazProfileService.GetProfiles();
            var categoriesTask = QuestionCategoryService.GetCategories();

            await Task.WhenAll(profileTask, categoriesTask);

            _difficultyProfiles = await profileTask ?? [];
            _blockTypes = await categoriesTask ?? [];
        }

        private void ResetFilters()
        {
            if (SelectedDifficultyLevel is not null || SelectedBlockType is not null || SelectedDifficultyProfileInput is not null)
            {
                SelectedDifficultyLevel = null;
                SelectedDifficultyProfileInput = null;
                SelectedBlockType = null;
                listItemReloadingKey++;
            }
        }

        private async Task DeleteBlockAsync(long blockId)
        {
            var userAuthorized = await AuthService.IsCurrentUserAuthorizedAsync(
                blockId,
                OesTemplateRoleConstants.BlockDeleter,
                BlazBlockService.GetBlockGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!userAuthorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToDeleteBlock, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.BlockDeletionWarningMessage },
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
                var response = await BlazBlockService.DeleteBlockAsync(blockId);

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

        private async Task OnDifficultyProfileChanged(ProfileDto selectedDifficiltyProfile)
        {
            _selectedDifficultyProfileInput = selectedDifficiltyProfile;

            _blockFilterPaginationModel._SelectedProfile = _selectedDifficultyProfileInput?.Id ?? 0;

            listItemReloadingKey++;

            if (_selectedDifficultyProfileInput != null)
            {
                _difficultyLevels = await BlazDifficultyLevelService.GetDifficultyLevelByProfileIdAsync(_selectedDifficultyProfileInput.Id);

                DifficultyLevels = _difficultyLevels;

                disabled = false;

                if (_difficultyLevels == null || !_difficultyLevels.Any())
                {
                    disabled = true;
                    _selectedDifficultyLevelInput = null;
                }
            }
            else
            {
                _difficultyLevels = [];

                disabled = true;

                _selectedDifficultyLevelInput = null;
            }

            StateHasChanged();
        }

        private async Task<CustomTableData<GetBlockResponseDto>> GetBlocksPaginatedAsync(PaginationSearchModel paginationSearchModel)
        {
            return await BlazBlockService.GetAllBlockPaginatedAsync(null, paginationSearchModel);
        }

        public async Task EditBlockAsync(long blockId)
        {
            var userAuthorized = await AuthService.IsCurrentUserAuthorizedAsync(
                blockId,
                OesTemplateRoleConstants.BlockEditor,
                BlazBlockService.GetBlockGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!userAuthorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToUpdateBlock, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(blockId, MiscConstants.PerformEditBtnClick);
            NavigationManager.NavigateTo("/EditBlock");
        }

        private async Task ViewBlockAsync(long blockId)
        {
            var userAuthorized = await AuthService.IsCurrentUserAuthorizedAsync(
                blockId,
                OesTemplateRoleConstants.BlockViewer,
                BlazBlockService.GetBlockGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!userAuthorized)
            {
                Snackbar.Add(Resource.NotAuthorizedToViewBlock, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(blockId, MiscConstants.PerformViewBtnClick);

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseOnEscapeKey = true
            };

            await DialogService.ShowAsync<BlockDetailsViewDialog>(string.Empty, options);
        }
    }
}
