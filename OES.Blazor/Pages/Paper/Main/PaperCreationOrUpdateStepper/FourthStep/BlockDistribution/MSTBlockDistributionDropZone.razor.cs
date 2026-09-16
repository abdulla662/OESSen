using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.MainComponent;
using OES.Blazor.Services.Interfaces.Block;
using OES.Blazor.Services.Interfaces.Form;
using OES.Blazor.Services.Interfaces.Template;
using OES.Helper.Dtos.Block.Responses;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.Dtos.Section;
using OES.Helper.Dtos.Stage;
using OES.Helper.Dtos.Template.Response;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using SharedHelper.Enums;

namespace OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.FourthStep.BlockDistribution
{
    public partial class MSTBlockDistributionDropZone
    {
        [Inject] private IBlazBlockService BlazBlockService { get; set; }
        [Inject] private IBlazFormService BlazFormService { get; set; }
        [Inject] private IBlazTemplateService BlazTemplateService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        [Parameter] public PaperMetadataResultedParamsDto PaperMetadataResultedParamsDto { get; set; }

        private List<StageDto> Stages { get; set; } = [];
        private List<GetChoosenBlocksDto> Blocks { get; set; } = [];
        private List<AdaptiveSectionDto> AdaptiveSections { get; set; } = [];
        private List<GetListedTemplateResponseDto> InstructionTemplate { get; set; } = [];
        private double SectionTimeInMinutes { get; set; }
        private double SectionTimeInSeconds { get; set; }
        private double StageTimeInMinutes { get; set; }
        private double StageTimeInSeconds { get; set; }

        // Selected Section (shown as "Part" in the UI)
        private MudTextField<string?> _renderedPartNameField;
        private MudDropContainer<GetChoosenBlocksDto> _dropContainer;
        private List<AdaptiveSectionDto> _availableSections = [];
        private GetChoosenBlocksDto _blockToMove;
        private AdaptiveSectionDto _newSection = new();
        private AdaptiveSectionDto _currentSection;

        // Loading properties
        private bool isLoading = true;

        // Filter properties
        private GetDifficultyLevelDto _selectedDifficulty = new();
        private QuestionCategoryDto _selectedBlockType = new();
        private bool _filtersActive;
        private int _activeFilterCount = 0;

        // Dialog visibility flags
        private bool _isAddSectionDialogOpen;
        private bool _isSectionSelectionDialogOpen;
        private bool _isSectionEditDialogOpen;
        private bool _isStageSettingsDialogOpen;

        // Stage properties
        private StageDto _currentStageSettings;
        private StageDto _currentSectionStage;

        // Other properties
        private string _distributionMode = "drag-drop";
        private HashSet<long> _selectedBlockIds = [];
        private string _bulkTargetStage = string.Empty;
        private string _bulkTargetSection = string.Empty;
        private int _componentKey = 0;
        private static long _tempIdCounter = 0;


        // DATA PROCESSING METHODS

        protected override async Task OnInitializedAsync()
        {
            isLoading = true;
            StateHasChanged();

            var response = await BlazBlockService.GetStagesBlocksByPaperIdAsync(PaperMetadataResultedParamsDto.PaperId);

            if (response?.CustomCodeStatus == CustomCodeStatus.Success && response.Data != null)
            {
                var data = response.Data as PaperStagesBlocksDto ?? new();
                Stages = data.Stages?.Where(s => s != null).ToList() ?? [];
                AdaptiveSections = data.AdaptiveSections?.Where(s => s != null).ToList() ?? [];
                Blocks = data.Blocks ?? [];
            }

            var forms = await BlazFormService.GetFormByPaperId(PaperMetadataResultedParamsDto.PaperId);

            PaperMetadataResultedParamsDto.FormId = forms.FirstOrDefault()?.Id ?? 0;

            // Generating default sections (UI: parts)
            GenerateDefaultSectionsForAdaptivePaper();

            InstructionTemplate = await BlazTemplateService.GetTemplatesByTypeId((long)TemplateTypeEnum.InstructionSection);

            isLoading = false;
            StateHasChanged();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                _dropContainer?.Refresh();
            }
        }

        private void ApplyFilters()
        {
            _activeFilterCount = 0;

            if (_selectedDifficulty != null && _selectedDifficulty.Id != 0) _activeFilterCount++;
            if (_selectedBlockType != null && _selectedBlockType.Id != 0) _activeFilterCount++;

            _filtersActive = _activeFilterCount > 0;

            _dropContainer?.Refresh();

            StateHasChanged();
        }

        private void ResetFilters()
        {
            _selectedDifficulty = null;
            _selectedBlockType = null;
            _filtersActive = false;
            _activeFilterCount = 0;

            _dropContainer?.Refresh();

            StateHasChanged();
        }

        private bool ShouldDisplayBlock(GetChoosenBlocksDto block)
        {
            // For blocks in the Blocks stage or with no stage assigned
            if (block.AdaptiveSectionId == null || block.AdaptiveSectionName == OESBlockStagesConstant.Blocks)
            {
                if (!_filtersActive)
                    return true;

                bool matchesDifficulty = _selectedDifficulty == null || _selectedDifficulty.Id == 0 || (block.DifficultyLevel != null && block.DifficultyLevel.Name == _selectedDifficulty.Name);

                bool matchesBlockType = _selectedBlockType == null || _selectedBlockType.Id == 0 || (block.BlockType != null && block.BlockType.Name == _selectedBlockType.Name);

                return matchesDifficulty && matchesBlockType;
            }

            return false;
        }

