using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.Paper.Sectioning.AutoSectioning;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.MainComponent;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper;
using OES.Helper.Dtos.FlattenedTree.Responses;
using OES.Helper.Dtos.Paper.AutoSelectedQuestionsSectioning;
using OES.Helper.Dtos.SectionDistributionDto;
using OES.Helper.Dtos.SectionDistributionDto.Common;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text.RegularExpressions;
using QuestionTypeEnum = OES.Helper.Enums.QuestionType;

namespace OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.FourthStep.QuestionsSectioning
{
    public partial class AutoSelectedQuestionsSectioning
    {
        [Inject] private IBlazPaperService BlazPaperService { get; set; }
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [Parameter] public PaperMetadataResultedParamsDto PaperMetadataResultedParamsDto { get; set; } = new();

        private GetPaperItemBanksResponseDto PaperFlattenedItemBanksTree { get; set; } = new();
        private List<ItemBankSectionDisplayModel> ItemBankSections { get; set; } = [];
        private List<DistributionSectionRequestDto> DistributionSections { get; set; } = [];
        private List<MixedSelectedQuestionsNodeDto> QuestionsNodes { get; set; } = [];
        private bool IsAutoQuestionSelectionLocked => PaperMetadataResultedParamsDto.SelectedQuestionSelectionType == QuestionSelectionType.Auto && PaperMetadataResultedParamsDto.OutputFormsCount >= 2;

        private bool _isAddingSectionMode;
        private bool _questionBodyExpanded;
        private readonly DistributionSectionRequestDto _newSectionModel = new(string.Empty);
        private const string _commonItemBankSectionNamePrefix = "ItemBankSection#57284631#";
        private StepperOperationalMode _thisComponentCurrentOperationalMode = StepperOperationalMode.InsertionMode; // You may need this status field in the future
        private string _searchQuery = string.Empty;
        private bool _isDragDropMode = true;
        private Dictionary<string, string> _selectedSectionsForNodes = [];
        private readonly Dictionary<string, bool> _subQuestionBreakdownExpanded = [];
        private Dictionary<string, MixedSelectedQuestionsNodeDto> _nodesLookup = [];
        private Dictionary<string, long> _difficultyLevelLookup = [];
        private Dictionary<string, List<MixedSelectedQuestionsNodeDto>> _sectionNodes = [];
        private long _totalDistributedQuestionsCount;

        private readonly Dictionary<string, string> _questionTypeIcons = new()
        {
            { nameof(QuestionTypeEnum.MCQ), Icons.Material.Filled.RadioButtonChecked },
            { nameof(QuestionTypeEnum.Essay), Icons.Material.Filled.Edit },
            { nameof(QuestionTypeEnum.Comprehension), Icons.Material.Filled.MenuBook },
            { nameof(QuestionTypeEnum.MultipleCorrectAnswers), Icons.Material.Filled.CheckBox },
            { nameof(QuestionTypeEnum.TrueAndFalse), Icons.Material.Filled.ToggleOn },
        };

        #region Data Processing Methods

        protected override async Task OnInitializedAsync()
        {
            PaperFlattenedItemBanksTree = await BlazPaperService.GetItemBanksFlattenedTreeNodesAsync(PaperMetadataResultedParamsDto.PaperId);

            if (PaperFlattenedItemBanksTree != null)
            {
                ItemBankSections = [.. PaperFlattenedItemBanksTree.ItemBanks.Select(ib => new ItemBankSectionDisplayModel(ib.ItemBankId, ib.ItemBankName))];

                PrepareItemBankSectionsWithTheirQuestionTypes();
            }

            _difficultyLevelLookup = PaperFlattenedItemBanksTree?.ItemBanks?
                .Where(ib => ib.QuestionTypes != null)
                .SelectMany(ib => ib.QuestionTypes)
                .Where(qt => qt.DifficultyLevels != null)
                .SelectMany(qt => qt.DifficultyLevels,
                (qt, dl) => new
                {
                    Key = $"{qt.QuestionTypeId}_{(dl.DifficultyLevelName ?? "").ToLower()}",
                    dl.DifficultyLevelId
                })
                .GroupBy(x => x.Key)
                .ToDictionary(g => g.Key, g => g.First().DifficultyLevelId) ?? [];

            var paperAutoSectionsWithQuestionsResponseDto = await BlazPaperService.GetPaperAutoSectionsWithTheirQuestionsDistributionsAsync(PaperMetadataResultedParamsDto.PaperId);

            if (paperAutoSectionsWithQuestionsResponseDto.PaperAutoSections.Count > 0)
            {
                DistributionSections = paperAutoSectionsWithQuestionsResponseDto.PaperAutoSections.ConvertAll(s => new DistributionSectionRequestDto(
                    s.Name,
                    s.IsRestrictedTime,
                    s.TimeInMinutes,
                    s.IsRandom,
                    s.InstructionSectionTemplateId,
                    s.OrderId
                ));
            }

            if (paperAutoSectionsWithQuestionsResponseDto.PaperMixedQuestions.Count > 0)
            {
                foreach (var questionNode in paperAutoSectionsWithQuestionsResponseDto.PaperMixedQuestions)
                {
                    questionNode.LanguageId = PaperMetadataResultedParamsDto.LanguageId;

                    var key = GetNodeKey(questionNode.ItemBankId, questionNode.QuestionTypeId, GetItemBankSectionIdentifier(questionNode.ItemBankId));

                    _nodesLookup.TryGetValue(key, out var targetOriginalQuestionNode);

                    if (targetOriginalQuestionNode != null)
                    {
                        targetOriginalQuestionNode.CurrentQuestionsCount -= questionNode.CurrentQuestionsCount;

                        var intersectedDifficultyLevels = questionNode.AssignedDifficultyLevelsBreakdown.IntersectBy(
                            targetOriginalQuestionNode.DifficultyLevelsBreakdown.Keys,
                            x => x.Key);

                        foreach (var difficultyLevel in intersectedDifficultyLevels)
                        {
                            targetOriginalQuestionNode.DifficultyLevelsBreakdown[difficultyLevel.Key] -= difficultyLevel.Value;
                        }

                        if (targetOriginalQuestionNode.CurrentQuestionsCount <= 0)
                        {
                            QuestionsNodes.Remove(targetOriginalQuestionNode);
                            RefreshCaches();
                        }
                    }
                }

                QuestionsNodes.AddRange(paperAutoSectionsWithQuestionsResponseDto.PaperMixedQuestions);
                RefreshCaches();

                _thisComponentCurrentOperationalMode = StepperOperationalMode.UpdateMode;
            }
        }

