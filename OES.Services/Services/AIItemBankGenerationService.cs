using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OES.Helper.Dtos.AIItemBankGenerator.Request;
using OES.Helper.Dtos.AIItemBankGenerator.Response;
using OES.Helper.Dtos.Document.Request;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using OES.Services.AIFeatures.Telemetry;
using System.Net;
using System.Text.RegularExpressions;
using static OES.Helper.Dtos.Document.Response.ExtractionResult;

namespace OES.Services.Services
{
    public class AIItemBankGenerationService : IAIItemBankGenerationService
    {
        private readonly IItemBankService _itemBankService;
        private readonly IItemBankLevelService _itemBankLevelService;
        private readonly IPromptTemplate<AIItemBankGenerationPromptContextDto> _promptTemplate;
        private readonly IAIResponseGeneratorService _aIResponseGeneratorService;
        private readonly IDocumentExtractorService _documentExtractorService;
        private readonly ICommonService _commonService;
        private readonly ILogger<AIItemBankGenerationService> _logger;
        private readonly IAIResponseResultValidator<AIGeneratedItemBankWrapperDto, AIItemBankGenerationPromptContextDto> _itemBankGenerationValidator;

        private const int DefaultMaxLevelsCount = 50;
        private const int DefaultMaxChildrenPerNode = 50;
        private const int TokensPerItemBankNode = 1000;
        private const int OutputTokensSafetyBuffer = 1500;
        private const int MaxEstimatedOutputTokens = 100000;
        private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

        public AIItemBankGenerationService(
            IItemBankService itemBankService,
            IItemBankLevelService itemBankLevelService,
            IPromptTemplate<AIItemBankGenerationPromptContextDto> promptTemplate,
            IAIResponseGeneratorService aIResponseGeneratorService,
            IDocumentExtractorService documentExtractorService,
            ICommonService commonService,
            ILogger<AIItemBankGenerationService> logger,
            IAIResponseResultValidator<AIGeneratedItemBankWrapperDto, AIItemBankGenerationPromptContextDto> itemBankGenerationValidator
        )
        {
            _itemBankService = itemBankService;
            _itemBankLevelService = itemBankLevelService;
            _promptTemplate = promptTemplate;
            _aIResponseGeneratorService = aIResponseGeneratorService;
            _documentExtractorService = documentExtractorService;
            _commonService = commonService;
            _logger = logger;
            _itemBankGenerationValidator = itemBankGenerationValidator;
        }