        private void TaskUpdated(MudItemDropInfo<GetChoosenBlocksDto> info)
        {
            if (info.DropzoneIdentifier == null || info.DropzoneIdentifier == OESBlockStagesConstant.Blocks)
            {
                info.Item.AdaptiveSectionId = null;
                info.Item.AdaptiveSectionName = null;

                _dropContainer?.Refresh();
                StateHasChanged();
                return;
            }

            var targetStage = info.DropzoneIdentifier?.ToString();
            if (string.IsNullOrEmpty(targetStage))
                return;

            var stage = Stages?.Find(s => s != null && s.Name == targetStage);
            if (stage == null)
            {
                Snackbar.Add(string.Format(Resource.StageNotFound, targetStage), Severity.Error);
                return;
            }

            var sectionsInStage = AdaptiveSections?
                .Where(s => s != null && s.StageId == stage.Id)
                .ToList() ?? [];

            if (sectionsInStage.Count == 0)
            {
                Snackbar.Add(Resource.PleaseCreateSectionFirst, Severity.Info);
                return;
            }

            int draggedBlockQuestionCount = info.Item.QuestionCount;

            AdaptiveSectionDto targetSection = null;

            if (sectionsInStage.Count == 1)
            {
                targetSection = sectionsInStage[0];
            }

            if (targetSection != null)
            {
                var sectionDifficulty = Blocks.Where(d => d.DifficultyLevel.Id == targetSection.DifficultyLevelId).Select(x => x.DifficultyLevel).FirstOrDefault();

                var blockDifficulty = info.Item.DifficultyLevel;

                if (sectionDifficulty != null && blockDifficulty != null &&
                    sectionDifficulty.Name != blockDifficulty.Name)
                {
                    Snackbar.Add(string.Format(Resource.CantAddBlock, blockDifficulty.Name, sectionDifficulty.Name), Severity.Warning);
                    return;
                }

                var existingBlocksInSection = GetBlocksInSection(targetSection.Id);

                if (existingBlocksInSection.Count > 0)
                {
                    var firstBlockOfSameType = existingBlocksInSection.Find(b => b.BlockType.Id == info.Item.BlockType.Id);

                    if (firstBlockOfSameType != null && draggedBlockQuestionCount != firstBlockOfSameType.QuestionCount)
                    {

                        Snackbar.Add(string.Format(
                            Resource.CantAddBlockWithDifferentQuestions,
                            info.Item.BlockType.Name,
                            draggedBlockQuestionCount,
                            firstBlockOfSameType.QuestionCount
                        ),
                        Severity.Warning);

                        return;
                    }
                }
            }

            if (sectionsInStage.Count == 1)
            {
                var section = sectionsInStage[0];
                if (section != null)
                {
                    info.Item.AdaptiveSectionId = section.Id;
                    info.Item.AdaptiveSectionName = section.Name;

                    _dropContainer?.Refresh();
                    StateHasChanged();
                }
            }
            else
            {
                _blockToMove = info.Item;
                _availableSections = [.. sectionsInStage.Where(s =>
                {
                    var sectionDiff = Blocks.Where(d => d.DifficultyLevel.Id == s.DifficultyLevelId).Select(x => x.DifficultyLevel).FirstOrDefault();

                    bool difficultyMatches = sectionDiff?.Name == info.Item.DifficultyLevel?.Name;

                    var existingBlocks = GetBlocksInSection(s.Id);

                    // Check question count consistency only if there are existing blocks
                    bool questionCountMatches = true;

                    if (existingBlocks.Count > 0)
                    {
                        var firstBlockOfSameType = existingBlocks.Find(b => b.BlockType.Id == info.Item.BlockType.Id);

                        if (firstBlockOfSameType != null)
                            questionCountMatches = draggedBlockQuestionCount == firstBlockOfSameType.QuestionCount;
                    }

                    return difficultyMatches && questionCountMatches;
                })];

                if (_availableSections.Count == 0)
                {
                    Snackbar.Add(string.Format(Resource.NoSectionsAvailable, stage.Name), Severity.Warning);
                    return;
                }

                _isSectionSelectionDialogOpen = true;
            }

            _dropContainer?.Refresh();
            StateHasChanged();
        }

        private List<GetChoosenBlocksDto> GetBlocksInSection(long sectionId)
        {
            if (Blocks == null)
                return [];

            return [.. Blocks.Where(b => b.AdaptiveSectionId == sectionId)];
        }

        private string GetDifficultyName(long id)
        {
            return Blocks.Find(d => d.DifficultyLevel.Id == id)?.DifficultyLevel.Name;
        }

        private void OnSectionSelected(AdaptiveSectionDto section)
        {
            if (_blockToMove != null)
            {
                // Update the block's section reference
                _blockToMove.AdaptiveSectionId = section.Id;
                _blockToMove.AdaptiveSectionName = section.Name;

                _isSectionSelectionDialogOpen = false;
                _dropContainer?.Refresh();
                StateHasChanged();
            }
        }

        private void RemoveBlockFromSection(GetChoosenBlocksDto block, AdaptiveSectionDto section)
        {
            if (block != null && block.AdaptiveSectionId == section.Id)
            {
                // Update block properties to remove section association
                block.AdaptiveSectionId = null;
                block.AdaptiveSectionName = null;

                _dropContainer?.Refresh();
                StateHasChanged();
            }
        }