        private void PrepareItemBankSectionsWithTheirQuestionTypes()
        {
            foreach (var itemBank in PaperFlattenedItemBanksTree.ItemBanks)
            {
                foreach (var questionType in itemBank.QuestionTypes)
                {
                    var totalQuestionsCount = questionType.DifficultyLevels.Sum(dl => dl.QuestionCount);

                    if (totalQuestionsCount > 0)
                    {
                        var difficultyBreakdown = questionType
                            .DifficultyLevels
                            .ToDictionary(dl => dl.DifficultyLevelName, dl => dl.QuestionCount);

                        QuestionsNodes.Add(new MixedSelectedQuestionsNodeDto
                        {
                            ItemBankId = itemBank.ItemBankId,
                            ItemBankName = itemBank.ItemBankName,
                            QuestionTypeId = questionType.QuestionTypeId,
                            QuestionTypeName = questionType.QuestionTypeName.ToLocalizedString<QuestionTypeEnum>(),
                            SectionName = GetItemBankSectionIdentifier(itemBank.ItemBankId),
                            CurrentQuestionsCount = totalQuestionsCount,
                            TotalManualQuestions = [],
                            DifficultyLevelsBreakdown = difficultyBreakdown,
                            AssignedDifficultyLevelsBreakdown = [],
                            LanguageId = PaperMetadataResultedParamsDto.LanguageId,
                            DifficultyProfileId = PaperMetadataResultedParamsDto.DifficultyProfileId ?? 0
                        });
                    }
                }
            }

            RefreshCaches();
        }

        private long GetItemBankSectionQuestionsCount(long itemBankId)
        {
            var key = GetItemBankSectionIdentifier(itemBankId);

            return _sectionNodes.TryGetValue(key, out var nodes) ? nodes.Sum(item => item.CurrentQuestionsCount) : 0;
        }

        private long GetDistributionSectionQuestionsCount(string distributionSectionName)
        {
            if (!_sectionNodes.TryGetValue(distributionSectionName, out var nodes))
                return 0;

            return nodes.Sum(item =>
            {
                if (item.QuestionTypeId == (long)QuestionTypeEnum.Comprehension)
                {
                    long manual = item.TotalManualQuestions?
                        .Where(q => q.IsSelected)
                        .Sum(q => (long)q.SubQuestionsCount) ?? 0;

                    long auto = item.SubQuestionDistributions?
                        .SelectMany(d => d.Value)
                        .Sum(x => (long)x.SubQuestionsCount * x.Count) ?? 0;

                    return manual + auto;
                }

                return item.CurrentQuestionsCount;
            });
        }

        private long GetAllDistributedQuestionsCount()
        {
            return QuestionsNodes
                .Where(item => item.SectionName?.StartsWith(_commonItemBankSectionNamePrefix) == false)
                .Sum(item =>
                {
                    if (item.QuestionTypeId == (long)QuestionTypeEnum.Comprehension)
                    {
                        long manual = item.TotalManualQuestions?
                            .Where(q => q.IsSelected)
                            .Sum(q => (long)q.SubQuestionsCount) ?? 0;

                        long auto = item.SubQuestionDistributions?
                            .SelectMany(d => d.Value)
                            .Sum(x => (long)x.SubQuestionsCount * x.Count) ?? 0;

                        return manual + auto;
                    }

                    return item.CurrentQuestionsCount;
                });
        }

        private static string GetItemBankSectionIdentifier(long itemBankId)
        {
            return $"{_commonItemBankSectionNamePrefix}{itemBankId}";
        }

        private void ToggleItemBankSectionCollapsing(ItemBankSectionDisplayModel targetItemBank)
        {
            targetItemBank.IsCollapsed = !targetItemBank.IsCollapsed;
        }

