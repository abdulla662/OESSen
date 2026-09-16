using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.MainComponent;
using OES.Blazor.Services.Interfaces.Block;
using OES.Blazor.Services.Interfaces.DifficultyLevel;
using OES.Blazor.Services.Interfaces.DifficultyProfile;
using OES.Blazor.Services.Interfaces.QuestionCataegory;
using OES.Helper.Dtos.Block.Requests;
using OES.Helper.Dtos.Block.Responses;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using SharedHelper.Enums;
using System.Net;

namespace OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.SecondStep.BlocksSelection
{
    public partial class BlocksSelection : ComponentBase
    {
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] IBlazBlockService BlazBlockService { get; set; }
        [Inject] IBlazDifficultyLevelService BlazDifficultyLevelService { get; set; }
        [Inject] IBlazDifficultyProfileService BlazProfileService { get; set; }
        [Inject] private IBlazQuestionCategoryService BlazQuestionCategoryService { get; set; }
        [Inject] IPaginationSearchModel PaginationSearchModel { get; set; }

        [Parameter] public PaperMetadataResultedParamsDto PaperMetadataResultedParamsDto { get; set; } = new();

        private List<DifficultyLevelDto> _difficultyLevels = [];
        private List<ProfileDto> _difficultyProfiles = [];
        private List<QuestionCategoryDto> _questionCategories = [];
        private BlockFilterPaginationModel _blockFilterPaginationModel = new();
        private DifficultyLevelDto _selectedDifficultyLevelInput;
        private ProfileDto _selectedDifficultyProfileInput;
        private QuestionCategoryDto _selectedCategoryInput;
        private long _blockListChangeKey;
        private bool disabled = true;
        private ListItem<GetBlockResponseDto> _listItemRef;

        private bool IsAdaptiveMST => PaperMetadataResultedParamsDto.SelectedPaperType == PaperType.Adaptive && PaperMetadataResultedParamsDto.AdaptiveSubtype == AdaptivePaperSubtype.MST;
        private bool IsAdaptiveStep => PaperMetadataResultedParamsDto.SelectedPaperType == PaperType.Adaptive && (PaperMetadataResultedParamsDto.AdaptiveSubtype == AdaptivePaperSubtype.STEP);
        private List<DifficultyLevelDto> DifficultyLevels { get; set; }
        private List<GetBlockResponseDto> SelectedBlocks { get; set; } = [];
        public DifficultyLevelDto SelectedDifficultyLevel
        {
            get => _selectedDifficultyLevelInput;
            set
            {
                if (_selectedDifficultyLevelInput != value)
                {
                    _selectedDifficultyLevelInput = value;
                    _blockFilterPaginationModel._SelectedDifficultyLevel = _selectedDifficultyLevelInput?.Id ?? 0;
                    _blockListChangeKey += 1;
                }
            }
        }
        public QuestionCategoryDto SelectedCategoryInput
        {
            get => _selectedCategoryInput;
            set
            {
                if (_selectedCategoryInput != value)
                {
                    _selectedCategoryInput = value;
                    _blockFilterPaginationModel._SelectedBlockType = _selectedCategoryInput?.Id ?? 0;
                    _blockListChangeKey++;
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


        // DATA PROCESSING METHODS

        protected override async void OnInitialized()
        {
            var profileTask = BlazProfileService.GetProfiles();
            var categoriesTask = BlazQuestionCategoryService.GetCategories();
            var paperSelectedBlocksTask = BlazBlockService.GetPaperBlocksByPaperIdAsync(PaperMetadataResultedParamsDto.PaperId);

            await Task.WhenAll(profileTask, categoriesTask, paperSelectedBlocksTask);

            _difficultyProfiles = await profileTask ?? [];
            _questionCategories = await categoriesTask ?? [];
            _blockFilterPaginationModel._SelectedLanguageId = PaperMetadataResultedParamsDto.LanguageId;

            var paperSelectedBlocks = await paperSelectedBlocksTask;

            if (paperSelectedBlocks.Count > 0)
            {
                SelectedBlocks = paperSelectedBlocks.ConvertAll(x =>
                    new GetBlockResponseDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        DeltaTypeName = x.DeltaTypeName,
                        DeltaTypeId = x.DeltaTypeId,
                        DifficultyLevelId = x.DifficultyLevelId,
                        QuestionCategoryId = x.QuestionCategoryId,
                        QuestionsIds = x.QuestionsMetadataIds
                    }
                );

                StateHasChanged();
            }
        }

        private void SelectedBlockChanged(object selectedBlock)
        {
            SelectedBlocks.Add((GetBlockResponseDto)selectedBlock);

            StateHasChanged();
        }

        private void RemoveSelectedBlock(GetBlockResponseDto selectedBlock)
        {
            SelectedBlocks.Remove(selectedBlock);
        }


        // FORM SUBMIT METHODS

        public async Task<bool> OnSecondStepBlocksSelectionSubmitAsync()
        {
            if (ValidateSelectedBlocks())
            {
                var paperBlockCreationDto = new PaperBlockCreationDto(PaperMetadataResultedParamsDto.PaperId, SelectedBlocks.ConvertAll(x => x.Id));

                var response = await BlazBlockService.AddOrUpdatePaperBlocksAsync(paperBlockCreationDto);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Snackbar.Add(response.Message, Severity.Success);
                    return true;
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }
            }

            return false;
        }


        // VALIDATION METHODS

        private bool ValidateSelectedBlocks()
        {
            if (SelectedBlocks.Count == 0)
            {
                Snackbar.Add(Resource.PleaseSelectAtLeastOneBlock, Severity.Info);
                return false;
            }

            int stagesCount = PaperMetadataResultedParamsDto.StageCount > 0
                ? PaperMetadataResultedParamsDto.StageCount
                : 3;

            if (SelectedBlocks.Count < stagesCount)
            {
                Snackbar.Add(string.Format(Resource.AdaptivePaperRequiresMoreBlocks, stagesCount), Severity.Info);
                return false;
            }

            var commonDeltaType = nameof(DeltaTypes.Common).ToLower();
            var hasCommonBlock = SelectedBlocks.Any(b => b.DeltaTypeName.ToLower() == commonDeltaType);
            if (!hasCommonBlock)
            {
                Snackbar.Add(Resource.PleaseSelectAtLeastOneCommonBlock, Severity.Info);
                return false;
            }

            var categoryGroups = SelectedBlocks.GroupBy(b => b.QuestionCategoryId).ToList();

            if (categoryGroups.Count > 1)
            {
                var commonDifficultyIds = SelectedBlocks
                    .Where(b => b.DeltaTypeName != null && b.DeltaTypeName.ToLower() == commonDeltaType)
                    .Select(b => b.DifficultyLevelId)
                    .ToHashSet();

                var allDifficultyLevels = SelectedBlocks
                    .Select(b => b.DifficultyLevelId)
                    .Distinct()
                    .ToHashSet();

                foreach (var group in categoryGroups)
                {
                    var categoryDifficultyLevels = group
                        .Select(b => b.DifficultyLevelId)
                        .Distinct()
                        .ToHashSet();

                    bool isStepPlusCategory = PaperMetadataResultedParamsDto.IsStepPlus && categoryDifficultyLevels.IsSubsetOf(commonDifficultyIds);

                    if (!isStepPlusCategory && !allDifficultyLevels.SetEquals(categoryDifficultyLevels))
                    {
                        Snackbar.Add(Resource.EachCategoryDifficultyLevelMustHaveAtLeastOneBlock, Severity.Error);
                        return false;
                    }
                }
            }

            // NOTICE: Commented for now, because you might have two blocks, on the same difficulty level, in the same category, contain the same question.

            //var blockPairs = SelectedBlocks
            //    .SelectMany((block, index) =>
            //        SelectedBlocks
            //            .Skip(index + 1)
            //            .Select(other => new { block, other })
            //    );

            //foreach (var pair in blockPairs)
            //{
            //    var sharedQuestions = pair.block.QuestionsIds
            //        .Intersect(pair.other.QuestionsIds)
            //        .ToList();

            //    if (sharedQuestions.Count > 0)
            //    {
            //        Snackbar.Add($"{Resource.Blocks} ( {pair.block.Name} - {pair.other.Name} ) {Resource.Sharedthesamequestions}", Severity.Error);
            //        return false;
            //    }
            //}

            return true;
        }

        private async Task OnDifficultyProfileChanged(ProfileDto selectedDifficiltyProfile)
        {
            _selectedDifficultyProfileInput = selectedDifficiltyProfile;

            _blockFilterPaginationModel._SelectedProfile = _selectedDifficultyProfileInput?.Id ?? 0;

            _blockListChangeKey++;

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

                _selectedDifficultyLevelInput = null;

                disabled = true;
            }

            StateHasChanged();
        }