        private void GenerateDefaultSectionsForAdaptivePaper()
        {
            var allDifficulties = Blocks
                .Where(b => b.DifficultyLevel != null && b.DeltaType != null)
                .Select(b => b.DifficultyLevel)
                .DistinctBy(d => d.Id)
                .ToList();

            if (allDifficulties.Count == 0) return;

            var normalDifficulties = allDifficulties
                .Where(d => d.DeltaTypeId == 1)
                .OrderBy(d => d.Id)
                .ToList();

            var commonDifficulty = allDifficulties
                .FirstOrDefault(d => d.DeltaTypeId == 2);

            if (commonDifficulty == null && normalDifficulties.Count > 0)
            {
                commonDifficulty = normalDifficulties[normalDifficulties.Count / 2];
            }

            string GetNextUniqueName(long stageId)
            {
                int nextNumber = 1;

                string candidateName;

                bool exists;

                do
                {
                    candidateName = $"{Resource.Part} {nextNumber}";

                    exists = AdaptiveSections.Any(s =>
                        s != null &&
                        s.StageId == stageId &&
                        !string.IsNullOrEmpty(s.Name) &&
                        s.Name.Equals(candidateName, StringComparison.OrdinalIgnoreCase)
                    );

                    nextNumber++;
                }
                while (exists);

                return candidateName;
            }

            var stage1 = Stages.FirstOrDefault(s => s.Order == 1);

            if (stage1 != null && commonDifficulty != null)
            {
                bool hasSections = AdaptiveSections.Any(s => s.StageId == stage1.Id);

                if (!hasSections)
                {
                    const int commonSectionNumbers = 3;

                    for (int i = 0; i < commonSectionNumbers; i++)
                    {
                        string partName = GetNextUniqueName(stage1.Id);

                        CreateSectionForStage(stage1, partName, commonDifficulty.Id);
                    }
                }
            }

            if (normalDifficulties.Count == 0) return;

            foreach (var stage in Stages.Where(s => s.Order > 1))
            {
                foreach (var difficulty in normalDifficulties)
                {
                    bool hasSectionForDifficulty = AdaptiveSections
                        .Any(s => s.StageId == stage.Id && s.DifficultyLevelId == difficulty.Id);

                    if (!hasSectionForDifficulty)
                    {
                        string newName = GetNextUniqueName(stage.Id);

                        CreateSectionForStage(stage, newName, difficulty.Id);
                    }
                }
            }
        }

        private void CreateSectionForStage(StageDto stage, string name, long difficultyId)
        {
            var newSection = new AdaptiveSectionDto
            {
                Id = GenerateTemporaryId(),
                Name = name,
                AdaptivePaperSubtype = AdaptivePaperSubtype.MST,
                StageId = stage.Id,
                DifficultyLevelId = difficultyId,
                TimeInMinutes = 0,
                UnScored = false
            };

            AdaptiveSections.Add(newSection);
        }

        private bool IsStageEmpty(StageDto stage)
        {
            var sections = AdaptiveSections?.Where(s => s != null && s.StageId == stage.Id).ToList() ?? [];

            return sections.Count == 0 || sections.TrueForAll(s => !Blocks.Exists(b => b.AdaptiveSectionId == s.Id));
        }

        private bool IsFirstStage(StageDto stage)
        {
            return Stages.IndexOf(stage) == 0;
        }

        private static string GetStageStyle(string stageName, bool isEmpty)
        {
            var baseStyle = stageName == OESBlockStagesConstant.Blocks
                ? "flex-shrink: 0; border: 2px solid #90caf9;"
                : "flex-shrink: 0; border: 1px solid #ddd;";

            if (isEmpty)
            {
                baseStyle += " border: 2px solid red;";
            }

            return baseStyle;
        }


        // SECTION PROCESSING METHODS

        private void OpenAddSectionDialog(StageDto stage)
        {
            SectionTimeInMinutes = 0;
            SectionTimeInSeconds = 0;
            _currentSectionStage = stage;
            _isAddSectionDialogOpen = true;
            _newSection = new AdaptiveSectionDto
            {
                StageId = stage.Id
            };

            if (IsFirstStage(stage))
            {
                var commonDeltaType = Blocks.Where(d => d.DeltaType.Name == nameof(Common)).Select(x => x.DeltaType).FirstOrDefault();

                if (commonDeltaType != null)
                {
                    var commonDifficulty = Blocks.Where(b => b.DeltaType.Id == commonDeltaType.Id).Select(b => b.DifficultyLevel).FirstOrDefault();

                    if (commonDifficulty != null)
                    {
                        _newSection.DifficultyLevelId = commonDifficulty.Id;
                    }
                    else
                    {
                        Snackbar.Add(Resource.CommonDifficultyLevelNotFound, Severity.Error);
                        return;
                    }
                }
                else
                {
                    Snackbar.Add(Resource.CommonDeltaTypeNotFound, Severity.Error);
                    return;
                }
            }

            StateHasChanged();
        }

