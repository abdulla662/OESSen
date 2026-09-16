using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Pages.AIItemBankGenerator.Dialogs;
using OES.Blazor.Services.Interfaces.AIFeatures;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.ItemBankLevel;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.AIItemBankGenerator.ThirdStep
{
    public partial class AIItemBankReviewStep
    {
        [Inject] private IBlazAIItemBankGenerationService BlazAIItemBankGenerationService { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        [Parameter] public AIItemBankNodeDto? GeneratedTree { get; set; }
        [Parameter] public bool IsConfigurationLocked { get; set; }
        [Parameter] public EventCallback<bool> IsConfigurationLockedChanged { get; set; }
        [Parameter] public int? MaxDepth { get; set; } = 50;
        [Parameter] public int? MaxChildrenPerNode { get; set; } = 50;
        [Parameter] public ItemBankLevelsCreation ItemBankLevelsCreation { get; set; }
        [Parameter] public List<ItemLevelsDto> ExistingLevels { get; set; } = [];

        private bool _isSaving;

        private AIItemBankNodeDto? _selectedNode;

        private AIItemBankNodeDto? _nodeToMove;

        private int? _moveTargetNumber;


        private static AIItemBankNodeDto? FindFather(AIItemBankNodeDto current, AIItemBankNodeDto target)
        {
            if (current.Children == null) return null;

            if (current.Children.Contains(target))
            {
                return current;
            }

            foreach (var child in current.Children)
            {
                var found = FindFather(child, target);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static (string? Level, long LevelId) FindLevelAtDepth(AIItemBankNodeDto root, int targetDepth, int currentDepth = 1)
        {
            if (currentDepth == targetDepth)
            {
                if (root.LevelId > 0 || !string.IsNullOrWhiteSpace(root.Level))
                {
                    return (root.Level, root.LevelId);
                }

                return (null, 0);
            }

            if (root.Children == null)
            {
                return (null, 0);
            }

            foreach (var child in root.Children)
            {
                var result = FindLevelAtDepth(child, targetDepth, currentDepth + 1);

                if (result.LevelId > 0 || !string.IsNullOrWhiteSpace(result.Level))
                {
                    return result;
                }
            }

            return (null, 0);
        }

        private async Task AddChildNodeAsync(AIItemBankNodeDto parentNode)
        {
            if (GeneratedTree == null)
                return;

            var parentDepth = GetNodeDepth(
                GeneratedTree,
                parentNode);

            if (parentDepth == -1)
            {
                Snackbar.Add(
                    Resource.UnableToDetermineNodeDepth,
                    Severity.Error);

                return;
            }

            if (parentDepth >= MaxDepth)
            {
                Snackbar.Add(
                    Resource.MaximumTreeDepthReached,
                    Severity.Warning);

                return;
            }

            parentNode.Children ??= [];

            if (parentNode.Children.Count >= MaxChildrenPerNode)
            {
                Snackbar.Add(
                    Resource.NodeHasMaxChildrenNumber,
                    Severity.Warning);

                return;
            }

            var newNodeDepth = parentDepth + 1;

            var (predefinedLevel, predefinedLevelId) = FindLevelAtDepth(
                GeneratedTree,
                newNodeDepth);

            var newNode = new AIItemBankNodeDto
            {
                Name = string.Empty,
                Code = string.Empty,
                Level = predefinedLevel ?? string.Empty,
                LevelId = predefinedLevelId,
                Hours = 0,
                Description = string.Empty
            };

            var parameters = new DialogParameters<AIItemBankNodeAddDialog>
            {
                {
                    x => x.NodeModel,
                    newNode
                },
                {
                    x => x.IsLevelPredefined,
                    predefinedLevelId > 0 || !string.IsNullOrWhiteSpace(predefinedLevel)
                },
                {
                    x => x.ItemBankLevelsCreation,
                    ItemBankLevelsCreation
                },
                {
                    x => x.ExistingLevels,
                    ExistingLevels
                },
                {
                    x => x.ExistingSiblingNodes,
                    parentNode.Children?.ToList() ?? []
                }
            };

            var dialog = await DialogService.ShowAsync<AIItemBankNodeAddDialog>(
                Resource.AddChildNode,
                parameters);

            var result = await dialog.Result;

            if (result == null ||
                result.Canceled ||
                result.Data is not AIItemBankNodeDto createdNode)
            {
                return;
            }

            parentNode.Children.Add(createdNode);

            StateHasChanged();
        }

        private async Task EditNodeAsync(AIItemBankNodeDto node)
        {
            var parentNode = GeneratedTree != null ? FindFather(GeneratedTree, node) : null;

            var siblingNodes = parentNode?.Children?.Where(x => !ReferenceEquals(x, node)).ToList() ?? [];

            var nodeCopy = new AIItemBankNodeDto
            {
                Name = node.Name,
                Code = node.Code,
                Level = node.Level,
                LevelId = node.LevelId,
                Hours = node.Hours,
                Description = node.Description,
                Unscored = node.Unscored,
                IsActive = node.IsActive,
                ParentId = node.ParentId,
                OESGroupDtos = node.OESGroupDtos != null ? [.. node.OESGroupDtos] : []
            };

            var parameters = new DialogParameters<AIItemBankNodeEditDialog>
            {
                {
                    x => x.NodeModel,
                    nodeCopy
                },
                {
                    x => x.ExistingLevels,
                    ExistingLevels
                },
                {
                    x => x.ExistingSiblingNodes,
                    siblingNodes
                }
            };

            var dialog = await DialogService.ShowAsync<AIItemBankNodeEditDialog>(
                Resource.Edit,
                parameters);

            var result = await dialog.Result;

            if (result == null ||
                result.Canceled ||
                result.Data is not AIItemBankNodeDto editedNode)
            {
                return;
            }

            node.Name = editedNode.Name;
            node.Code = editedNode.Code;
            node.Hours = editedNode.Hours;
            node.Description = editedNode.Description;

            var newLevel = editedNode.Level?.Trim();

            if (!string.Equals(node.Level, newLevel, StringComparison.OrdinalIgnoreCase))
            {
                if (GeneratedTree != null)
                {
                    var father = FindFather(GeneratedTree, node);
                    if (father != null && father.Children != null)
                    {
                        foreach (var sibling in father.Children)
                        {
                            sibling.Level = newLevel;
                            sibling.LevelId = 0;
                        }
                    }
                    else
                    {
                        node.Level = newLevel;
                        node.LevelId = 0;
                    }
                }
                else
                {
                    node.Level = newLevel;
                    node.LevelId = 0;
                }
            }
            else
            {
                node.Level = editedNode.Level;
                node.LevelId = editedNode.LevelId;
            }

            node.Unscored = editedNode.Unscored;
            node.IsActive = editedNode.IsActive;
            node.OESGroupDtos = editedNode.OESGroupDtos;

            StateHasChanged();
        }

        private async Task DeleteNodeAsync(AIItemBankNodeDto node)
        {
            var parameters = new DialogParameters<GenericDialog>
            {
                { x => x.Title, Resource.Delete },
                { x => x.Content, Resource.AreYouSureYouWantToDeleteThisItem },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Delete },
                { x => x.SubmitButtonColor, Color.Error }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);
            var result = await dialog.Result;

            if (result == null || result.Canceled)
            {
                return;
            }

            RemoveNodeFromParent(
                GeneratedTree,
                node);

            if (_nodeToMove != null && (ReferenceEquals(_nodeToMove, node) || IsDescendant(node, _nodeToMove)))
            {
                CancelMove();
            }

            if (ReferenceEquals(_selectedNode, node))
            {
                _selectedNode = null;
            }

            StateHasChanged();
        }

        private async Task PrepareMoveNodeAsync(AIItemBankNodeDto node)
        {
            if (GeneratedTree == null || ReferenceEquals(node, GeneratedTree))
            {
                return;
            }

            _nodeToMove = node;
            _moveTargetNumber = null;

            StateHasChanged();

            await Task.Delay(50);

            await JSRuntime.InvokeVoidAsync(
                "scrollToElement",
                "move-node-section");
        }

        private void CancelMove()
        {
            _nodeToMove = null;

            _moveTargetNumber = null;

            StateHasChanged();
        }

        private void MoveSelectedNode()
        {
            if (GeneratedTree == null ||
                _nodeToMove == null ||
                !_moveTargetNumber.HasValue)
            {
                return;
            }

            if (_moveTargetNumber.Value < 0)
            {
                Snackbar.Add(Resource.NodeNumberNotFound, Severity.Error);
                return;
            }

            var targetNode = FindNodeByNumber(
                GeneratedTree,
                _moveTargetNumber.Value);

            if (targetNode == null)
            {
                Snackbar.Add(
                    Resource.NodeNumberNotFound,
                    Severity.Error);

                return;
            }

            if (ReferenceEquals(_nodeToMove, targetNode))
            {
                Snackbar.Add(
                    Resource.NodeCannotBeMovedIntoItself,
                    Severity.Warning);

                return;
            }

            if (IsDescendant(_nodeToMove, targetNode))
            {
                Snackbar.Add(
                    Resource.CannotMoveNodeIntoItsChildren,
                    Severity.Warning);

                return;
            }

            if (targetNode.Children?.Contains(_nodeToMove) == true)
            {
                CancelMove();
                return;
            }

            targetNode.Children ??= [];

            if (targetNode.Children.Count >= MaxChildrenPerNode)
            {
                Snackbar.Add(
                    Resource.NodeHasMaxChildrenNumber,
                    Severity.Warning);

                return;
            }

            var duplicateName = targetNode.Children.Any(x => !ReferenceEquals(x, _nodeToMove) &&
            string.Equals(x.Name?.Trim(), _nodeToMove.Name?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (duplicateName)
            {
                Snackbar.Add(Resource.ItemBankNameAlreadyExists, Severity.Error);

                return;
            }

            var duplicateCode = targetNode.Children.Any(x => !ReferenceEquals(x, _nodeToMove) &&
            string.Equals(x.Code?.Trim(), _nodeToMove.Code?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (duplicateCode)
            {
                Snackbar.Add(Resource.ItemBankCodeAlreadyExists, Severity.Error);

                return;
            }

            var targetDepth = GetNodeDepth(
                GeneratedTree,
                targetNode);

            if (targetDepth == -1)
            {
                Snackbar.Add(
                    Resource.UnableToDetermineNodeDepth,
                    Severity.Error);

                return;
            }

            var movingSubtreeDepth = GetSubtreeDepth(_nodeToMove);

            var finalDepth = targetDepth + movingSubtreeDepth;

            if (finalDepth > MaxDepth)
            {
                Snackbar.Add(
                    Resource.MaximumTreeDepthReached,
                    Severity.Warning);

                return;
            }

            var sibling = targetNode.Children?.FirstOrDefault(c => !ReferenceEquals(c, _nodeToMove));
            if (sibling != null)
            {
                _nodeToMove.Level = sibling.Level;
                _nodeToMove.LevelId = sibling.LevelId;
            }

            RemoveNodeFromParent(
                GeneratedTree,
                _nodeToMove);

            targetNode.Children.Add(_nodeToMove);

            Snackbar.Add(
                Resource.NodeMovedSuccessfully,
                Severity.Success);

            CancelMove();

            StateHasChanged();
        }

        private static AIItemBankNodeDto? FindNodeByNumber(AIItemBankNodeDto root, int targetNumber)
        {
            var currentNumber = 0;

            return FindNodeByNumberRecursive(
                root,
                targetNumber,
                ref currentNumber);
        }

        private static AIItemBankNodeDto? FindNodeByNumberRecursive(AIItemBankNodeDto node, int targetNumber, ref int currentNumber)
        {
            if (currentNumber == targetNumber)
            {
                return node;
            }

            currentNumber++;

            if (node.Children == null)
            {
                return null;
            }

            foreach (var child in node.Children)
            {
                var result = FindNodeByNumberRecursive(
                    child,
                    targetNumber,
                    ref currentNumber);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static bool IsDescendant(AIItemBankNodeDto node, AIItemBankNodeDto possibleDescendant)
        {
            if (node.Children == null)
            {
                return false;
            }

            foreach (var child in node.Children)
            {
                if (ReferenceEquals(child, possibleDescendant))
                {
                    return true;
                }

                if (IsDescendant(child, possibleDescendant))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool RemoveNodeFromParent(AIItemBankNodeDto parent, AIItemBankNodeDto target)
        {
            if (parent.Children == null)
            {
                return false;
            }

            if (parent.Children.Remove(target))
            {
                return true;
            }

            foreach (var child in parent.Children)
            {
                if (RemoveNodeFromParent(child, target))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task SaveItemBankTreeAsync()
        {
            if (GeneratedTree == null)
            {
                return;
            }

            _isSaving = true;

            StateHasChanged();

            try
            {
                var response = await BlazAIItemBankGenerationService.SaveGeneratedItemBankTreeAsync(GeneratedTree);

                if (response?.StatusCode == HttpStatusCode.OK)
                {
                    Snackbar.Add(
                        Resource.ItemBankSavedSuccessfully,
                        Severity.Success);

                    IsConfigurationLocked = true;

                    await IsConfigurationLockedChanged.InvokeAsync(true);
                }
                else
                {
                    Snackbar.Add(
                        response?.Message ?? Resource.FailedToSaveData,
                        Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add(
                    ex.Message,
                    Severity.Error);
            }
            finally
            {
                _isSaving = false;

                StateHasChanged();
            }
        }

        private static int GetNodeDepth(AIItemBankNodeDto currentNode, AIItemBankNodeDto targetNode, int currentDepth = 1)
        {
            if (ReferenceEquals(currentNode, targetNode))
            {
                return currentDepth;
            }

            if (currentNode.Children == null)
                return -1;

            foreach (var child in currentNode.Children)
            {
                var depth = GetNodeDepth(child, targetNode, currentDepth + 1);

                if (depth != -1)
                {
                    return depth;
                }
            }

            return -1;
        }

        private static int GetSubtreeDepth(AIItemBankNodeDto node)
        {
            if (node.Children == null ||
                node.Children.Count == 0)
            {
                return 1;
            }

            return 1 + node.Children.Max(GetSubtreeDepth);
        }
    }
}