        private IEnumerable<ItemBankSectionDisplayModel> GetFilteredItemBanks()
        {
            if (string.IsNullOrWhiteSpace(_searchQuery))
                return ItemBankSections;

            return ItemBankSections.Where(ib => ib.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetNodeKey(long itemBankId, long questionTypeId, string sectionName)
        {
            return $"{itemBankId}_{questionTypeId}_{sectionName}";
        }

        private void RefreshNodesLookup()
        {
            _nodesLookup = QuestionsNodes.ToDictionary(x => GetNodeKey(x.ItemBankId, x.QuestionTypeId, x.SectionName));
        }

        private void RefreshSectionNodes()
        {
            _sectionNodes = QuestionsNodes.GroupBy(x => x.SectionName).ToDictionary(g => g.Key, g => g.ToList());
        }

        private void RefreshDistributedQuestionsCount()
        {
            _totalDistributedQuestionsCount = GetAllDistributedQuestionsCount();
        }

        private void RefreshCaches()
        {
            RefreshNodesLookup();
            RefreshSectionNodes();
            RefreshDistributedQuestionsCount();
        }

        private string GetSelectedSectionForNode(MixedSelectedQuestionsNodeDto node)
        {
            var key = GetNodeKey(node.ItemBankId, node.QuestionTypeId, node.SectionName);
            return _selectedSectionsForNodes.TryGetValue(key, out var value) ? value : string.Empty;
        }

        private void SetSelectedSectionForNode(MixedSelectedQuestionsNodeDto node, string sectionName)
        {
            var key = GetNodeKey(node.ItemBankId, node.QuestionTypeId, node.SectionName);
            _selectedSectionsForNodes[key] = sectionName;
        }

        private void ToggleDistributionMode()
        {
            _isDragDropMode = !_isDragDropMode;
        }

        private async Task DistributeToSelectedSectionAsync(MixedSelectedQuestionsNodeDto questionNode)
        {
            var targetSectionName = GetSelectedSectionForNode(questionNode);

            if (string.IsNullOrWhiteSpace(targetSectionName))
            {
                Snackbar.Add(Resource.SelectASection, Severity.Warning);
                return;
            }

            var oldItem = questionNode;

            if (!oldItem.SectionName.StartsWith(_commonItemBankSectionNamePrefix) && oldItem.SectionName != targetSectionName)
            {
                await ShowWarningDialog(Resource.YoucannotmoveaquestiondistributionbetweensectionsPleasereturnittothepoolandthenredistributeit);
                return;
            }

            var dialogParameters = new DialogParameters<DistributionDialog>
            {
                { x => x.SelectedItem, oldItem },
                { x => x.DifficultyBreakdown, oldItem.DifficultyLevelsBreakdown },
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<DistributionDialog>(Resource.DistributeQuestions, dialogParameters, options);
            var result = await dialog.Result;

            if (result.Canceled || result.Data is not DistributionResult distributionResult) return;

            var manuallySelectedQuestions = distributionResult.SelectedQuestions?.ToList() ?? [];

            var manualSelectionBreakdown = manuallySelectedQuestions
                .GroupBy(q => q.DifficultyLevelName)
                .ToDictionary(g => g.Key, g => (long)g.Count());

            var autoSelectionBreakdown = distributionResult.DifficultySelections;

            var mixedSelectionBreakDown = manualSelectionBreakdown
                .Concat(autoSelectionBreakdown)
                .GroupBy(x => x.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.Value)
                );

            var parentCount = GetParentCount(distributionResult, oldItem.QuestionTypeId);

            _nodesLookup.TryGetValue(GetNodeKey(oldItem.ItemBankId, oldItem.QuestionTypeId, targetSectionName), out var similarQuestionsNodeInSameSection);

            if (similarQuestionsNodeInSameSection != null)
            {
                similarQuestionsNodeInSameSection.CurrentQuestionsCount += parentCount;

                foreach (var kvp in mixedSelectionBreakDown)
                {
                    if (similarQuestionsNodeInSameSection.AssignedDifficultyLevelsBreakdown.ContainsKey(kvp.Key))
                        similarQuestionsNodeInSameSection.AssignedDifficultyLevelsBreakdown[kvp.Key] += kvp.Value;
                    else
                        similarQuestionsNodeInSameSection.AssignedDifficultyLevelsBreakdown[kvp.Key] = kvp.Value;
                }

                similarQuestionsNodeInSameSection.TotalManualQuestions.AddRange(manuallySelectedQuestions);

                if (distributionResult.SubQuestionDistributions != null)
                {
                    similarQuestionsNodeInSameSection.SubQuestionDistributions ??= [];

                    foreach (var kvp in distributionResult.SubQuestionDistributions)
                    {
                        if (similarQuestionsNodeInSameSection.SubQuestionDistributions.ContainsKey(kvp.Key))
                        {
                            similarQuestionsNodeInSameSection.SubQuestionDistributions[kvp.Key].AddRange(kvp.Value);
                        }
                        else
                        {
                            similarQuestionsNodeInSameSection.SubQuestionDistributions[kvp.Key] = [.. kvp.Value];
                        }
                    }
                }
            }
            else
            {
                var newItem = new MixedSelectedQuestionsNodeDto
                {
                    ItemBankId = oldItem.ItemBankId,
                    ItemBankName = oldItem.ItemBankName,
                    QuestionTypeId = oldItem.QuestionTypeId,
                    QuestionTypeName = oldItem.QuestionTypeName,
                    SectionName = targetSectionName,
                    CurrentQuestionsCount = parentCount,
                    DifficultyLevelsBreakdown = [],
                    AssignedDifficultyLevelsBreakdown = new Dictionary<string, long>(mixedSelectionBreakDown),
                    TotalManualQuestions = manuallySelectedQuestions,
                    SubQuestionDistributions = distributionResult.SubQuestionDistributions ?? [],
                    LanguageId = oldItem.LanguageId,
                    DifficultyProfileId = oldItem.DifficultyProfileId
                };

                QuestionsNodes.Add(newItem);
                RefreshCaches();
            }

            oldItem.DifficultyLevelsBreakdown ??= [];
            oldItem.TotalManualQuestions ??= [];
            oldItem.CurrentQuestionsCount -= parentCount;

            foreach (var kvp in mixedSelectionBreakDown)
            {
                if (oldItem.DifficultyLevelsBreakdown.ContainsKey(kvp.Key))
                {
                    oldItem.DifficultyLevelsBreakdown[kvp.Key] -= kvp.Value;
                }
            }

            if (oldItem.CurrentQuestionsCount <= 0)
            {
                QuestionsNodes.Remove(oldItem);
                RefreshCaches();
            }

            SetSelectedSectionForNode(questionNode, string.Empty);
            RefreshDistributedQuestionsCount();
            StateHasChanged();
        }

        private async Task OnQuestionsNodeUpdatedAsync(MudItemDropInfo<MixedSelectedQuestionsNodeDto> dropInfo)
        {
            if (!dropInfo.DropzoneIdentifier.StartsWith(_commonItemBankSectionNamePrefix))
                await HandleNodeDistributionOnDropAsync(dropInfo);

            RefreshDistributedQuestionsCount();
            StateHasChanged();
        }

        private async Task HandleNodeDistributionOnDropAsync(MudItemDropInfo<MixedSelectedQuestionsNodeDto> dropInfo)
        {
            var oldItem = dropInfo.Item;

            if (!oldItem.SectionName.StartsWith(_commonItemBankSectionNamePrefix) && oldItem.SectionName != dropInfo.DropzoneIdentifier)
            {
                await ShowWarningDialog(Resource.YoucannotmoveaquestiondistributionbetweensectionsPleasereturnittothepoolandthenredistributeit);
                return;
            }

            _nodesLookup.TryGetValue(GetNodeKey(oldItem.ItemBankId, oldItem.QuestionTypeId, dropInfo.DropzoneIdentifier), out var existingNode);

            var isUpdateMode = existingNode != null;

            var dialogParameters = new DialogParameters<DistributionDialog>
            {
                { x => x.SelectedItem, oldItem },
                { x => x.DifficultyBreakdown, oldItem.DifficultyLevelsBreakdown },
                { x => x.IsUpdateMode, isUpdateMode },
                { x => x.ExistingAutoDistributions, isUpdateMode ? existingNode.AssignedDifficultyLevelsBreakdown : [] },
                { x => x.ExistingSubQuestionDistributions, isUpdateMode ? existingNode.SubQuestionDistributions : [] }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<DistributionDialog>(
                isUpdateMode ? Resource.UpdateDistribution : Resource.DistributeQuestions,
                dialogParameters,
                options
            );

            var result = await dialog.Result;

            if (result.Canceled || result.Data is not DistributionResult distributionResult) return;

            var manuallySelectedQuestions = distributionResult.SelectedQuestions?.ToList() ?? [];

            var manualSelectionBreakdown = manuallySelectedQuestions
                .GroupBy(q => q.DifficultyLevelName)
                .ToDictionary(g => g.Key, g => (long)g.Count());

            var autoSelectionBreakdown = distributionResult.DifficultySelections;

            var mixedSelectionBreakDown = manualSelectionBreakdown
                .Concat(autoSelectionBreakdown)
                .GroupBy(x => x.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.Value)
                );

            if (isUpdateMode && existingNode != null)
            {
                _nodesLookup.TryGetValue(GetNodeKey(
                    existingNode.ItemBankId,
                    existingNode.QuestionTypeId,
                    GetItemBankSectionIdentifier(existingNode.ItemBankId)), out var originalQuestionsNode);

                if (originalQuestionsNode != null)
                {
                    originalQuestionsNode.CurrentQuestionsCount += existingNode.CurrentQuestionsCount;

                    foreach (var difficultyLevel in existingNode.AssignedDifficultyLevelsBreakdown)
                    {
                        if (originalQuestionsNode.DifficultyLevelsBreakdown.ContainsKey(difficultyLevel.Key))
                            originalQuestionsNode.DifficultyLevelsBreakdown[difficultyLevel.Key] += difficultyLevel.Value;
                        else
                            originalQuestionsNode.DifficultyLevelsBreakdown[difficultyLevel.Key] = difficultyLevel.Value;
                    }
                }

                QuestionsNodes.Remove(existingNode);
                RefreshCaches();

                oldItem = originalQuestionsNode ?? oldItem;
            }

            _nodesLookup.TryGetValue(GetNodeKey(oldItem.ItemBankId, oldItem.QuestionTypeId, dropInfo.DropzoneIdentifier), out var similarQuestionsNodeInSameSection);

            var parentCount = GetParentCount(distributionResult, oldItem.QuestionTypeId);

            if (similarQuestionsNodeInSameSection != null)
            {
                similarQuestionsNodeInSameSection.CurrentQuestionsCount += parentCount;

                foreach (var kvp in mixedSelectionBreakDown)
                {
                    if (similarQuestionsNodeInSameSection.AssignedDifficultyLevelsBreakdown.ContainsKey(kvp.Key))
                        similarQuestionsNodeInSameSection.AssignedDifficultyLevelsBreakdown[kvp.Key] += kvp.Value;
                    else
                        similarQuestionsNodeInSameSection.AssignedDifficultyLevelsBreakdown[kvp.Key] = kvp.Value;
                }

                similarQuestionsNodeInSameSection.TotalManualQuestions.AddRange(manuallySelectedQuestions);
            }
            else
            {
                var newItem = new MixedSelectedQuestionsNodeDto
                {
                    ItemBankId = oldItem.ItemBankId,
                    ItemBankName = oldItem.ItemBankName,
                    QuestionTypeId = oldItem.QuestionTypeId,
                    QuestionTypeName = oldItem.QuestionTypeName,
                    SectionName = dropInfo.DropzoneIdentifier,
                    CurrentQuestionsCount = parentCount,
                    DifficultyLevelsBreakdown = [],
                    AssignedDifficultyLevelsBreakdown = new Dictionary<string, long>(mixedSelectionBreakDown),
                    TotalManualQuestions = manuallySelectedQuestions,
                    SubQuestionDistributions = distributionResult.SubQuestionDistributions ?? [],
                    LanguageId = oldItem.LanguageId,
                    DifficultyProfileId = oldItem.DifficultyProfileId
                };

                QuestionsNodes.Add(newItem);
                RefreshCaches();
            }

            oldItem.DifficultyLevelsBreakdown ??= [];
            oldItem.TotalManualQuestions ??= [];
            oldItem.CurrentQuestionsCount -= parentCount;

            foreach (var kvp in mixedSelectionBreakDown)
            {
                if (oldItem.DifficultyLevelsBreakdown.ContainsKey(kvp.Key))
                {
                    oldItem.DifficultyLevelsBreakdown[kvp.Key] -= kvp.Value;
                }
            }

            if (oldItem.CurrentQuestionsCount <= 0)
            {
                QuestionsNodes.Remove(oldItem);
                RefreshCaches();
            }

            StateHasChanged();
        }

        private async Task OpenUpdateDistributionDialogAsync(MixedSelectedQuestionsNodeDto questionNode)
        {
            _nodesLookup.TryGetValue(GetNodeKey(questionNode.ItemBankId, questionNode.QuestionTypeId, GetItemBankSectionIdentifier(questionNode.ItemBankId)), out var poolNode);

            var totalAvailableCount = (poolNode?.CurrentQuestionsCount ?? 0) + questionNode.CurrentQuestionsCount;

            var availableBreakdown = new Dictionary<string, long>();

            if (poolNode?.DifficultyLevelsBreakdown != null)
            {
                foreach (var kvp in poolNode.DifficultyLevelsBreakdown)
                {
                    availableBreakdown[kvp.Key] = kvp.Value;
                }
            }

            foreach (var kvp in questionNode.AssignedDifficultyLevelsBreakdown)
            {
                if (availableBreakdown.ContainsKey(kvp.Key))
                    availableBreakdown[kvp.Key] += kvp.Value;
                else
                    availableBreakdown[kvp.Key] = kvp.Value;
            }

            var itemForFetching = new MixedSelectedQuestionsNodeDto
            {
                ItemBankId = questionNode.ItemBankId,
                ItemBankName = questionNode.ItemBankName,
                QuestionTypeId = questionNode.QuestionTypeId,
                QuestionTypeName = questionNode.QuestionTypeName,
                SectionName = GetItemBankSectionIdentifier(questionNode.ItemBankId),
                CurrentQuestionsCount = totalAvailableCount,
                DifficultyLevelsBreakdown = availableBreakdown,
                TotalManualQuestions = questionNode.TotalManualQuestions?.ToList() ?? [],
                AssignedDifficultyLevelsBreakdown = [],
                LanguageId = questionNode.LanguageId,
                DifficultyProfileId = questionNode.DifficultyProfileId
            };

            var dialogParameters = new DialogParameters<DistributionDialog>
            {
                { x => x.SelectedItem, itemForFetching },
                { x => x.DifficultyBreakdown, availableBreakdown },
                { x => x.IsUpdateMode, true },
                { x => x.ExistingAutoDistributions, questionNode.AssignedDifficultyLevelsBreakdown },
                { x => x.ExistingSubQuestionDistributions, questionNode.SubQuestionDistributions }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<DistributionDialog>(Resource.UpdateDistribution, dialogParameters, options);
            var result = await dialog.Result;

            if (result.Canceled || result.Data is not DistributionResult distributionResult) return;

            _nodesLookup.TryGetValue(GetNodeKey(questionNode.ItemBankId, questionNode.QuestionTypeId, GetItemBankSectionIdentifier(questionNode.ItemBankId)), out var originalPoolNode);

            var parentCount = GetParentCount(distributionResult, questionNode.QuestionTypeId);

            if (originalPoolNode != null)
            {
                originalPoolNode.CurrentQuestionsCount += questionNode.CurrentQuestionsCount;

                foreach (var difficultyLevel in questionNode.AssignedDifficultyLevelsBreakdown)
                {
                    if (originalPoolNode.DifficultyLevelsBreakdown.ContainsKey(difficultyLevel.Key))
                        originalPoolNode.DifficultyLevelsBreakdown[difficultyLevel.Key] += difficultyLevel.Value;
                    else
                        originalPoolNode.DifficultyLevelsBreakdown[difficultyLevel.Key] = difficultyLevel.Value;
                }
            }
            else
            {
                originalPoolNode = new MixedSelectedQuestionsNodeDto
                {
                    ItemBankId = questionNode.ItemBankId,
                    ItemBankName = questionNode.ItemBankName,
                    QuestionTypeId = questionNode.QuestionTypeId,
                    QuestionTypeName = questionNode.QuestionTypeName,
                    SectionName = GetItemBankSectionIdentifier(questionNode.ItemBankId),
                    CurrentQuestionsCount = questionNode.CurrentQuestionsCount,
                    TotalManualQuestions = [],
                    DifficultyLevelsBreakdown = new Dictionary<string, long>(questionNode.AssignedDifficultyLevelsBreakdown),
                    AssignedDifficultyLevelsBreakdown = [],
                    LanguageId = questionNode.LanguageId,
                    DifficultyProfileId = questionNode.DifficultyProfileId
                };

                QuestionsNodes.Add(originalPoolNode);
                RefreshCaches();
            }

            var manuallySelectedQuestions = distributionResult.SelectedQuestions?.ToList() ?? [];

            var manualSelectionBreakdown = manuallySelectedQuestions
                .GroupBy(q => q.DifficultyLevelName)
                .ToDictionary(g => g.Key, g => (long)g.Count());

            var autoSelectionBreakdown = distributionResult.DifficultySelections;

            var mixedSelectionBreakDown = manualSelectionBreakdown
                .Concat(autoSelectionBreakdown)
                .GroupBy(x => x.Key)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Value));

            questionNode.CurrentQuestionsCount = parentCount;
            questionNode.AssignedDifficultyLevelsBreakdown = new Dictionary<string, long>(mixedSelectionBreakDown);
            questionNode.TotalManualQuestions = manuallySelectedQuestions;
            questionNode.SubQuestionDistributions = distributionResult.SubQuestionDistributions ?? [];

            originalPoolNode.CurrentQuestionsCount -= parentCount;

            foreach (var kvp in mixedSelectionBreakDown)
            {
                if (originalPoolNode.DifficultyLevelsBreakdown.ContainsKey(kvp.Key))
                {
                    originalPoolNode.DifficultyLevelsBreakdown[kvp.Key] -= kvp.Value;
                }
            }

            if (originalPoolNode.CurrentQuestionsCount <= 0)
            {
                QuestionsNodes.Remove(originalPoolNode);
                RefreshCaches();
            }

            RefreshDistributedQuestionsCount();
            StateHasChanged();
        }

        private void ReturnDistributedNodeBackToItsPool(MixedSelectedQuestionsNodeDto selectedQuestionsNode, bool refresh = true)
        {
            if (selectedQuestionsNode.SectionName.StartsWith(_commonItemBankSectionNamePrefix)) return;

            _nodesLookup.TryGetValue(GetNodeKey(selectedQuestionsNode.ItemBankId, selectedQuestionsNode.QuestionTypeId, GetItemBankSectionIdentifier(selectedQuestionsNode.ItemBankId)), out var originalQuestionsNode);

            if (originalQuestionsNode != null)
            {
                selectedQuestionsNode.TotalManualQuestions = [];

                originalQuestionsNode.CurrentQuestionsCount += selectedQuestionsNode.CurrentQuestionsCount;

                foreach (var difficultyLevel in selectedQuestionsNode.AssignedDifficultyLevelsBreakdown)
                {
                    if (originalQuestionsNode.DifficultyLevelsBreakdown.ContainsKey(difficultyLevel.Key))
                        originalQuestionsNode.DifficultyLevelsBreakdown[difficultyLevel.Key] += difficultyLevel.Value;
                    else
                        originalQuestionsNode.DifficultyLevelsBreakdown[difficultyLevel.Key] = difficultyLevel.Value;
                }
            }
            else
            {
                QuestionsNodes.Add(new MixedSelectedQuestionsNodeDto
                {
                    ItemBankId = selectedQuestionsNode.ItemBankId,
                    ItemBankName = selectedQuestionsNode.ItemBankName,
                    QuestionTypeId = selectedQuestionsNode.QuestionTypeId,
                    QuestionTypeName = selectedQuestionsNode.QuestionTypeName,
                    SectionName = GetItemBankSectionIdentifier(selectedQuestionsNode.ItemBankId),
                    CurrentQuestionsCount = selectedQuestionsNode.CurrentQuestionsCount,
                    TotalManualQuestions = [],
                    DifficultyLevelsBreakdown = selectedQuestionsNode.AssignedDifficultyLevelsBreakdown,
                    AssignedDifficultyLevelsBreakdown = [],
                    LanguageId = selectedQuestionsNode.LanguageId,
                    DifficultyProfileId = selectedQuestionsNode.DifficultyProfileId
                });
            }

            QuestionsNodes.Remove(selectedQuestionsNode);

            if (refresh)
            {
                RefreshCaches();
                StateHasChanged();
            }
        }

        private static Color GetDifficultyColor(string difficulty)
        {
            return difficulty switch
            {
                "Hard" => Color.Error,
                "Medium" => Color.Warning,
                "Easy" => Color.Success,
                "Low" => Color.Success,
                _ => Color.Info
            };
        }

        #endregion Data Processing Methods

        #region Section Management

        private void StartAddingSection()
        {
            _isAddingSectionMode = true;
        }

        private void CancelAddingSection()
        {
            _isAddingSectionMode = false;
            _newSectionModel.Name = string.Empty;
        }

        private void AddNewSection()
        {
            if (string.IsNullOrWhiteSpace(_newSectionModel.Name))
            {
                Snackbar.Add(Resource.Sectionnamecannotbeempty, Severity.Error);
                return;
            }

            if (_newSectionModel.Name.Trim().Length < 2)
            {
                Snackbar.Add(Resource.Sectionnamemustcontainatleast2characters, Severity.Error);
                return;
            }

            if (DistributionSections.Exists(s => s.Name.Equals(_newSectionModel.Name, StringComparison.OrdinalIgnoreCase)))
            {
                Snackbar.Add(Resource.Asectionwiththisnamealreadyexists, Severity.Error);
                return;
            }

            var existingOrders = DistributionSections
                .Where(s => s.OrderId > 0)
                .Select(s => s.OrderId)
                .Order()
                .ToList();

            int nextOrder = existingOrders.Count > 0
                ? existingOrders[^1] + 1
                : 1;

            var newSection = new DistributionSectionRequestDto(_newSectionModel.Name)
            {
                OrderId = nextOrder,
                IsRandom = false,
                IsRestrictedTime = false,
                TimeInMinutes = 0
            };

            DistributionSections.Add(newSection);

            DistributionSections = [.. DistributionSections.OrderBy(s => s.OrderId)];

            _newSectionModel.Name = string.Empty;
            _isAddingSectionMode = false;

            StateHasChanged();
        }

        private static void StartSectionRenaming(DistributionSectionRequestDto section)
        {
            section.IsRenaming = true;
            section.NewName = section.Name;
        }

        private void RenameSection(DistributionSectionRequestDto section)
        {
            if (string.IsNullOrWhiteSpace(section.NewName))
            {
                Snackbar.Add(Resource.Sectionnamecannotbeempty, Severity.Error);
                return;
            }

            var oldSectionName = section.Name;

            section.Name = section.NewName;

            section.IsRenaming = false;

            foreach (var qn in QuestionsNodes.Where(q => q.SectionName == oldSectionName))
            {
                qn.SectionName = section.Name;
            }

            RefreshCaches();
        }

        private static void CancelSectionRenaming(DistributionSectionRequestDto section)
        {
            section.IsRenaming = false;
            section.NewName = string.Empty;
        }

        private static void ToggleSectionCollapsing(DistributionSectionRequestDto section)
        {
            section.IsCollapsed = !section.IsCollapsed;
        }

        private async Task OpenSectionPropertiesDialogAsync(DistributionSectionRequestDto section)
        {
            var allNumbers = Enumerable
                .Range(1, DistributionSections.Count)
                .Select(x => x)
                .ToList();

            var parameters = new DialogParameters<AutoSectionPropertiesDialog>
            {
                { x => x.Section, section },
                { x => x.AvailableOrderNumbers, allNumbers }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<AutoSectionPropertiesDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                DistributionSections = [.. DistributionSections.OrderBy(s => s.OrderId)];

                Snackbar.Add(Resource.SectionPropertiesUpdatedSuccessfully, Severity.Success);

                StateHasChanged();
            }
        }

        private async Task OpenSectionTemplateDialogAsync(DistributionSectionRequestDto section)
        {
            var parameters = new DialogParameters<AutoSectionInstructionTemplateDialog>
            {
                { x => x.Section, section }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            await DialogService.ShowAsync<AutoSectionInstructionTemplateDialog>(string.Empty, parameters, options);
        }

        private async Task DeleteSectionAsync(string sectionName)
        {
            var sectionQuestionsNodes = QuestionsNodes.Where(i => i.SectionName == sectionName).ToList();

            var parameters = new DialogParameters<GenericDialog>
            {
                { x => x.Content, $"{Resource.ThisSectionContains} {sectionQuestionsNodes.Count} {Resource.Items}. {Resource.DeleteAndReturnQuestions}" },
                { x => x.SubmitText, Resource.Delete },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitButtonColor, Color.Error },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.Delete }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(Resource.DeleteConfirmation, parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                if (sectionQuestionsNodes.Count > 0)
                {
                    foreach (var node in sectionQuestionsNodes)
                    {
                        ReturnDistributedNodeBackToItsPool(node, false);
                    }

                    RefreshCaches();
                    StateHasChanged();
                }

                DistributionSections.RemoveAll(x => x.Name == sectionName);

                StateHasChanged();
            }
        }

        #endregion Section Management

        #region Save Distribution

        public async Task<bool> OnFourthStepAutoQuestionsSectioningSubmitAsync()
        {
            var validationResult = await ValidateDistributedQuestionsNodes();

            if (validationResult.IsValid)
            {
                var addSectionDistributionsRequest = new AddOrUpdateAutoSectionsDistributionsRequestDto
                {
                    PaperId = PaperMetadataResultedParamsDto.PaperId,
                    Sections = validationResult.PreparedSections
                };

                var response = await BlazPaperService.AddOrUpdateSectionsDistributionsForAutoAsync(addSectionDistributionsRequest);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Snackbar.Add(response.Message, Severity.Success);
                    return true;
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                    return false;
                }
            }

            return false;
        }