        private async Task SelectCurrentPageBlocks()
        {
            if (_listItemRef?.table == null) return;

            var searchModel = PaginationSearchModel.GetPaginationSearchModel(
                pageIndex: _listItemRef.table.CurrentPage,
                pageSize: _listItemRef.table.RowsPerPage,
                searchKey: _listItemRef.SearchKey,
                searchInName: _listItemRef.SearchInName,
                searchInBody: _listItemRef.SearchInBody,
                searchInDescription: _listItemRef.SearchInDescription,
                fromDate: _listItemRef.FromDate,
                toDate: _listItemRef.ToDate,
                orderBy: _listItemRef.OrderBy,
                paginationOff: false,
                filterObject: _blockFilterPaginationModel
            );

            var response = await GetBlocksPaginatedAsync(searchModel);

            if (response?.Items != null && response.Items.Any())
            {
                int addedCount = 0;

                var existingIds = new HashSet<long>(SelectedBlocks.Select(x => x.Id));

                foreach (var block in response.Items)
                {
                    if (!existingIds.Contains(block.Id))
                    {
                        SelectedBlocks.Add(block);

                        existingIds.Add(block.Id);

                        addedCount++;
                    }
                }

                if (addedCount > 0)
                {
                    Snackbar.Add($"{addedCount} {Resource.Blocks} {Resource.Added} ", Severity.Success);
                    StateHasChanged();
                }
                else
                {
                    Snackbar.Add(Resource.NoNewBlocksToAdd, Severity.Info);
                }
            }
        }

        private void ResetFilters()
        {
            if (SelectedDifficultyLevel is not null || SelectedDifficultyProfileInput is not null)
            {
                SelectedDifficultyLevel = null;
                SelectedDifficultyProfileInput = null;
                SelectedCategoryInput = null;
                _blockListChangeKey++;
            }
        }

        private async Task<CustomTableData<GetBlockResponseDto>> GetBlocksPaginatedAsync(PaginationSearchModel paginationSearchModel)
        {
            _blockFilterPaginationModel._AdaptiveSubtype = PaperMetadataResultedParamsDto.SelectedPaperType == PaperType.Adaptive ?
                                                           PaperMetadataResultedParamsDto.AdaptiveSubtype :
                                                           AdaptivePaperSubtype.MST;

            _blockFilterPaginationModel._IsStepPlus = PaperMetadataResultedParamsDto.IsStepPlus;

            return await BlazBlockService.GetAllBlockPaginatedAsync(
                PaperMetadataResultedParamsDto.LanguageId,
                paginationSearchModel
            );
        }
    }
}
