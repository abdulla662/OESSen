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
    public partial class STEPBlockDistributionDropZone
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
        private readonly HashSet<long> _selectedBlockIds = [];
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

            // STEP-specific: Validate category match
            var blockCategory = info.Item.BlockType;

            if (blockCategory == null)
            {
                Snackbar.Add(Resource.BlockCategoryNotFound, Severity.Error);
                return;
            }

            var assignedCategoryId = GetAssignedCategoryForStageGroup(stage);

            if (assignedCategoryId.HasValue && blockCategory.Id != assignedCategoryId.Value)
            {
                var assignedCategoryName = Blocks
                    .FirstOrDefault(b => b.BlockType?.Id == assignedCategoryId.Value)?.BlockType?.Name ?? Resource.Unknown;

                Snackbar.Add(
                    $"{Resource.StageCategory} '{assignedCategoryName}'. {Resource.Canotblockcategory} '{blockCategory.Name}'",
                    Severity.Warning
                );

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
                if (targetSection.DifficultyLevelId != info.Item.DifficultyLevel.Id)
                {
                    var sectionDifficultyName = Blocks
                        .Where(d => d.DifficultyLevel != null && d.DifficultyLevel.Id == targetSection.DifficultyLevelId)
                        .Select(x => x.DifficultyLevel.Name)
                        .FirstOrDefault() ?? Resource.Unknown;

                    Snackbar.Add(string.Format(Resource.CantAddBlock, info.Item.DifficultyLevel.Name, sectionDifficultyName), Severity.Warning);
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
                    bool difficultyMatches = s.DifficultyLevelId == info.Item.DifficultyLevel?.Id;

                    var existingBlocks = GetBlocksInSection(s.Id);

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


        // STEP-SPECIFIC METHODS

        private long? GetAssignedCategoryForStageGroup(StageDto stage)
        {
            const int stagesPerGroup = 3;

            int groupIndex = (stage.Order - 1) / stagesPerGroup;
            int groupStartOrder = (groupIndex * stagesPerGroup) + 1;
            int groupEndOrder = groupStartOrder + stagesPerGroup - 1;

            var stagesInGroup = Stages
                .Where(s => s.Order >= groupStartOrder && s.Order <= groupEndOrder)
                .ToList();

            var sectionIdsInGroup = AdaptiveSections
                .Where(s => stagesInGroup.Any(sg => sg.Id == s.StageId))
                .Select(s => s.Id)
                .ToList();

            var blocksInGroup = Blocks
                .Where(b => b.AdaptiveSectionId.HasValue &&
                            sectionIdsInGroup.Contains(b.AdaptiveSectionId.Value) &&
                            b.BlockType != null)
                .ToList();

            if (blocksInGroup.Count == 0)
                return null;

            return blocksInGroup[0].BlockType.Id;
        }

        private static bool IsFirstStageInCategoryGroup(StageDto stage)
        {
            const int stagesPerGroup = 3;
            return (stage.Order - 1) % stagesPerGroup == 0;
        }

        private string GetDifficultyName(long id)
        {
            return Blocks.Find(d => d.DifficultyLevel.Id == id)?.DifficultyLevel.Name;
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


        // SECTION GENERATION METHODS

        private void GenerateDefaultSectionsForAdaptivePaper()
        {
            const int stagesPerGroup = 3;

            int totalGroups = (int)Math.Ceiling(Stages.Count / (double)stagesPerGroup);

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

            for (int groupIndex = 0; groupIndex < totalGroups; groupIndex++)
            {
                int groupStartOrder = (groupIndex * stagesPerGroup) + 1;
                int groupEndOrder = Math.Min(groupStartOrder + stagesPerGroup - 1, Stages.Count);

                var groupStages = Stages
                    .Where(s => s.Order >= groupStartOrder && s.Order <= groupEndOrder)
                    .OrderBy(s => s.Order)
                    .ToList();

                if (groupStages.Count == 0) continue;

                var firstStage = groupStages[0];
                if (commonDifficulty != null)
                {
                    bool hasSections = AdaptiveSections.Any(s => s.StageId == firstStage.Id);

                    if (!hasSections)
                    {
                        string partName = GetNextUniqueName(firstStage.Id);
                        CreateSectionForStage(firstStage, partName, commonDifficulty.Id);
                    }
                }

                if (normalDifficulties.Count == 0) continue;

                for (int i = 1; i < groupStages.Count; i++)
                {
                    var stage = groupStages[i];

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
        }

        private void CreateSectionForStage(StageDto stage, string name, long difficultyId)
        {
            var newSection = new AdaptiveSectionDto
            {
                Id = GenerateTemporaryId(),
                Name = name,
                AdaptivePaperSubtype = AdaptivePaperSubtype.STEP,
                StageId = stage.Id,
                DifficultyLevelId = difficultyId,
                TimeInMinutes = 0,
                UnScored = IsStepPlusStage(stage)
            };

            AdaptiveSections.Add(newSection);
        }

        private string GetNextUniqueName(long stageId)
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

        private bool IsStageEmpty(StageDto stage)
        {
            var sections = AdaptiveSections?.Where(s => s != null && s.StageId == stage.Id).ToList() ?? [];

            return sections.Count == 0 || sections.TrueForAll(s => !Blocks.Exists(b => b.AdaptiveSectionId == s.Id));
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
            if (!IsFirstStageInCategoryGroup(stage))
            {
                Snackbar.Add(Resource.CanOnlyAddSectionsToFirstStageOfCategory, Severity.Warning);
                return;
            }

            SectionTimeInMinutes = 0;
            SectionTimeInSeconds = 0;
            _currentSectionStage = stage;
            _isAddSectionDialogOpen = true;
            _newSection = new AdaptiveSectionDto
            {
                StageId = stage.Id,
                UnScored = IsStepPlusStage(stage),
            };


            var commonDeltaType = Blocks
                .Where(d => d.DeltaType != null && d.DeltaType.Name == nameof(Common))
                .Select(x => x.DeltaType)
                .FirstOrDefault();

            if (commonDeltaType != null)
            {
                var commonDifficulty = Blocks
                    .Where(b => b.DeltaType != null && b.DeltaType.Id == commonDeltaType.Id)
                    .Select(b => b.DifficultyLevel)
                    .FirstOrDefault();

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

                _newSection.AdaptivePaperSubtype = AdaptivePaperSubtype.STEP;

                _newSection.TimeInMinutes = SectionTimeInMinutes + (SectionTimeInSeconds / 60.0);

                _newSection.InstructionSectionAdaptiveName = _newSection.InstructionSectionAdaptiveId.HasValue
                   ? InstructionTemplate?.Find(t => t.Id == _newSection.InstructionSectionAdaptiveId.Value)?.Name
                   : null;

                var stage = Stages.FirstOrDefault(s => s.Id == _newSection.StageId);

                AdaptiveSections.Add(_newSection);

                if (stage != null)
                {
                    UpdateFirstStageTimeInGroup(stage);
                }

                _isAddSectionDialogOpen = false;

                _newSection = new AdaptiveSectionDto();
                _dropContainer?.Refresh();
                StateHasChanged();
            }
            else
            {
                Snackbar.Add(Resource.PleaseEnterValidFieldData, Severity.Warning);
            }
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

        private void CloseSectionSelectionDialog()
        {
            _isSectionSelectionDialogOpen = false;
            StateHasChanged();
        }

        private void RemoveSection(AdaptiveSectionDto section)
        {
            if (section == null) return;

            var blocksInSection = Blocks.Where(b => b.AdaptiveSectionId == section.Id).ToList();

            foreach (var block in blocksInSection)
            {
                block.AdaptiveSectionId = null;
                block.AdaptiveSectionName = null;
            }

            var stage = Stages.FirstOrDefault(s => s.Id == section.StageId);

            AdaptiveSections.Remove(section);

            if (stage != null)
            {
                UpdateFirstStageTimeInGroup(stage);
            }

            _dropContainer?.Refresh();
            StateHasChanged();
        }

        private void OpenEditSectionDialog(AdaptiveSectionDto section)
        {
            if (section == null) return;

            _currentSection = section;
            _currentSectionStage = Stages.FirstOrDefault(s => s.Id == section.StageId);

            var totalMinutes = (int)section.TimeInMinutes;
            SectionTimeInMinutes = totalMinutes;
            SectionTimeInSeconds = (section.TimeInMinutes - totalMinutes) * 60;

            _isSectionEditDialogOpen = true;
            StateHasChanged();
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
            sectionToUpdate.AdaptivePaperSubtype = AdaptivePaperSubtype.STEP;
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

            if (IsStepPlusStage(_currentSectionStage))
            {
                sectionToUpdate.UnScored = true;
            }
            else
            {
                sectionToUpdate.UnScored = _currentSection.UnScored;
            }

            if (!string.Equals(oldSectionName, trimmedName, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var block in Blocks.Where(b => b.AdaptiveSectionId == sectionToUpdate.Id))
                {
                    block.AdaptiveSectionName = trimmedName;
                }
            }

            CloseEditSectionDialog();

            Snackbar.Add(string.Format(Resource.SectionUpdatedSuccessfully, sectionToUpdate.Name), Severity.Success);

            var stage = Stages.FirstOrDefault(s => s.Id == _currentSection.StageId);

            if (stage != null)
            {
                UpdateFirstStageTimeInGroup(stage);
            }

            _dropContainer?.Refresh();
            StateHasChanged();
        }

        private void CloseAddSectionDialog()
        {
            _isAddSectionDialogOpen = false;
            _newSection = new AdaptiveSectionDto();
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

        private void SaveStageSettings()
        {
            if (_currentStageSettings == null) return;

            var stageToUpdate = Stages.FirstOrDefault(s => s.Id == _currentStageSettings.Id);

            if (stageToUpdate == null) return;

            double totalSeconds = StageTimeInMinutes * 60 + StageTimeInSeconds;

            double totalMinutes = totalSeconds / 60;

            if (totalMinutes > PaperMetadataResultedParamsDto.PaperExamDuration)
            {
                Snackbar.Add(string.Format(Resource.SectionTimeExceedsPaperTime, PaperMetadataResultedParamsDto.PaperExamDuration), Severity.Error);
                return;
            }

            stageToUpdate.RenderedPartName = _currentStageSettings.RenderedPartName;

            stageToUpdate.TimeInMinutes = totalMinutes;

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

        private void UpdateFirstStageTimeInGroup(StageDto stage)
        {
            if (stage != null && IsFirstStageInCategoryGroup(stage))
            {
                var stageSections = AdaptiveSections.Where(s => s != null && s.StageId == stage.Id).ToList();
                stage.TimeInMinutes = stageSections.Sum(s => s.TimeInMinutes);
            }
        }

        private void CloseStageSettingsDialog()
        {
            _isStageSettingsDialogOpen = false;
            _currentStageSettings = null;
            StateHasChanged();
        }


        // FORM SUBMIT METHODS

        public async Task<bool> OnFourthStepBlockDistributionDropZoneSubmitAsync()
        {
            if (await ValidateDistribution())
            {
                const int stagesPerGroup = 3;

                int totalGroups = (int)Math.Ceiling(Stages.Count / (double)stagesPerGroup);

                for (int groupIndex = 0; groupIndex < totalGroups; groupIndex++)
                {
                    int groupStartOrder = (groupIndex * stagesPerGroup) + 1;

                    var firstStage = Stages.FirstOrDefault(s => s.Order == groupStartOrder);

                    if (firstStage != null)
                    {
                        var stageSections = AdaptiveSections.Where(s => s != null && s.StageId == firstStage.Id).ToList();
                        firstStage.TimeInMinutes = stageSections.Sum(s => s.TimeInMinutes);
                    }
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
            const int stagesPerGroup = 3;

            var blocksByCategory = unassignedBlocks
                .Where(b => b.BlockType != null)
                .GroupBy(b => b.BlockType.Id)
                .ToDictionary(g => g.Key, g => g.ToList());

            int totalGroups = (int)Math.Ceiling(Stages.Count / (double)stagesPerGroup);

            var shuffledCategories = blocksByCategory.Keys.OrderBy(_ => random.Next()).ToList();

            int categoryIndex = 0;

            for (int groupIndex = 0; groupIndex < totalGroups && categoryIndex < shuffledCategories.Count; groupIndex++)
            {
                var categoryId = shuffledCategories[categoryIndex];
                var categoryBlocks = blocksByCategory[categoryId];

                int groupStartOrder = (groupIndex * stagesPerGroup) + 1;
                int groupEndOrder = Math.Min(groupStartOrder + stagesPerGroup - 1, Stages.Count);

                var groupStages = Stages
                    .Where(s => s.Order >= groupStartOrder && s.Order <= groupEndOrder)
                    .OrderBy(s => s.Order)
                    .ToList();

                if (groupStages.Count == 0) continue;

                var assignedCategory = GetAssignedCategoryForStageGroup(groupStages[0]);
                if (assignedCategory.HasValue && assignedCategory.Value != categoryId)
                {
                    continue;
                }

                var sectionsInGroup = AdaptiveSections
                    .Where(s => groupStages.Any(gs => gs.Id == s.StageId))
                    .ToList();

                if (sectionsInGroup.Count == 0) continue;

                var blocksByDifficulty = categoryBlocks
                    .GroupBy(b => b.DifficultyLevel.Id)
                    .ToList();

                foreach (var difficultyGroup in blocksByDifficulty)
                {
                    var blocksInDifficultyGroup = difficultyGroup.ToList();
                    var difficultyId = difficultyGroup.Key;

                    var matchingSections = sectionsInGroup
                        .Where(s => s.DifficultyLevelId == difficultyId)
                        .ToList();

                    if (matchingSections.Count == 0) continue;

                    blocksInDifficultyGroup = blocksInDifficultyGroup.OrderBy(_ => random.Next()).ToList();

                    foreach (var block in blocksInDifficultyGroup)
                    {
                        var targetSection = matchingSections
                            .Select(s => new
                            {
                                Section = s,
                                BlockCount = Blocks.Count(b => b.AdaptiveSectionId == s.Id)
                            })
                            .OrderBy(x => x.BlockCount)
                            .ThenBy(_ => random.Next())
                            .First()
                            .Section;

                        block.AdaptiveSectionId = targetSection.Id;
                        block.AdaptiveSectionName = targetSection.Name;

                        distributedCount++;
                    }
                }

                categoryIndex++;
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

        public async Task<bool> ValidateDistribution()
        {
            // FIRST VALIDATION
            var undistributedBlocks = Blocks.Where(b => b.AdaptiveSectionId == null).ToList();

            if (undistributedBlocks.Count > 0)
            {
                Snackbar.Add(string.Format(Resource.UndistributedBlocksExist, undistributedBlocks.Count), Severity.Error);

                return false;
            }

            // SECOND VALIDATION
            foreach (var stage in Stages)
            {
                if (IsStageEmpty(stage))
                {
                    Snackbar.Add(string.Format(Resource.StageIsEmpty, stage.Name), Severity.Error);

                    return false;
                }
            }

            // THIRD VALIDATION
            foreach (var stage in Stages)
            {
                var sectionsInStage = AdaptiveSections.Where(s => s != null && s.StageId == stage.Id).ToList();

                if (sectionsInStage.Count == 0)
                {
                    Snackbar.Add(string.Format(Resource.StageHasNoSections, stage.Name), Severity.Error);

                    return false;
                }

                foreach (var section in sectionsInStage)
                {
                    var blocksInSection = Blocks.Where(b => b != null && b.AdaptiveSectionId == section.Id).ToList();

                    if (blocksInSection.Count == 0)
                    {
                        Snackbar.Add(string.Format(Resource.SectionHasNoBlocks, section.Name, stage.Name), Severity.Error);

                        return false;
                    }
                }
            }

            // FOURTH VALIDATION
            foreach (var section in AdaptiveSections)
            {
                var blocksInSection = Blocks.Where(b => b != null && b.AdaptiveSectionId == section.Id).ToList();

                if (blocksInSection.Count == 0) continue;

                var questionCounts = blocksInSection.Select(b => b.QuestionCount).Distinct().ToList();

                if (questionCounts.Count > 1)
                {
                    var stageName = Stages.Find(s => s != null && s.Id == section.StageId)?.Name;

                    Snackbar.Add(string.Format(Resource.SectionHasMismatchedQuestionCounts, section.Name, stageName), Severity.Error);

                    return false;
                }
            }

            // FIFTH VALIDATION
            float paperDuration = PaperMetadataResultedParamsDto.PaperExamDuration;

            double totalAssignedTime = 0.0;

            foreach (var stage in Stages.Where(s => s != null))
            {
                double stageTime = 0.0;

                if (IsFirstStageInCategoryGroup(stage))
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

            // SIXTH VALIDATION - STEP-specific: Validate category consistency within stage groups
            const int stagesPerGroup = 3;
            int totalGroups = (int)Math.Ceiling(Stages.Count / (double)stagesPerGroup);

            for (int groupIndex = 0; groupIndex < totalGroups; groupIndex++)
            {
                int groupStartOrder = (groupIndex * stagesPerGroup) + 1;
                int groupEndOrder = Math.Min(groupStartOrder + stagesPerGroup - 1, Stages.Count);

                var groupStages = Stages
                    .Where(s => s.Order >= groupStartOrder && s.Order <= groupEndOrder)
                    .ToList();

                if (groupStages.Count == 0) continue;

                var sectionsInGroup = AdaptiveSections
                    .Where(s => groupStages.Any(gs => gs.Id == s.StageId))
                    .ToList();

                var blocksInGroup = Blocks
                    .Where(b => b.AdaptiveSectionId.HasValue &&
                                sectionsInGroup.Any(s => s.Id == b.AdaptiveSectionId.Value) &&
                                b.BlockType != null)
                    .ToList();

                if (blocksInGroup.Count == 0) continue;

                var categoriesInGroup = blocksInGroup
                    .Select(b => b.BlockType.Id)
                    .Distinct()
                    .ToList();

                if (categoriesInGroup.Count > 1)
                {
                    return false;
                }
            }

            // SEVENTH VALIDATION
            var assignedBlocks = Blocks.Where(b => b != null && b.AdaptiveSectionId != null && b.BlockId != 0).ToList();
            if (assignedBlocks.Count == 0)
                return true;

            var distinctBlockIds = assignedBlocks.Select(b => b.BlockId).Distinct().ToList();
            var fetchTasks = distinctBlockIds.ConvertAll(id => BlazBlockService.GetBlockDataById(id));
            var blocksDetailsArray = await Task.WhenAll(fetchTasks);

            var blockIdToQuestions = new Dictionary<long, HashSet<long>>();
            for (int i = 0; i < distinctBlockIds.Count; i++)
            {
                var id = distinctBlockIds[i];
                var details = blocksDetailsArray[i];
                if (details?.QuestionsCount == 0)
                    blockIdToQuestions[id] = [];
                else
                    blockIdToQuestions[id] = [.. details.Questions.Select(q => q.Id)];
            }

            var categories = assignedBlocks
                .Where(b => b.BlockType != null)
                .Select(b => new { Id = b.BlockType.Id, Name = b.BlockType.Name })
                .DistinctBy(x => x.Id)
                .ToList();

            foreach (var category in categories)
            {
                var blocksForCategory = assignedBlocks.Where(b => b.BlockType != null && b.BlockType.Id == category.Id).ToList();

                var stageOneLocal = Stages.Find(s => s != null && s.Order == 1) ?? Stages.FirstOrDefault();
                if (stageOneLocal != null)
                {
                    var stageOneSections = AdaptiveSections.Where(s => s != null && s.StageId == stageOneLocal.Id).ToList();

                    var sectionToQuestions = new Dictionary<long, HashSet<long>>();
                    foreach (var section in stageOneSections)
                    {
                        var blocksInSection = blocksForCategory.Where(b => b.AdaptiveSectionId == section.Id).ToList();
                        var qset = new HashSet<long>();

                        foreach (var block in blocksInSection)
                        {
                            if (block.BlockId == 0) continue;
                            if (blockIdToQuestions.TryGetValue(block.BlockId, out var set))
                            {
                                qset.UnionWith(set);
                            }
                        }

                        sectionToQuestions[section.Id] = qset;
                    }

                    for (int i = 0; i < stageOneSections.Count; i++)
                    {
                        for (int j = i + 1; j < stageOneSections.Count; j++)
                        {
                            var s1 = stageOneSections[i];
                            var s2 = stageOneSections[j];
                            var q1 = sectionToQuestions.GetValueOrDefault(s1.Id, []);
                            var q2 = sectionToQuestions.GetValueOrDefault(s2.Id, []);
                            if (q1.Overlaps(q2))
                            {
                                Snackbar.Add(string.Format(Resource.DuplicateQuestionAcrossSections + " ({0} - {1}) [{2}]", s1.Name, s2.Name, category.Name), Severity.Error);
                                return false;
                            }
                        }
                    }
                }

                var stagesToCheck = Stages
                    .Where(s => s != null && s.Order >= 2 && AdaptiveSections.Any(a => a.StageId == s.Id && blocksForCategory.Any(b => b.AdaptiveSectionId == a.Id)))
                    .ToList();

                var questionAssignedStage = new Dictionary<long, StageDto>();

                foreach (var stage in stagesToCheck)
                {
                    var sections = AdaptiveSections.Where(s => s != null && s.StageId == stage.Id).ToList();
                    var stageQset = new HashSet<long>();

                    foreach (var section in sections)
                    {
                        var blocksInSection = blocksForCategory.Where(b => b.AdaptiveSectionId == section.Id).ToList();
                        foreach (var block in blocksInSection)
                        {
                            if (block.BlockId == 0) continue;
                            if (blockIdToQuestions.TryGetValue(block.BlockId, out var set))
                            {
                                foreach (var q in set)
                                {
                                    stageQset.Add(q);
                                }
                            }
                        }
                    }

                    foreach (var q in stageQset)
                    {
                        if (questionAssignedStage.TryGetValue(q, out var prevStage))
                        {
                            Snackbar.Add(string.Format(Resource.DuplicateQuestionAcrossSections + " ({0} - {1}) [{2}]", prevStage.Name, stage.Name, category.Name), Severity.Error);
                            return false;
                        }

                        questionAssignedStage[q] = stage;
                    }
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

            if (blocksToMove.Count == 0)
                return;

            var assignedCategoryId = GetAssignedCategoryForStageGroup(stage);

            var existingBlocksInSection = GetBlocksInSection(targetSection.Id);

            var validationErrors = new List<string>();

            foreach (var block in blocksToMove)
            {
                var blockCategory = block.BlockType;

                if (blockCategory == null)
                {
                    validationErrors.Add(Resource.BlockCategoryNotFound);
                    continue;
                }

                if (assignedCategoryId.HasValue && blockCategory.Id != assignedCategoryId.Value)
                {
                    var assignedCategoryName = Blocks
                        .FirstOrDefault(b => b.BlockType?.Id == assignedCategoryId.Value)?.BlockType?.Name ?? Resource.Unknown;

                    validationErrors.Add(
                        $"{Resource.StageCategory} '{assignedCategoryName}'. {Resource.Canotblockcategory} '{blockCategory.Name}'"
                    );
                    continue;
                }

                if (targetSection.DifficultyLevelId != block.DifficultyLevel.Id)
                {
                    var sectionDifficultyName = Blocks
                        .Where(d => d.DifficultyLevel != null && d.DifficultyLevel.Id == targetSection.DifficultyLevelId)
                        .Select(x => x.DifficultyLevel.Name)
                        .FirstOrDefault() ?? Resource.Unknown;

                    validationErrors.Add(string.Format(Resource.CantAddBlock, block.DifficultyLevel.Name, sectionDifficultyName));
                    continue;
                }

                if (existingBlocksInSection.Count > 0)
                {
                    var firstBlockOfSameType = existingBlocksInSection.Find(b => b.BlockType.Id == block.BlockType.Id);

                    if (firstBlockOfSameType != null && block.QuestionCount != firstBlockOfSameType.QuestionCount)
                    {
                        validationErrors.Add(string.Format(
                            Resource.CantAddBlockWithDifferentQuestions,
                            block.BlockType.Name,
                            block.QuestionCount,
                            firstBlockOfSameType.QuestionCount
                        ));
                        continue;
                    }
                }

                existingBlocksInSection.Add(block);
            }

            if (validationErrors.Count > 0)
            {
                foreach (var error in validationErrors.Distinct())
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
        private List<string> GetValidationErrors(List<GetChoosenBlocksDto> blocksToAssign, AdaptiveSectionDto targetSection, StageDto targetStage)
        {
            var errors = new List<string>();
            if (blocksToAssign.Count == 0 || targetSection == null)
            {
                errors.Add(Resource.InvalidSelectionOrTargetSection);
                return errors;
            }

            if (targetStage == null)
            {
                errors.Add(Resource.TargetStageNotFound);
                return errors;
            }

            var assignedCategoryId = GetAssignedCategoryForStageGroup(targetStage);

            var blockCategories = blocksToAssign
                .Where(b => b.BlockType != null)
                .Select(b => b.BlockType.Id)
                .Distinct()
                .ToList();

            if (blockCategories.Count > 1)
            {
                errors.Add(Resource.AllBlocksMustMatchStageCategory);
                return errors;
            }

            var blockCategoryId = blockCategories[0];

            if (assignedCategoryId.HasValue && blockCategoryId != assignedCategoryId.Value)
            {
                errors.Add(Resource.AllBlocksMustMatchStageCategory);
            }

            var blockDifficultyIds = blocksToAssign
                .Where(b => b.DifficultyLevel != null)
                .Select(b => b.DifficultyLevel.Id)
                .Distinct()
                .ToList();

            var blockDifficultyId = blockDifficultyIds[0];

            if (targetSection.DifficultyLevelId != blockDifficultyId)
            {
                var sectionDifficultyName = Blocks
                    .Where(d => d.DifficultyLevel != null && d.DifficultyLevel.Id == targetSection.DifficultyLevelId)
                    .Select(x => x.DifficultyLevel.Name)
                    .FirstOrDefault() ?? Resource.Unknown;

                var blockDifficultyName = blocksToAssign[0].DifficultyLevel?.Name ?? Resource.Unknown;

                errors.Add(string.Format(Resource.CantAddBlock, blockDifficultyName, sectionDifficultyName));
            }

            return [.. errors.Distinct()];
        }

        private bool IsStepPlusStage(StageDto stage)
        {
            if (stage == null) return false;

            return PaperMetadataResultedParamsDto.IsStepPlus && stage.Order == Stages.Count;
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