        private List<SectionWithDistributionsDto> PrepareDistributedQuestionsNodesForSubmit()
        {
            var sections = new List<SectionWithDistributionsDto>();

            foreach (var section in DistributionSections)
            {
                var sectionNodes = QuestionsNodes.Where(i => i.SectionName == section.Name).ToList();

                var sectionDto = new SectionWithDistributionsDto
                {
                    Name = section.Name,
                    IsRestrictedTime = section.IsRestrictedTime,
                    TimeInMinutes = section.TimeInMinutes,
                    IsRandom = section.IsRandom,
                    Distributions = [],
                    InstructionSectionTemplateId = section.InstructionSectionTemplateId,
                    OrderId = section.OrderId,
                };

                foreach (var node in sectionNodes)
                {
                    foreach (var difficultyLevel in node.AssignedDifficultyLevelsBreakdown)
                    {
                        if (difficultyLevel.Value <= 0)
                        {
                            continue;
                        }

                        var difficultyLevelId = GetDifficultyLevelId(node.QuestionTypeId, difficultyLevel.Key);

                        if (node.QuestionTypeId == (long)QuestionTypeEnum.Comprehension)
                        {
                            if (node.SubQuestionDistributions != null &&
                                node.SubQuestionDistributions.TryGetValue(difficultyLevel.Key, out var subRows) &&
                                subRows.Count > 0)
                            {
                                foreach (var row in subRows)
                                {
                                    sectionDto.Distributions.Add(new SectionDistributionDto
                                    {
                                        ItemBankId = node.ItemBankId,
                                        QuestionTypeID = node.QuestionTypeId,
                                        DifficultyLevelID = difficultyLevelId,
                                        SubQuestionCount = row.SubQuestionsCount,
                                        SelectedCount = row.Count,
                                        QuestionIds = []
                                    });
                                }
                            }

                            var manualQuestionsForDifficulty = node.TotalManualQuestions?
                                .Where(q => q.DifficultyLevelName == difficultyLevel.Key)
                                .ToList();

                            if (manualQuestionsForDifficulty?.Count > 0)
                            {
                                var groupedBySubQ = manualQuestionsForDifficulty
                                    .GroupBy(q => q.SubQuestionsCount);

                                foreach (var subQGroup in groupedBySubQ)
                                {
                                    sectionDto.Distributions.Add(new SectionDistributionDto
                                    {
                                        ItemBankId = node.ItemBankId,
                                        QuestionTypeID = node.QuestionTypeId,
                                        DifficultyLevelID = difficultyLevelId,
                                        SubQuestionCount = subQGroup.Key,
                                        SelectedCount = subQGroup.Count(),
                                        QuestionIds = [.. subQGroup.Select(q => q.Id)]
                                    });
                                }
                            }

                            continue;
                        }
                        else
                        {
                            sectionDto.Distributions.Add(new SectionDistributionDto
                            {
                                ItemBankId = node.ItemBankId,
                                QuestionTypeID = node.QuestionTypeId,
                                DifficultyLevelID = difficultyLevelId,
                                SubQuestionCount = 1,
                                SelectedCount = difficultyLevel.Value,
                                QuestionIds = [.. node.TotalManualQuestions
                                    .Where(q => q.DifficultyLevelName == difficultyLevel.Key)
                                    .Select(q => q.Id)]
                            });
                        }
                    }
                }

                sections.Add(sectionDto);
            }

            return sections;
        }