        private void CreateSection()
        {
            if (_newSection != null && !string.IsNullOrWhiteSpace(_newSection.Name))
            {
                var trimmedName = _newSection.Name.Trim();

                if (_newSection.TimeInMinutes > PaperMetadataResultedParamsDto.PaperExamDuration)
                {
                    Snackbar.Add(string.Format(Resource.SectionTimeExceedsPaperTime, PaperMetadataResultedParamsDto.PaperExamDuration), Severity.Error);
                    return;
                }

                var nameExists = AdaptiveSections.Any(s =>
                    s != null &&
                    s.StageId == _newSection.StageId &&
                    !string.IsNullOrEmpty(s.Name) &&
                    s.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)
                );

                if (nameExists)
                {
                    Snackbar.Add(Resource.SectionNameAlreadyExists, Severity.Warning);
                    return;
                }

                _newSection.Id = GenerateTemporaryId();
                _newSection.Name = trimmedName;

                _newSection.AdaptivePaperSubtype = AdaptivePaperSubtype.MST;

                _newSection.TimeInMinutes = SectionTimeInMinutes + (SectionTimeInSeconds / 60.0);

                _newSection.InstructionSectionAdaptiveName = _newSection.InstructionSectionAdaptiveId.HasValue
                    ? InstructionTemplate?.Find(t => t.Id == _newSection.InstructionSectionAdaptiveId.Value)?.Name
                    : null;

                AdaptiveSections.Add(_newSection);
                UpdateStageOneTime();
                _newSection = new AdaptiveSectionDto();
                _isAddSectionDialogOpen = false;
                _dropContainer?.Refresh();
                StateHasChanged();
            }
            else
            {
                Snackbar.Add(Resource.PleaseEnterValidFieldData, Severity.Warning);
            }
        }

        private void OpenEditSectionDialog(AdaptiveSectionDto section)
        {
            if (section == null) return;

            _currentSection = new AdaptiveSectionDto
            {
                Id = section.Id,
                Name = section.Name,
                AdaptivePaperSubtype = AdaptivePaperSubtype.MST,
                StageId = section.StageId,
                DifficultyLevelId = section.DifficultyLevelId,
                TimeInMinutes = section.TimeInMinutes,
                InstructionSectionAdaptiveId = section.InstructionSectionAdaptiveId,
                InstructionSectionAdaptiveName = section.InstructionSectionAdaptiveName,
                UnScored = section.UnScored,
                Order = section.Order
            };

            if (_currentSection.TimeInMinutes > 0)
            {
                SectionTimeInMinutes = Math.Floor(_currentSection.TimeInMinutes);
                SectionTimeInSeconds = Math.Round((_currentSection.TimeInMinutes - SectionTimeInMinutes) * 60, 2);
            }

            _currentSectionStage = Stages.Find(s => s != null && s.Id == section.StageId);
            _isSectionEditDialogOpen = true;
            StateHasChanged();
        }

        private void RemoveSection(AdaptiveSectionDto section)
        {
            if (section == null) return;

            foreach (var block in Blocks.Where(b => b.AdaptiveSectionId == section.Id).ToList())
            {
                block.AdaptiveSectionId = null;
                block.AdaptiveSectionName = null;
            }

            AdaptiveSections.Remove(section);
            UpdateStageOneTime();
            _dropContainer?.Refresh();
            StateHasChanged();

            Snackbar.Add(string.Format(Resource.SectionAndBlocksRemoved, section.Name), Severity.Success);
        }

        private void SaveSection()
        {
            if (_currentSection == null || string.IsNullOrWhiteSpace(_currentSection.Name))
            {
                Snackbar.Add(Resource.SectionNameCannotBeEmpty, Severity.Warning);
                return;
            }

            var trimmedName = _currentSection.Name.Trim();

            if (_currentSection.TimeInMinutes > PaperMetadataResultedParamsDto.PaperExamDuration)
            {
                Snackbar.Add(string.Format(Resource.SectionTimeExceedsPaperTime, PaperMetadataResultedParamsDto.PaperExamDuration), Severity.Error);
                return;
            }

            var nameExists = AdaptiveSections.Any(s =>
                s != null &&
                s.Id != _currentSection.Id &&
                s.StageId == _currentSection.StageId &&
                !string.IsNullOrEmpty(s.Name) &&
                s.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)
            );

            if (nameExists)
            {
                Snackbar.Add(Resource.SectionNameAlreadyExists, Severity.Warning);
                return;
            }

            var sectionToUpdate = AdaptiveSections?.Find(s => s != null && s.Id == _currentSection.Id);

            if (sectionToUpdate == null)
            {
                Snackbar.Add(Resource.SectionNotFound, Severity.Error);
                CloseEditSectionDialog();
                return;
            }

            var oldSectionName = sectionToUpdate.Name;

            sectionToUpdate.Name = trimmedName;
            sectionToUpdate.AdaptivePaperSubtype = AdaptivePaperSubtype.MST;
            sectionToUpdate.DifficultyLevelId = _currentSection.DifficultyLevelId;
            sectionToUpdate.TimeInMinutes = SectionTimeInMinutes + (SectionTimeInSeconds / 60.0);
            sectionToUpdate.InstructionSectionAdaptiveId = _currentSection.InstructionSectionAdaptiveId;
            if (_currentSection.InstructionSectionAdaptiveId.HasValue)
            {
                var selectedTemplate = InstructionTemplate?.Find(t => t.Id == _currentSection.InstructionSectionAdaptiveId.Value);
                sectionToUpdate.InstructionSectionAdaptiveName = selectedTemplate?.Name;
            }
            else
            {
                sectionToUpdate.InstructionSectionAdaptiveName = null;
            }

            sectionToUpdate.UnScored = _currentSection.UnScored;

            if (!string.Equals(oldSectionName, trimmedName, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var block in Blocks.Where(b => b.AdaptiveSectionId == sectionToUpdate.Id))
                {
                    block.AdaptiveSectionName = trimmedName;
                }
            }

            CloseEditSectionDialog();
            Snackbar.Add(string.Format(Resource.SectionUpdatedSuccessfully, sectionToUpdate.Name), Severity.Success);
            UpdateStageOneTime();
            _dropContainer?.Refresh();
            StateHasChanged();
        }

        private void CloseAddSectionDialog()
        {
            _isAddSectionDialogOpen = false;
            StateHasChanged();
        }

        private void CloseSectionSelectionDialog()
        {
            _isSectionSelectionDialogOpen = false;
            StateHasChanged();
        }

        private void CloseEditSectionDialog()
        {
            _isSectionEditDialogOpen = false;
            _dropContainer?.Refresh();
            StateHasChanged();
        }


        // STAGE SETTINGS METHODS

        private void OpenStageSettingsDialog(StageDto stage)
        {
            if (stage == null) return;

            _currentStageSettings = new StageDto
            {
                Id = stage.Id,
                Name = stage.Name,
                RenderedPartName = stage.RenderedPartName,
                PaperId = stage.PaperId,
                Order = stage.Order,
                TimeInMinutes = stage.TimeInMinutes,
                InstructionSectionAdaptiveId = stage.InstructionSectionAdaptiveId,
                InstructionSectionAdaptiveName = stage.InstructionSectionAdaptiveName
            };

            var totalMinutes = (int)_currentStageSettings.TimeInMinutes;
            StageTimeInMinutes = totalMinutes;
            StageTimeInSeconds = (_currentStageSettings.TimeInMinutes - totalMinutes) * 60;

            _isStageSettingsDialogOpen = true;
            StateHasChanged();
        }

        private void CloseStageSettingsDialog()
        {
            _isStageSettingsDialogOpen = false;
            _currentStageSettings = null;
            StateHasChanged();
        }

        private async Task SaveStageSettingsAsync()
        {
            if (_currentStageSettings == null) return;

            var stageToUpdate = Stages.FirstOrDefault(s => s.Id == _currentStageSettings.Id);

            if (stageToUpdate == null) return;

            await _renderedPartNameField.Validate();

            if (_renderedPartNameField.HasErrors)
                return;

            _currentStageSettings.TimeInMinutes = StageTimeInMinutes + (StageTimeInSeconds / 60.0);

            if (_currentStageSettings.TimeInMinutes > PaperMetadataResultedParamsDto.PaperExamDuration)
            {
                Snackbar.Add(string.Format(Resource.SectionTimeExceedsPaperTime, PaperMetadataResultedParamsDto.PaperExamDuration), Severity.Error);
                return;
            }

            stageToUpdate.RenderedPartName = _currentStageSettings.RenderedPartName;

            stageToUpdate.TimeInMinutes = _currentStageSettings.TimeInMinutes;

            stageToUpdate.InstructionSectionAdaptiveId = _currentStageSettings.InstructionSectionAdaptiveId;

            if (_currentStageSettings.InstructionSectionAdaptiveId.HasValue)
            {
                var selectedTemplate = InstructionTemplate?.Find(t => t.Id == _currentStageSettings.InstructionSectionAdaptiveId.Value);

                stageToUpdate.InstructionSectionAdaptiveName = selectedTemplate?.Name;
            }
            else
            {
                stageToUpdate.InstructionSectionAdaptiveName = null;
            }

            CloseStageSettingsDialog();

            Snackbar.Add(Resource.UpdatesSavedSuccessfully, Severity.Success);

            StateHasChanged();
        }

        private void UpdateStageOneTime()
        {
            var stageOne = Stages.FirstOrDefault(s => s.Order == 1);

            if (stageOne != null)
            {
                var stageOneSections = AdaptiveSections.Where(s => s != null && s.StageId == stageOne.Id).ToList();

                stageOne.TimeInMinutes = stageOneSections.Sum(s => s.TimeInMinutes);
            }
        }

        // FORM SUBMIT METHODS

        public async Task<bool> OnFourthStepBlockDistributionDropZoneSubmitAsync()
        {
            if (await ValidateBlocksDistribution())
            {
                var stageOne = Stages.FirstOrDefault(s => s.Order == 1);

                if (stageOne != null)
                {
                    var stageOneSections = AdaptiveSections.Where(s => s != null && s.StageId == stageOne.Id).ToList();

                    stageOne.TimeInMinutes = stageOneSections.Sum(s => s.TimeInMinutes);
                }

                var paperBlocks = new PaperStagesBlocksDto
                {
                    Blocks = Blocks,
                    Stages = Stages,
                    AdaptiveSections = AdaptiveSections,
                    PaperId = PaperMetadataResultedParamsDto.PaperId,
                    FormId = PaperMetadataResultedParamsDto.FormId
                };

                var response = await BlazBlockService.CreateOrUpdatePaperBlockDistributionAsync(paperBlocks);

                if (response.CustomCodeStatus == CustomCodeStatus.Success)
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

        private void AutoDistributeBlocks()
        {
            var unassignedBlocks = Blocks.Where(b => b.AdaptiveSectionId == null).ToList();

            if (unassignedBlocks.Count == 0)
            {
                Snackbar.Add(Resource.NoUnassignedBlocksToDistribute, Severity.Info);
                return;
            }

            int distributedCount = 0;
            var random = new Random();

            var groupedBlocks = unassignedBlocks
                .GroupBy(b => new { BlockTypeId = b.BlockType.Id, DifficultyId = b.DifficultyLevel.Id })
                .ToList();

            foreach (var group in groupedBlocks)
            {
                var blocksInGroup = group.ToList();
                var difficultyId = group.Key.DifficultyId;
                var blockTypeId = group.Key.BlockTypeId;

                var matchingSections = AdaptiveSections
                    .Where(s => s != null && s.DifficultyLevelId == difficultyId)
                    .ToList();

                if (matchingSections.Count == 0)
                {
                    continue;
                }

                blocksInGroup = [.. blocksInGroup.OrderBy(_ => random.Next())];

                foreach (var block in blocksInGroup)
                {
                    var targetSection = matchingSections
                        .Select(s => new
                        {
                            Section = s,
                            CategoryBlockCount = Blocks.Count(b => b.AdaptiveSectionId == s.Id && b.BlockType.Id == blockTypeId)
                        })
                        .OrderBy(x => x.CategoryBlockCount)
                        .ThenBy(_ => random.Next())
                        .First()
                        .Section;

                    block.AdaptiveSectionId = targetSection.Id;

                    block.AdaptiveSectionName = targetSection.Name;

                    distributedCount++;
                }
            }

            if (distributedCount > 0)
            {
                _dropContainer?.Refresh();
                StateHasChanged();
                Snackbar.Add($"{Resource.AutoDistributedBlocksCount} {distributedCount}", Severity.Success);
            }
            else
            {
                Snackbar.Add(Resource.NoMatchingSectionsForUnassignedBlocks, Severity.Warning);
            }
        }


        // VALIDATION AND MISC METHODS

        private async Task<bool> ValidateBlocksDistribution()
        {
            // FIRST VALIDATION
            var unassignedBlocks = Blocks.Where(b => b.AdaptiveSectionId == null).ToList();
            if (unassignedBlocks.Count != 0)
            {
                Snackbar.Add(string.Format(Resource.UnassignedBlocksMessage, unassignedBlocks.Count), Severity.Error);
                return false;
            }

            // SECOND VALIDATION
            var sectionsGroupedByStage = AdaptiveSections
                    .Where(s => s != null)
                    .GroupBy(s => s.StageId)
                    .ToDictionary(g => g.Key, g => g.ToList());
            var emptyStages = Stages
                .Where(stage => stage != null && (
                    !sectionsGroupedByStage.ContainsKey(stage.Id) ||
                    !Blocks.Exists(b => b != null && b.AdaptiveSectionId != null && AdaptiveSections.Exists(s => s != null && s.Id == b.AdaptiveSectionId && s.StageId == stage.Id))))
                .ToList();
            if (emptyStages.Count > 0)
            {
                Snackbar.Add(string.Format(Resource.StageIsEmpty, string.Join(", ", emptyStages.Select(s => s.Name))), Severity.Error);
                return false;
            }

            // THIRD VALIDATION
            var emptySections = AdaptiveSections.Where(section => section != null && !Blocks.Exists(block => block != null && block.AdaptiveSectionId == section.Id)).ToList();
            if (emptySections.Count != 0)
            {
                Snackbar.Add(string.Format(Resource.SectionIsEmpty, string.Join(", ", emptySections.Select(s => s.Name))), Severity.Warning);
                return false;
            }

            // FOURTH VALIDATION
            var stageOne = Stages.Find(s => s != null && s.Order == 1);
            if (stageOne == null) stageOne = Stages.FirstOrDefault();
            if (stageOne != null)
            {
                var stageOneSections = AdaptiveSections.Where(s => s != null && s.StageId == stageOne.Id).ToList();
                var scoredSections = stageOneSections.Where(s => s != null && !s.UnScored).ToList();
                if (scoredSections.Count == 0)
                {
                    Snackbar.Add(string.Format(Resource.StageMustContainScoredSection, stageOne.Name), Severity.Warning);
                    return false;
                }
            }

            // FIFTH VALIDATION
            float paperDuration = PaperMetadataResultedParamsDto.PaperExamDuration;

            double totalAssignedTime = 0.0;

            foreach (var stage in Stages.Where(s => s != null))
            {
                double stageTime = 0.0;

                if (IsFirstStage(stage))
                {
                    var sections = AdaptiveSections.Where(s => s != null && s.StageId == stage.Id).ToList();

                    stageTime = sections.Sum(s => s.TimeInMinutes);
                }
                else
                {
                    stageTime = stage.TimeInMinutes;
                }

                if (paperDuration > 0 && stageTime <= 0)
                {
                    Snackbar.Add(string.Format(Resource.TotalSectionTimeMustEqualPaperTime, totalAssignedTime, paperDuration), Severity.Error);

                    return false;
                }

                totalAssignedTime += stageTime;
            }
            if (paperDuration > 0)
            {
                if (totalAssignedTime != paperDuration)
                {
                    Snackbar.Add(string.Format(Resource.TotalSectionTimeMustEqualPaperTime, totalAssignedTime, paperDuration), Severity.Error);

                    return false;
                }
            }
            else
            {
                if (totalAssignedTime > 0)
                {
                    Snackbar.Add(string.Format(Resource.TotalSectionTimeMustEqualPaperTime, totalAssignedTime, 0), Severity.Error);

                    return false;
                }
            }

            // SIXTH VALIDATION
            var allBlockTypes = Blocks
                .Where(b => b.BlockType != null)
                .Select(b => b.BlockType)
                .DistinctBy(bt => bt.Id)
                .ToList();
            if (allBlockTypes.Count == 0)
                return false;

            // SEVENTH VALIDATION
            foreach (var section in AdaptiveSections)
            {
                var blocksInSection = Blocks.Where(b => b != null && b.AdaptiveSectionId == section.Id).ToList();

                if (blocksInSection.Count == 0) continue;

                var sectionBlockTypes = blocksInSection
                    .Where(b => b?.BlockType != null)
                    .Select(b => b.BlockType)
                    .DistinctBy(bt => bt?.Id)
                    .ToList();

                var missingTypes = allBlockTypes
                    .Where(bt => !sectionBlockTypes.Exists(sbt => sbt?.Id == bt?.Id))
                    .ToList();

                if (missingTypes.Count > 0)
                {
                    var stageName = Stages.Find(s => s != null && s.Id == section.StageId)?.Name;

                    Snackbar.Add(
                        string.Format(Resource.SectionMissingBlockTypes, section.Name, stageName, string.Join(", ", missingTypes.Select(t => t.Name))),
                        Severity.Error
                    );

                    return false;
                }
            }

            // EIGHTH VALIDATION
            var assignedBlocks = Blocks.Where(b => b != null && b.AdaptiveSectionId != null && b.BlockId != 0).ToList();
            if (assignedBlocks.Count == 0)
            {
                return true;
            }

            var distinctBlockIds = assignedBlocks.Select(b => b.BlockId).Distinct().ToList();
            var fetchTasks = distinctBlockIds.ConvertAll(id => BlazBlockService.GetBlockDataById(id));
            var blocksDetails = await Task.WhenAll(fetchTasks);

            var blockIdToQuestions = new Dictionary<long, HashSet<long>>();
            for (int i = 0; i < distinctBlockIds.Count; i++)
            {
                var id = distinctBlockIds[i];
                var details = blocksDetails[i];
                if (details?.QuestionsCount == 0)
                    blockIdToQuestions[id] = [];
                else
                    blockIdToQuestions[id] = [.. details.Questions.Select(q => q.Id)];
            }

            if (stageOne != null)
            {
                var stageOneSections = AdaptiveSections.Where(s => s != null && s.StageId == stageOne.Id).ToList();
                var sectionToQuestions = new Dictionary<long, HashSet<long>>();

                foreach (var section in stageOneSections)
                {
                    var blocksInSection = assignedBlocks.Where(b => b.AdaptiveSectionId == section.Id).ToList();
                    var qset = new HashSet<long>();

                    foreach (var block in blocksInSection)
                    {
                        if (block.BlockId == 0) continue;
                        if (blockIdToQuestions.TryGetValue(block.BlockId, out var set))
                            qset.UnionWith(set);
                    }

                    sectionToQuestions[section.Id] = qset;
                }

                for (int i = 0; i < stageOneSections.Count; i++)
                {
                    for (int j = i + 1; j < stageOneSections.Count; j++)
                    {
                        var s1 = stageOneSections[i];
                        var s2 = stageOneSections[j];
                        var q1 = sectionToQuestions.TryGetValue(s1.Id, out var set1) ? set1 : [];
                        var q2 = sectionToQuestions.TryGetValue(s2.Id, out var set2) ? set2 : [];

                        if (q1.Overlaps(q2))
                        {
                            Snackbar.Add(string.Format(Resource.DuplicateQuestionAcrossSections + " ({0} - {1}) [{2}]", s1.Name, s2.Name), Severity.Error);
                            return false;
                        }
                    }
                }
            }

            var stagesToCheck = Stages
                    .Where(s => s != null && s.Order >= 2 && AdaptiveSections.Any(a => a.StageId == s.Id && assignedBlocks.Any(b => b.AdaptiveSectionId == a.Id)))
                    .ToList(); var questionAssignedStage = new Dictionary<long, StageDto>();

            foreach (var stage in stagesToCheck)
            {
                var sections = AdaptiveSections.Where(s => s != null && s.StageId == stage.Id).ToList();
                var stageQset = new HashSet<long>();

                foreach (var section in sections)
                {
                    var blocksInSection = assignedBlocks.Where(b => b.AdaptiveSectionId == section.Id).ToList();
                    foreach (var block in blocksInSection)
                    {
                        if (block.BlockId == 0) continue;
                        if (blockIdToQuestions.TryGetValue(block.BlockId, out var set))
                            stageQset.UnionWith(set);
                    }
                }

                foreach (var q in stageQset)
                {
                    if (questionAssignedStage.TryGetValue(q, out var prevStage))
                    {
                        Snackbar.Add(string.Format(Resource.DuplicateQuestionAcrossSections + " ({0} - {1}) [{2}]", prevStage.Name, stage.Name), Severity.Error);
                        return false;
                    }
                    questionAssignedStage[q] = stage;
                }
            }

            return true;
        }

        private static long GenerateTemporaryId()
        {
            // This guarantees a unique negative number (-1, -2, -3...) every time it is called,
            return Interlocked.Decrement(ref _tempIdCounter);
        }

        private static string GetDifficultyColor(long difficultyLevelId)
        {
            return difficultyLevelId switch
            {
                1 => "#4CAF50",
                2 => "#FFC107",
                3 => "#F44336",
                _ => "#9E9E9E"
            };
        }


        // DISTRIBUTION MODE METHODS

        private void ToggleBlockSelection(long blockId, bool isSelected)
        {
            if (isSelected)
                _selectedBlockIds.Add(blockId);
            else
                _selectedBlockIds.Remove(blockId);
        }

        private void ToggleSelectAllBlocks(List<GetChoosenBlocksDto> blocks, bool allSelected)
        {
            if (allSelected)
            {
                foreach (var block in blocks)
                    _selectedBlockIds.Remove(block.Id);
            }
            else
            {
                foreach (var block in blocks)
                    _selectedBlockIds.Add(block.Id);
            }

            _componentKey++;
            StateHasChanged();
        }

        private void ClearBlockSelection()
        {
            _selectedBlockIds.Clear();
            _bulkTargetStage = string.Empty;
            _bulkTargetSection = string.Empty;

            _componentKey++;
            StateHasChanged();
        }

        private void BulkAssignBlocks()
        {
            if (string.IsNullOrEmpty(_bulkTargetStage) || string.IsNullOrEmpty(_bulkTargetSection) || !_selectedBlockIds.Any())
                return;

            var stage = Stages.FirstOrDefault(s => s.Name == _bulkTargetStage);
            if (stage == null) return;

            var targetSection = AdaptiveSections.FirstOrDefault(s => s?.Name == _bulkTargetSection && s.StageId == stage.Id);
            if (targetSection == null) return;

            var blocksToMove = Blocks.Where(b => _selectedBlockIds.Contains(b.Id) && b.AdaptiveSectionName == null).ToList();

            var validationErrors = GetValidationErrors(blocksToMove, targetSection);
            if (validationErrors.Any())
            {
                foreach (var error in validationErrors)
                {
                    Snackbar.Add(error, Severity.Error, config => { config.VisibleStateDuration = 6000; });
                }
                return;
            }

            foreach (var block in blocksToMove)
            {
                block.AdaptiveSectionName = _bulkTargetSection;
                block.AdaptiveSectionId = targetSection.Id;
            }

            ClearBlockSelection();

            _componentKey++;
            StateHasChanged();
        }

        private List<string> GetValidationErrors(List<GetChoosenBlocksDto> blocksToAssign, AdaptiveSectionDto targetSection)
        {
            var errors = new List<string>();

            if (blocksToAssign.Count == 0 || targetSection == null)
            {
                errors.Add(Resource.InvalidSelectionOrTargetSection);
                return errors;
            }

            var targetStage = Stages.FirstOrDefault(s => s.Id == targetSection.StageId);
            if (targetStage == null)
            {
                errors.Add(Resource.TargetStageNotFound);
                return errors;
            }

            var sectionDifficulty = Blocks
                .Where(d => d.DifficultyLevel?.Id == targetSection.DifficultyLevelId)
                .Select(x => x.DifficultyLevel)
                .FirstOrDefault();

            foreach (var block in blocksToAssign)
            {
                if (sectionDifficulty != null && sectionDifficulty.Name != block.DifficultyLevel.Name)
                {
                    errors.Add(string.Format(Resource.CantAddBlock, block.DifficultyLevel.Name, sectionDifficulty.Name));
                }

                bool isFirstStage = IsFirstStage(targetStage);

                bool isCommonBlock = block.DeltaType?.Name == nameof(Common) ||
                                     block.DifficultyLevel?.DeltaTypeId == 2;

                if (isFirstStage && !isCommonBlock)
                {
                    errors.Add(string.Format(Resource.CantAddBlock, block.DifficultyLevel.Name, sectionDifficulty.Name));
                }
                else if (!isFirstStage && isCommonBlock)
                {
                    errors.Add(string.Format(Resource.CantAddBlock, sectionDifficulty.Name, block.DifficultyLevel.Name));
                }
            }

            //var blocksAlreadyInSection = Blocks.Where(b => b.AdaptiveSectionId == targetSection.Id).ToList();

            //int? requiredQuestionCount;
            //if (blocksAlreadyInSection.Count > 0)
            //{
            //    requiredQuestionCount = blocksAlreadyInSection[0].QuestionCount;
            //}
            //else if (blocksToAssign.Count > 0)
            //{
            //    requiredQuestionCount = blocksToAssign[0].QuestionCount;
            //}
            //else
            //{
            //    requiredQuestionCount = null;
            //}

            //if (blocksToAssign.Select(b => b.QuestionCount).Distinct().Count() > 1)
            //{
            //    errors.Add(Resource.CannotAssignBlocksWithDifferentQuestionCounts);
            //}

            //foreach (var block in blocksToAssign)
            //{
            //    if (block.DifficultyLevel.Id != targetSection.DifficultyLevelId)
            //    {
            //        var sectionDifficultyName = GetDifficultyName(targetSection.DifficultyLevelId) ?? Resource.Unknown;

            //        errors.Add(string.Format(Resource.BlockCannotBePlacedInSection,
            //            block.Name,
            //            block.DifficultyLevel.Name,
            //            sectionDifficultyName)
            //        );
            //    }

            //    if (requiredQuestionCount.HasValue && block.QuestionCount != requiredQuestionCount.Value)
            //    {
            //        if (IsFirstStage(targetStage))
            //        {
            //            errors.Add(string.Format(Resource.BlockNotAllowedDueToQuestionCount, block.Name, block.QuestionCount, requiredQuestionCount.Value));
            //        }
            //        else
            //        {
            //            errors.Add(string.Format(Resource.BlockNotAllowedInSectionDueToQuestionCount, block.Name, block.QuestionCount, requiredQuestionCount.Value));
            //        }
            //    }
            //}

            return [.. errors.Distinct()];
        }

        private static string GetFormattedTime(double totalMinutes)
        {
            int minutes = (int)totalMinutes;
            int seconds = (int)Math.Round((totalMinutes - minutes) * 60);

            if (seconds >= 60)
            {
                minutes += 1;
                seconds = 0;
            }

            if (seconds == 0)
            {
                return $"{minutes} {Resource.Mins}";
            }

            return $"{minutes}:{seconds:D2} {Resource.Mins}";
        }
    }
}