        public async Task<ApiResponse> GenerateItemBankFromFileAsync(IFormFile file, AIItemBankGenerationConfigurationsDto configuration, CancellationToken cancellationToken = default)
        {
            if (file.Length > MiscConstants.AIQuestionsGenerationMaxFileSizeInBytes)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.FileSizeExceedsLimit,
                    null);
            }

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["FileName"] = file.FileName
            });

            _logger.LogInformation("Extracting content from uploaded file for ItemBank generation");

            await using var stream = file.OpenReadStream();

            var options = new ExtractionOptions
            {
                UseDocling = false,
                EnableOcr = true,
                OcrLanguage = MiscConstants.DefaultOcrLanguage
            };

            var extraction = await _documentExtractorService.ExtractAsync(stream, file.FileName, options, cancellationToken);

            if (!extraction.Success)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.DocumentExtractionFailed,
                    extraction.Error);
            }

            var documentText = BuildDocumentText(extraction.Content);

            return await GenerateItemBankFromTextAsync(documentText, configuration, cancellationToken);
        }

        public async Task<ApiResponse> GenerateItemBankFromTextAsync(string documentText, AIItemBankGenerationConfigurationsDto configuration, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting ItemBank tree generation from text");

                using var activity = AIActivitySource.StartItemBankGeneration(configuration.ItemBankLevelsCreation.ToString());

                var promptContext = await MapToPromptContextAsync(documentText, configuration);

                var systemPrompt = _promptTemplate.BuildSystemPrompt(promptContext);
                var userPrompt = _promptTemplate.BuildUserPrompt(promptContext);

                var maxOutputTokens = EstimateMaxOutputTokens(promptContext.MaxLevelsCount ?? DefaultMaxLevelsCount, promptContext.MaxChildrenPerNode ?? DefaultMaxChildrenPerNode);

                _logger.LogInformation("Estimated output token budget for this request: {MaxOutputTokens}", maxOutputTokens);

                var result = await _aIResponseGeneratorService.GenerateWithValidationAsync(
                    systemPrompt,
                    userPrompt,
                    promptContext,
                    _itemBankGenerationValidator,
                    images: null,
                    maxOutputTokens: maxOutputTokens,
                    maxAttempts: 3,
                    ct: cancellationToken
                );

                var levelNameToIdMap = promptContext.ExistingLevels?
                    .Where(l => !string.IsNullOrWhiteSpace(l.Name))
                    .GroupBy(l => l.Name.Trim().ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.First().Id);

                var finalTree = result?.Root != null
                    ? BuildFinalTree(result.Root, levelNameToIdMap)
                    : null;

                _logger.LogInformation("ItemBank tree generation completed");

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.ItemBankGeneratedSuccess,
                    finalTree);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ItemBank tree generation failed");
                throw;
            }
        }

        public async Task<ApiResponse> SaveGeneratedItemBankTreeAsync(AIItemBankNodeDto tree, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Saving generated ItemBank tree rooted at \"{RootName}\"", tree.Name);

            return await _itemBankService.AddItemBankTreeAsync([tree]);
        }


        #region Helper Methods

        private async Task<AIItemBankGenerationPromptContextDto> MapToPromptContextAsync(
            string documentText,
            AIItemBankGenerationConfigurationsDto configuration
        )
        {
            List<ExistingLevelOptionDto>? existingLevels = null;

            if (configuration.ItemBankLevelsCreation is ItemBankLevelsCreation.UseExisting or ItemBankLevelsCreation.Mix)
            {
                var levelsResponse = _itemBankLevelService.GetAllLevels();

                if (levelsResponse.CustomCodeStatus == CustomCodeStatus.Success && levelsResponse.Data is List<Helper.Dtos.ItemBankLevel.ItemLevelsDto> levels)
                {
                    existingLevels = levels
                        .ConvertAll(l => new ExistingLevelOptionDto { Id = l.Id, Name = l.Name });
                }
            }

            var promptContext = new AIItemBankGenerationPromptContextDto
            {
                DocumentText = documentText,
                ItemBankLevelsCreation = configuration.ItemBankLevelsCreation,
                MaxLevelsCount = configuration.MaxLevelsCount is > 0
                    ? configuration.MaxLevelsCount.Value
                    : DefaultMaxLevelsCount,
                MaxChildrenPerNode = configuration.MaxChildrenPerNode is > 0
                    ? configuration.MaxChildrenPerNode.Value
                    : DefaultMaxChildrenPerNode,
                ExistingLevels = existingLevels,
                LanguageDto = configuration.LanguageDto
            };

            return await Task.FromResult(promptContext);
        }

        private static AIItemBankNodeDto BuildFinalTree(AIGeneratedItemBankNodeDto aiNode, Dictionary<string, long>? levelNameToIdMap = null)
        {
            long levelId = aiNode.LevelId ?? 0;

            if (levelId == 0 && !string.IsNullOrWhiteSpace(aiNode.LevelName) && levelNameToIdMap != null && levelNameToIdMap.TryGetValue(aiNode.LevelName.Trim().ToLowerInvariant(), out var mappedId))
            {
                levelId = mappedId;
            }

            var node = new AIItemBankNodeDto
            {
                Name = aiNode.Name?.Trim(),
                Code = GenerateCodeFromName(aiNode.Name),
                Description = aiNode.Description ?? string.Empty,
                Hours = aiNode.Hours,
                Level = aiNode.LevelName?.Trim(),
                LevelId = levelId
            };

            foreach (var child in aiNode.Children ?? [])
            {
                node.Children.Add(BuildFinalTree(child, levelNameToIdMap));
            }

            return node;
        }

        private static string GenerateCodeFromName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            return WhitespaceRegex.Replace(name.Trim(), "-");
        }

        private static string BuildDocumentText(IReadOnlyList<ExtractedContent> content)
        {
            var builder = new System.Text.StringBuilder();

            foreach (var item in content)
            {
                if (!string.IsNullOrWhiteSpace(item.Content))
                {
                    builder.AppendLine(item.Content);
                }

                builder.AppendLine();
            }

            return builder.ToString().Trim();
        }

        private static int EstimateMaxOutputTokens(int maxLevelsCount, int maxChildrenPerNode)
        {
            long estimatedNodeCount = 0;
            long nodesAtCurrentLevel = 1;

            for (var level = 0; level < maxLevelsCount; level++)
            {
                estimatedNodeCount += nodesAtCurrentLevel;

                if (estimatedNodeCount * TokensPerItemBankNode >= MaxEstimatedOutputTokens)
                {
                    estimatedNodeCount = MaxEstimatedOutputTokens / TokensPerItemBankNode;
                    break;
                }

                nodesAtCurrentLevel *= maxChildrenPerNode;
            }

            var estimatedTokens = (int)(estimatedNodeCount * TokensPerItemBankNode) + OutputTokensSafetyBuffer;

            return Math.Min(estimatedTokens, MaxEstimatedOutputTokens);
        }

        #endregion
    }
}