        #endregion Save Distribution

        #region Helper Methods

        private async Task<(bool IsValid, List<SectionWithDistributionsDto> PreparedSections)> ValidateDistributedQuestionsNodes()
        {
            var distributedCount = GetAllDistributedQuestionsCount();

            if (distributedCount != PaperMetadataResultedParamsDto.QuestionsCount)
            {
                Snackbar.Add($"{Resource.SelectedQuestions} ({distributedCount}) {Resource.dontmatchthetotalrequired} ({PaperMetadataResultedParamsDto.QuestionsCount})", Severity.Error);
                return (false, []);
            }

            var addedSections = PrepareDistributedQuestionsNodesForSubmit();

            if (addedSections.Count == 0)
            {
                Snackbar.Add(Resource.Pleaseaddatleastonesection, Severity.Error);
                return (false, []);
            }

            if (addedSections.Exists(x => x.Distributions.Count == 0))
            {
                Snackbar.Add(Resource.Sectionscannotbeempty, Severity.Error);
                return (false, []);
            }

            var (isValid, errorMessage) = ValidateSectionsDurationsAgainstPaperDuration();

            if (!isValid)
            {
                Snackbar.Add(errorMessage, Severity.Error);
                return (false, []);
            }

            return (true, addedSections);
        }

        private (bool isValid, string errorMessage) ValidateSectionsDurationsAgainstPaperDuration()
        {
            var invalidTimedSection = DistributionSections
                .FirstOrDefault(s => s.IsRestrictedTime && s.TimeInMinutes <= 0);

            if (invalidTimedSection != null)
            {
                return (false, string.Format(Resource.SectionRestrictedTimeInvalidDuration, invalidTimedSection.Name));
            }

            const float epsilon = 0.0001f;
            double sum = DistributionSections.Sum(s => s.TimeInMinutes);

            if (DistributionSections.TrueForAll(x => x.IsRestrictedTime))
            {
                var isValid = Math.Abs(sum - PaperMetadataResultedParamsDto.PaperExamDuration) < epsilon;
                return (isValid, string.Format(Resource.RestrictedSectionsDurationMismatch, sum, PaperMetadataResultedParamsDto.PaperExamDuration));
            }
            else if (DistributionSections.TrueForAll(x => !x.IsRestrictedTime))
            {
                return (true, Resource.SectionsAddedSuccessfully);
            }
            else
            {
                return (false, Resource.SectionsTimingError);
            }
        }

