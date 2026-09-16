using OES.Helper.Dtos.AIItemBankGenerator.Request;
using OES.Helper.Dtos.AIItemBankGenerator.Response;
using OES.Helper.Dtos.Validation;
using OES.Helper.Enums;
using OES.Interface.Interfaces;

namespace OES.Services.Validators
{
    public class ItemBankGenerationValidator : IAIResponseResultValidator<AIGeneratedItemBankWrapperDto, AIItemBankGenerationPromptContextDto>
    {
        public AIResponseValidationResult Validate(AIGeneratedItemBankWrapperDto result, AIItemBankGenerationPromptContextDto context)
        {
            if (result?.Root is null || string.IsNullOrWhiteSpace(result.Root.Name))
            {
                return AIResponseValidationResult.Fail("The response must contain exactly one root node with a non-empty \"name\".");
            }

            var errors = new List<string>();

            HashSet<string>? existingLevelNames = context.ItemBankLevelsCreation == ItemBankLevelsCreation.UseExisting
                ? [.. (context.ExistingLevels ?? []).Select(l => l.Name.Trim().ToLowerInvariant())]
                    : null;

            var levelNamesByDepth = new Dictionary<int, HashSet<string>>();

            ValidateNode(
                result.Root,
                depth: 1,
                path: "root",
                context,
                existingLevelNames,
                levelNamesByDepth,
                errors);

            foreach (var (depth, namesAtDepth) in levelNamesByDepth)
            {
                if (namesAtDepth.Count > 1)
                {
                    errors.Add(
                        $"Depth {depth} uses different levelName values ({string.Join(", ", namesAtDepth.Select(n => $"\"{n}\""))}). All nodes at the same depth must share the exact same levelName.");
                }
            }

            return errors.Count == 0
                ? AIResponseValidationResult.Success()
                : AIResponseValidationResult.Fail(errors);
        }

        private static void ValidateNode(
            AIGeneratedItemBankNodeDto node,
            int depth,
            string path,
            AIItemBankGenerationPromptContextDto context,
            HashSet<string>? existingLevelNames,
            Dictionary<int, HashSet<string>> levelNamesByDepth,
            List<string> errors)
        {
            if (depth > context.MaxLevelsCount)
            {
                errors.Add(
                    $"{path} - Node \"{node.Name}\" is at depth {depth}, exceeding the max allowed depth of {context.MaxLevelsCount}.");

                return;
            }

            if (string.IsNullOrWhiteSpace(node.Name))
            {
                errors.Add(
                    $"{path} - A node at depth {depth} has an empty \"name\".");
            }

            if (string.IsNullOrWhiteSpace(node.LevelName))
            {
                errors.Add(
                    $"{path} - Node \"{node.Name}\" at depth {depth} has an empty \"levelName\".");
            }
            else
            {
                var normalizedLevelName = node.LevelName.Trim().ToLowerInvariant();

                if (existingLevelNames != null && !existingLevelNames.Contains(normalizedLevelName))
                {
                    errors.Add(
                        $"{path} - Node \"{node.Name}\" uses levelName \"{node.LevelName}\" which is not one of the supplied EXISTING LEVELS.");
                }

                if (!levelNamesByDepth.TryGetValue(
                        depth,
                        out var namesAtDepth))
                {
                    namesAtDepth = [];
                    levelNamesByDepth[depth] = namesAtDepth;
                }

                namesAtDepth.Add(node.LevelName.Trim());
            }

            if (node.Hours < 0)
            {
                errors.Add(
                    $"{path} - Node \"{node.Name}\" has a negative \"hours\" value ({node.Hours}).");
            }

            var children = node.Children ?? [];

            if (children.Count > context.MaxChildrenPerNode)
            {
                errors.Add(
                    $"{path} - Node \"{node.Name}\" has {children.Count} children, exceeding the max allowed of {context.MaxChildrenPerNode}.");
            }

            // Important hard rule:
            // a node at MaxLevelsCount must always be a leaf.
            if (depth == context.MaxLevelsCount)
            {
                if (children.Count > 0)
                {
                    errors.Add(
                        $"{path} - Node \"{node.Name}\" is already at the maximum " +
                        $"allowed depth ({context.MaxLevelsCount}) but still has " +
                        $"{children.Count} child node(s). It must have children = [].");
                }

                return;
            }

            if (children.Count > 1)
            {
                var duplicateNames = children
                    .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                    .GroupBy(c => c.Name.Trim().ToLowerInvariant())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateNames.Count > 0)
                {
                    errors.Add(
                        $"{path} - Node \"{node.Name}\" has sibling children " +
                        $"with duplicate/near-identical names: " +
                        $"{string.Join(", ", duplicateNames)}.");
                }
            }

            for (var i = 0; i < children.Count; i++)
            {
                ValidateNode(
                    children[i],
                    depth + 1,
                    $"{path}.children[{i}]",
                    context,
                    existingLevelNames,
                    levelNamesByDepth,
                    errors);
            }
        }
    }
}