        private async Task ShowWarningDialog(string message)
        {
            var parameters = new DialogParameters<DistributionTransferWarningDialog>
            {
                { x => x.ContentText, message },
                { x => x.ButtonText, Resource.Ok },
                { x => x.Color, Color.Error }
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };

            await DialogService.ShowAsync<DistributionTransferWarningDialog>(Resource.Alert, parameters, options);
        }

        private long GetDifficultyLevelId(long questionTypeId, string difficultyName)
        {
            return _difficultyLevelLookup.TryGetValue($"{questionTypeId}_{difficultyName.ToLower()}", out var id) ? id : 0;
        }

        private string GetQuestionTypeIcon(string questionTypeName)
        {
            return _questionTypeIcons.TryGetValue(questionTypeName, out string value) ? value : Icons.Material.Filled.Help;
        }

        private async Task ShowSummaryDialogAsync()
        {
            var atLeastQuestionsNodeDistributed = QuestionsNodes.Select(x => x.SectionName).Intersect(DistributionSections.Select(x => x.Name)).Any();

            if (DistributionSections.Count == 0 || !atLeastQuestionsNodeDistributed)
            {
                Snackbar.Add(Resource.Youhaventcreatedanydistributionsectionsyet, Severity.Warning);
                return;
            }

            var totalDistributedQuestionsCount = GetAllDistributedQuestionsCount();

            var summaryData = new Dictionary<string, List<SummaryItem>>();

            foreach (var section in DistributionSections)
            {
                var sectionNodes = QuestionsNodes
                    .Where(i => i.SectionName == section.Name)
                    .ToList();

                var sectionSummary = new List<SummaryItem>();

                var questionTypeGroups = sectionNodes.GroupBy(i => i.QuestionTypeName);

                foreach (var typeGroup in questionTypeGroups)
                {
                    var questionType = typeGroup.Key;
                    var totalCount = typeGroup.Sum(i => i.CurrentQuestionsCount);

                    var difficultyBreakdown = new Dictionary<string, long>();
                    var sourceItemBanks = new Dictionary<string, long>();

                    foreach (var item in typeGroup)
                    {
                        if (!sourceItemBanks.ContainsKey(item.ItemBankName))
                        {
                            sourceItemBanks[item.ItemBankName] = 0;
                        }

                        sourceItemBanks[item.ItemBankName] += item.CurrentQuestionsCount;

                        foreach (var diff in item.AssignedDifficultyLevelsBreakdown)
                        {
                            if (!difficultyBreakdown.ContainsKey(diff.Key))
                            {
                                difficultyBreakdown[diff.Key] = 0;
                            }

                            difficultyBreakdown[diff.Key] += diff.Value;
                        }
                    }

                    sectionSummary.Add(new SummaryItem
                    {
                        QuestionType = questionType,
                        TotalCount = totalCount,
                        DifficultyBreakdown = difficultyBreakdown,
                        SourceItemBanks = sourceItemBanks
                    });
                }

                summaryData[section.Name] = sectionSummary;
            }

            var parameters = new DialogParameters<DistributionSummaryDialog>
            {
                { x => x.SummaryData, summaryData },
                { x => x.TotalDistributed, totalDistributedQuestionsCount },
                { x => x.TotalRequired, PaperMetadataResultedParamsDto.QuestionsCount }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            await DialogService.ShowAsync<DistributionSummaryDialog>(Resource.DistributionSummary, parameters, options);
        }

        private static string CleanHtmlLineBreaks(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            html = Regex.Replace(html, @"<p>(\s|&nbsp;|<br\s*/?>)*</p>", string.Empty, RegexOptions.IgnoreCase);

            return html.Trim();
        }

        private bool IsSubQuestionBreakdownExpanded(MixedSelectedQuestionsNodeDto node)
        {
            var key = $"{node.ItemBankId}_{node.QuestionTypeId}_{node.SectionName}";
            return _subQuestionBreakdownExpanded.TryGetValue(key, out var val) && val;
        }

        private void ToggleSubQuestionBreakdown(MixedSelectedQuestionsNodeDto node)
        {
            var key = $"{node.ItemBankId}_{node.QuestionTypeId}_{node.SectionName}";
            _subQuestionBreakdownExpanded[key] = !IsSubQuestionBreakdownExpanded(node);
        }

        private static long GetParentCount(DistributionResult distributionResult, long questionTypeId)
        {
            var manualCount = distributionResult.SelectedQuestions?.Count ?? 0;

            if (questionTypeId == (long)QuestionTypeEnum.Comprehension)
            {
                var autoComprehensionCount = distributionResult.SubQuestionDistributions?
                    .SelectMany(x => x.Value)
                    .Sum(x => x.Count) ?? 0;

                return manualCount + autoComprehensionCount;
            }

            var normalAutoCount = distributionResult.DifficultySelections?.Values.Sum() ?? 0;

            return manualCount + normalAutoCount;
        }

        #endregion Helper Methods
    }
}
