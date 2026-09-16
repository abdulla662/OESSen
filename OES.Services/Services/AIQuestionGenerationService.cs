using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OES.Helper.Dtos.AIQuestionGenerator.Request;
using OES.Helper.Dtos.AIQuestionGenerator.Response;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.Dtos.Document.Request;
using OES.Helper.Dtos.UploadFiles;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.General.DocLibBackEndHttpClientHelper;
using OES.Helper.General.NewApiResponse;
using OES.Helper.RegularExpressions;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using OES.Services.AIFeatures.Telemetry;
using SharedHelper.General;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static OES.Helper.Dtos.Document.Response.ExtractionResult;
using QuestionType = OES.Helper.Enums.QuestionType;

namespace OES.Services.Services
{
    public class AIQuestionGenerationService : IAIQuestionGenerationService
    {
        private readonly IQuestionService _questionService;
        private readonly IPromptTemplate<AIQuestionGenerationPromptContextDto> _promptTemplate;
        private readonly IAIResponseGeneratorService _aIResponseGeneratorService;
        private readonly IDocumentExtractorService _documentExtractorService;
        private readonly ICommonService _commonService;
        private readonly ILogger<AIQuestionGenerationService> _logger;
        private readonly IDifficultyLevelService _difficultyLevelService;
        private readonly IQuestionLayoutService _questionLayoutService;
        private readonly IAIResponseResultValidator<AIGeneratedQuestionsResultDto, AIQuestionGenerationPromptContextDto> _questionGenerationValidator;
        private readonly DocLibBackEndHttpClientHelper _docLibBackEndHttpClient;
        private readonly AIAssetStorage _aiAssetStorage;
        private readonly MathContentProcessingService _mathContentProcessingService;
        private const int PerceptualHashSize = 8;
        private const int PerceptualHashSimilarityThreshold = 5;
        private const string ImageUsageInstruction = "Now generate the exam questions per all the instructions above. Use the IMAGE_IDs exactly as labeled with each image above.";
        private const int TokensPerQuestion = 3000;
        private const int ImageQuestionBufferPerQuestion = 150;
        private const int OutputTokensSafetyBuffer = 1500;

        public AIQuestionGenerationService(
            IQuestionService questionService,
            IPromptTemplate<AIQuestionGenerationPromptContextDto> promptTemplate,
            IAIResponseGeneratorService aIResponseGeneratorService,
            IDocumentExtractorService documentExtractorService,
            ICommonService commonService,
            ILogger<AIQuestionGenerationService> logger,
            IDifficultyLevelService difficultyLevelService,
            IQuestionLayoutService questionLayoutService,
            IAIResponseResultValidator<AIGeneratedQuestionsResultDto, AIQuestionGenerationPromptContextDto> questionGenerationValidator,
            DocLibBackEndHttpClientHelper docLibBackEndHttpClient,
            AIAssetStorage aiAssetStorage,
            MathContentProcessingService mathContentProcessingService)
        {
            _questionService = questionService;
            _promptTemplate = promptTemplate;
            _aIResponseGeneratorService = aIResponseGeneratorService;
            _documentExtractorService = documentExtractorService;
            _commonService = commonService;
            _logger = logger;
            _difficultyLevelService = difficultyLevelService;
            _questionLayoutService = questionLayoutService;
            _questionGenerationValidator = questionGenerationValidator;
            _docLibBackEndHttpClient = docLibBackEndHttpClient;
            _aiAssetStorage = aiAssetStorage;
            _mathContentProcessingService = mathContentProcessingService;
        }

        public async Task<ApiResponse> GenerateQuestionsFromFileAsync(IFormFile file, AIQuestionGenerationConfigurationsDto configuration, CancellationToken cancellationToken = default)
        {
            if (file.Length > MiscConstants.AIQuestionsGenerationMaxFileSizeInBytes)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.FileSizeExceedsLimit,
                    null
                );
            }

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["FileName"] = file.FileName,
                ["ItemBankId"] = configuration.ItemBank.Id
            });

            _logger.LogInformation("Extracting content from uploaded file");

            await using var stream = file.OpenReadStream();

            var options = new ExtractionOptions
            {
                UseDocling = true,
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

            var imageCandidates = FilterImageCandidatesForAI(extraction.Content);

            var allowedImageIds = imageCandidates.Select(i => i.DocumentId).ToHashSet();

            var documentText = BuildDocumentText(extraction.Content, allowedImageIds);

            return await GenerateQuestionsFromTextAsync(documentText, configuration, imageCandidates, cancellationToken);
        }

        public async Task<ApiResponse> GenerateQuestionsFromTextAsync(string documentText, AIQuestionGenerationConfigurationsDto configuration, List<ExtractedImage>? imageCandidates = null, CancellationToken cancellationToken = default)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["ItemBankId"] = configuration.ItemBank.Id,
                ["LanguageId"] = configuration.Language.Id,
            });

            using var activity = AIActivitySource.StartQuestionGeneration(configuration.ItemBank.Id);

            try
            {
                _logger.LogInformation("Starting question generation from text");

                var promptContext = await MapToPromptContextAsync(documentText, configuration, imageCandidates);

                var systemPrompt = _promptTemplate.BuildSystemPrompt(promptContext);
                var userPrompt = _promptTemplate.BuildUserPrompt(promptContext);

                var maxOutputTokens = EstimateMaxOutputTokens(promptContext.QuestionTypes, promptContext.HasImages);

                _logger.LogInformation("Estimated output token budget for this request: {MaxOutputTokens}", maxOutputTokens);

                var result = await _aIResponseGeneratorService.GenerateWithValidationAsync(systemPrompt,
                    userPrompt,
                    promptContext,
                    _questionGenerationValidator,
                    imageCandidates,
                    additionalImageInstruction: ImageUsageInstruction,
                    maxOutputTokens: maxOutputTokens,
                    maxAttempts: 3,
                    ct: cancellationToken
                );

                if (result?.Questions != null)
                {
                    PopulateQuestionMetadata(result.Questions, configuration);
                    RegenerateUniqueQuestionCodes(result.Questions, configuration);
                    await AssignLayoutIdsAsync(result.Questions);
                    _mathContentProcessingService.ProcessMathContent(result.Questions, configuration.Language.Name);
                    WrapGeneratedQuestionsWithDirectionTags(result.Questions);
                }

                var questionCount = result?.Questions.Count ?? 0;

                activity?.SetTag("ai.questions.generated", questionCount);
                activity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogInformation("Question generation completed. Generated {Count} questions", questionCount);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.QuestionsGeneratedSuccess,
                    result);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        public async Task<ApiResponse> SaveGeneratedQuestionsAsync(AIGeneratedQuestionsResultDto result, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Saving {Count} generated questions", result.Questions.Count);

            await ProcessTempImagesInQuestionsAsync(result.Questions);

            return await _questionService.SaveAIGeneratedQuestionsAsync(result.Questions);
        }


        #region Helper Methods

        private static int EstimateMaxOutputTokens(IReadOnlyCollection<AIQuestionTypeCountDto> questionTypes, bool hasImages)
        {
            var totalQuestionCount = questionTypes.Sum(d => d.Count);

            var tokensPerQuestion = TokensPerQuestion +
                (hasImages ? ImageQuestionBufferPerQuestion : 0) +
                OutputTokensSafetyBuffer;

            return totalQuestionCount * tokensPerQuestion;
        }

        private static void PopulateQuestionMetadata(List<AIQuestionMetadataDto> questions, AIQuestionGenerationConfigurationsDto configuration)
        {
            foreach (var question in questions)
            {
                question.ItemBankId = configuration.ItemBank.Id;
                question.ItemBankName = configuration.ItemBank.Name;
                question.SubjectId = configuration.Subject.Id;
                question.SubjectName = configuration.Subject.Name;
                question.CategoryId = configuration.Category.Id;
                question.CategoryName = configuration.Category.Name;
                question.DifficultyProfileId = configuration.DifficultyProfile.Id;
                question.IloId = configuration.Ilo?.Id;
                question.IloName = configuration.Ilo?.Name;
                question.Author = configuration.Author;
                question.QuestionsExhaustionCount = configuration.QuestionsExhaustionCount;
                question.ScientificEditorPanelEnabled = configuration.ScientificEditorPanelEnabled;
                question.FileManagerEditorPanelEnabled = configuration.FileManagerEditorPanelEnabled;
                question.QuestionsExhaustionCount = configuration.QuestionsExhaustionCount;

                if (question.Details != null)
                {
                    foreach (var detail in question.Details)
                    {
                        detail.LanguageId = configuration.Language.Id;
                        detail.LanguageName = configuration.Language.Name;
                    }
                }
            }
        }

        private static void RegenerateUniqueQuestionCodes(List<AIQuestionMetadataDto> questions, AIQuestionGenerationConfigurationsDto configuration)
        {
            foreach (var question in questions)
            {
                var uniqueSuffix = DateTime.UtcNow.Ticks.ToString("X");
                question.Code = $"AIQ-{configuration.ItemBank.Id}-{configuration.Subject.Id}-{question.QuestionTypeId}-{uniqueSuffix}";
            }
        }

        private async Task<AIQuestionGenerationPromptContextDto> MapToPromptContextAsync(string documentText, AIQuestionGenerationConfigurationsDto configuration, List<ExtractedImage>? extractedImages = null)
        {
            var difficultyLevels = await GetDifficultyLevelsAsync(configuration.DifficultyProfile.Id);

            return new AIQuestionGenerationPromptContextDto
            {
                DocumentText = documentText,
                Language = configuration.Language,
                Category = configuration.Category,
                Subject = configuration.Subject,
                ItemBank = configuration.ItemBank,
                Ilo = configuration.Ilo,
                DifficultyProfile = configuration.DifficultyProfile,
                QuestionTypes = configuration.QuestionTypes,
                RequireAnswersFromDocumentOnly = configuration.RequireAnswersFromDocumentOnly,
                GeneratePlausibleDistractors = configuration.GeneratePlausibleDistractors,
                Author = configuration.Author,
                QuestionsExhaustionCount = configuration.QuestionsExhaustionCount,
                ScientificEditorPanelEnabled = configuration.ScientificEditorPanelEnabled,
                FileManagerEditorPanelEnabled = configuration.FileManagerEditorPanelEnabled,
                DifficultyLevels = difficultyLevels,
                ImageCandidates = extractedImages,
                HasImages = extractedImages is { Count: > 0 }
            };
        }

        private async Task<List<DifficultyLevelDto>> GetDifficultyLevelsAsync(long difficultyProfileId)
        {
            var response = await _difficultyLevelService.GetAllDifficultyLevelsByProfileIdAsync(difficultyProfileId);

            if (response.CustomCodeStatus != CustomCodeStatus.Success)
            {
                return [];
            }

            return (List<DifficultyLevelDto>)response.Data ?? [];
        }

        private async Task AssignLayoutIdsAsync(List<AIQuestionMetadataDto> questions)
        {
            var questionTypeIds = questions.Select(q => q.QuestionTypeId).Distinct().ToList();

            var layoutMap = await _questionLayoutService.GetDefaultLayoutIdsByQuestionTypeIdsAsync(questionTypeIds);

            foreach (var question in questions)
            {
                if (layoutMap.TryGetValue(question.QuestionTypeId, out var layout))
                {
                    question.LayoutId = layout.Id;
                    question.LayoutName = layout.Name;
                }
            }
        }

        private static string BuildDocumentText(IReadOnlyList<ExtractedContent> content, IReadOnlyCollection<Guid> allowedImageIds)
        {
            var builder = new StringBuilder();

            foreach (var item in content)
            {
                switch (item.Type)
                {
                    case ExtractedContentType.Image:
                        if (item.Image is not null && allowedImageIds.Contains(item.Image.DocumentId))
                        {
                            builder.AppendLine($"[[image:{item.Image.DocumentId}]]");
                        }
                        break;

                    case ExtractedContentType.Table:
                        builder.AppendLine(item.Content);
                        break;

                    default:
                        if (!string.IsNullOrWhiteSpace(item.Content))
                        {
                            builder.AppendLine(item.Content);
                        }
                        break;
                }

                builder.AppendLine();
            }

            return builder.ToString().Trim();
        }

        private List<ExtractedImage> FilterImageCandidatesForAI(IReadOnlyList<ExtractedContent> content)
        {
            var images = content
                .Where(x => x.Type == ExtractedContentType.Image && x.Image is not null)
                .Select(x => x.Image!)
                .Where(IsImageCandidateForAI)
                .ToList();

            if (images.Count <= 1)
            {
                return images;
            }

            var hashesByImageId = images.ToDictionary(image => image.DocumentId, image => ComputeImageHashes(image.DocumentId));

            var sha256Counts = new Dictionary<string, int>();

            foreach (var hashes in hashesByImageId.Values)
            {
                if (hashes.Sha256Hash is { } hash)
                {
                    sha256Counts[hash] = sha256Counts.GetValueOrDefault(hash) + 1;
                }
            }

            var afterExactDuplicates = images
                .Where(image =>
                {
                    var hash = hashesByImageId[image.DocumentId].Sha256Hash;
                    return hash is null || sha256Counts[hash] == 1;
                })
                .ToList();

            if (afterExactDuplicates.Count <= 1)
            {
                return afterExactDuplicates;
            }

            var result = new List<ExtractedImage>();
            var consumed = new HashSet<Guid>();

            foreach (var image in afterExactDuplicates)
            {
                if (!consumed.Add(image.DocumentId))
                {
                    continue;
                }

                result.Add(image);

                var hashA = hashesByImageId[image.DocumentId].AverageHash;
                if (hashA is null)
                {
                    continue;
                }

                foreach (var other in afterExactDuplicates)
                {
                    if (consumed.Contains(other.DocumentId))
                    {
                        continue;
                    }

                    var hashB = hashesByImageId[other.DocumentId].AverageHash;
                    if (hashB is null)
                    {
                        continue;
                    }

                    if (HammingDistance(hashA, hashB) <= PerceptualHashSimilarityThreshold)
                    {
                        consumed.Add(other.DocumentId);
                    }
                }
            }

            return result;
        }

        private ImageHashes ComputeImageHashes(Guid imageId)
        {
            try
            {
                var asset = _aiAssetStorage.Get(imageId);
                if (asset?.Data is null || asset.Data.Length == 0)
                {
                    return default;
                }

                string? sha256Hash = null;
                try
                {
                    sha256Hash = Convert.ToHexString(SHA256.HashData(asset.Data));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to compute content hash for AI asset {ImageId}", imageId);
                }

                var averageHash = TryComputeAverageHash(asset.Data, imageId);

                return new ImageHashes(sha256Hash, averageHash);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load AI asset {ImageId} for hashing", imageId);
                return default;
            }
        }

        private string? TryComputeAverageHash(byte[] imageBytes, Guid imageId)
        {
            try
            {
                using var image = Image.Load<L8>(imageBytes);
                image.Mutate(x => x.Resize(PerceptualHashSize, PerceptualHashSize));

                const int pixelCount = PerceptualHashSize * PerceptualHashSize;
                var pixels = new byte[pixelCount];
                var index = 0;
                long sum = 0;

                image.ProcessPixelRows(accessor =>
                {
                    for (var y = 0; y < accessor.Height; y++)
                    {
                        var row = accessor.GetRowSpan(y);
                        for (var x = 0; x < row.Length; x++)
                        {
                            pixels[index] = row[x].PackedValue;
                            sum += row[x].PackedValue;
                            index++;
                        }
                    }
                });

                var average = sum / pixelCount;
                var bits = new char[pixelCount];
                for (var i = 0; i < pixelCount; i++)
                {
                    bits[i] = pixels[i] >= average ? '1' : '0';
                }

                return new string(bits);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to compute average hash for AI asset {ImageId}", imageId);
                return null;
            }
        }

        private static int HammingDistance(string hashA, string hashB)
        {
            var length = Math.Min(hashA.Length, hashB.Length);
            var distance = Math.Abs(hashA.Length - hashB.Length);

            for (var i = 0; i < length; i++)
            {
                if (hashA[i] != hashB[i])
                {
                    distance++;
                }
            }

            return distance;
        }

        private static bool IsImageCandidateForAI(ExtractedImage image)
        {
            const int minWidth = 150;
            const int minHeight = 150;
            const double maxAspectRatio = 3.0;

            if (image.Width < minWidth || image.Height < minHeight)
            {
                return false;
            }

            var aspectRatio = (double)Math.Max(image.Width, image.Height) / Math.Min(image.Width, image.Height);

            return aspectRatio <= maxAspectRatio;
        }

        private async Task ProcessTempImagesInQuestionsAsync(List<AIQuestionMetadataDto> questions)
        {
            if (questions == null || questions.Count == 0) return;

            var replacedImages = new HashSet<Guid>();

            foreach (var question in questions)
            {
                if (question.Details == null) continue;

                foreach (var detail in question.Details)
                {
                    if (!string.IsNullOrWhiteSpace(detail.Body))
                    {
                        detail.Body = await ReplaceTempImagesWithDocLibUrlsAsync(detail.Body, replacedImages);
                        detail.Body = await ReplaceTempLatexObjectsWithDocLibUrlsAsync(detail.Body, replacedImages);
                    }

                    if (!string.IsNullOrWhiteSpace(detail.Instructions))
                    {
                        detail.Instructions = await ReplaceTempImagesWithDocLibUrlsAsync(detail.Instructions, replacedImages);
                        detail.Instructions = await ReplaceTempLatexObjectsWithDocLibUrlsAsync(detail.Instructions, replacedImages);
                    }

                    if (!string.IsNullOrWhiteSpace(detail.ModelAnswer))
                    {
                        detail.ModelAnswer = await ReplaceTempImagesWithDocLibUrlsAsync(detail.ModelAnswer, replacedImages);
                        detail.ModelAnswer = await ReplaceTempLatexObjectsWithDocLibUrlsAsync(detail.ModelAnswer, replacedImages);
                    }

                    if (detail.Choices != null)
                    {
                        foreach (var choice in detail.Choices)
                        {
                            if (!string.IsNullOrWhiteSpace(choice.Text))
                            {
                                choice.Text = await ReplaceTempImagesWithDocLibUrlsAsync(choice.Text, replacedImages);
                                choice.Text = await ReplaceTempLatexObjectsWithDocLibUrlsAsync(choice.Text, replacedImages);
                            }
                        }
                    }
                }
            }

            foreach (var imageId in replacedImages)
            {
                _aiAssetStorage.Remove(imageId);
            }
        }

        private async Task<string> ReplaceTempImagesWithDocLibUrlsAsync(string content, HashSet<Guid> replacedImages)
        {
            if (string.IsNullOrWhiteSpace(content)) return content;

            var matches = Regex.Matches(content, RegularExpressions.TempImageUrlRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            if (matches.Count == 0) return content;

            var sb = new StringBuilder();
            int lastIndex = 0;

            foreach (Match match in matches)
            {
                sb.Append(content, lastIndex, match.Index - lastIndex);

                var guidString = match.Groups[2].Value;

                if (Guid.TryParse(guidString, out Guid imageId))
                {
                    var tempImage = _aiAssetStorage.Get(imageId);

                    if (tempImage != null && tempImage.Data != null)
                    {
                        try
                        {
                            var fileExtension = GetExtensionFromMimeType(tempImage.ContentType);
                            var fileName = $"{Guid.NewGuid()}{fileExtension}";

                            var imageData = new MediaFileDataDto
                            {
                                Content = tempImage.Data,
                                Size = tempImage.Data.Length,
                                Name = fileName,
                                Type = tempImage.ContentType
                            };

                            var uploadResult = await _docLibBackEndHttpClient.PostAsJsonAsync<MediaFileDataDto, NewApiResponse<DocumentUrlFileResponseDto>>(
                                MiscConstants.UploadDocumentContent,
                                imageData
                            );

                            if (uploadResult != null && uploadResult.Success && uploadResult.Data != null)
                            {
                                var docLibUrl = $"{CentralizedUrlHelper.DocLibApiBaseUrl.TrimEnd('/')}/{uploadResult.Data.FileRelativeUrl.TrimStart('/')}";
                                sb.Append(docLibUrl);
                                replacedImages.Add(imageId);
                            }
                            else
                            {
                                _logger.LogWarning("DocLib upload did not succeed for AI asset {ImageId}. Success={Success}, HasData={HasData}. Leaving temp URL in place.", imageId, uploadResult?.Success, uploadResult?.Data != null);
                                sb.Append(match.Value);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to upload image {ImageId} from AIAssetStorage to DocLib", imageId);
                            sb.Append(match.Value);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("AI asset image {ImageId} not found in storage", imageId);
                        sb.Append(match.Value);
                    }
                }
                else
                {
                    sb.Append(match.Value);
                }

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < content.Length)
            {
                sb.Append(content, lastIndex, content.Length - lastIndex);
            }

            return sb.ToString();
        }

        private async Task<string> ReplaceTempLatexObjectsWithDocLibUrlsAsync(string content, HashSet<Guid> replacedImages)
        {
            if (string.IsNullOrWhiteSpace(content)) return content;

            var matches = Regex.Matches(content, RegularExpressions.TempLatexObjectPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (matches.Count == 0) return content;

            var sb = new StringBuilder();
            var lastIndex = 0;

            foreach (Match match in matches)
            {
                sb.Append(content, lastIndex, match.Index - lastIndex);

                var guidString = match.Groups["guid"].Value;
                var latexAttr = match.Groups["latexattr"].Value;

                if (Guid.TryParse(guidString, out var imageId))
                {
                    var tempImage = _aiAssetStorage.Get(imageId);

                    if (tempImage?.Data != null)
                    {
                        try
                        {
                            var fileName = $"{Guid.NewGuid()}.png";
                            var imageData = new MediaFileDataDto
                            {
                                Content = tempImage.Data,
                                Size = tempImage.Data.Length,
                                Name = fileName,
                                Type = tempImage.ContentType
                            };

                            var uploadResult = await _docLibBackEndHttpClient.PostAsJsonAsync<MediaFileDataDto, NewApiResponse<DocumentUrlFileResponseDto>>(
                                MiscConstants.UploadDocumentContent, imageData);

                            if (uploadResult is { Success: true, Data: not null })
                            {
                                var docLibUrl = $"{CentralizedUrlHelper.DocLibApiBaseUrl.TrimEnd('/')}/{uploadResult.Data.FileRelativeUrl.TrimStart('/')}";

                                sb.Append(
                                    $"<br><object data=\"{docLibUrl}\" type=\"image/png\" class=\"equation-img\" " +
                                    $"data-latex=\"{latexAttr}\" contenteditable=\"false\" unselectable=\"on\" " +
                                    $"style=\"display: inline-block; vertical-align: middle; height: 45px; width: auto;\"></object><br>");

                                replacedImages.Add(imageId);
                            }
                            else
                            {
                                _logger.LogWarning("DocLib upload did not succeed for LaTeX asset {ImageId}. Success={Success}, HasData={HasData}. Leaving temp object in place.", imageId, uploadResult?.Success, uploadResult?.Data != null);
                                sb.Append(match.Value);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to upload LaTeX image {ImageId} to DocLib", imageId);
                            sb.Append(match.Value);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("LaTeX asset image {ImageId} not found in storage", imageId);
                        sb.Append(match.Value);
                    }
                }
                else
                {
                    sb.Append(match.Value);
                }

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < content.Length)
            {
                sb.Append(content, lastIndex, content.Length - lastIndex);
            }

            return sb.ToString();
        }

        private static string GetExtensionFromMimeType(string mimeType)
        {
            return mimeType.ToLowerInvariant() switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpeg",
                "image/jpg" => ".jpg",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                "image/svg+xml" => ".svg",
                _ => ".png"
            };
        }

        private void WrapGeneratedQuestionsWithDirectionTags(List<AIQuestionMetadataDto> questions)
        {
            var isRtl = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            var direction = isRtl ? MiscConstants.Rtl : MiscConstants.Ltr;

            foreach (var question in questions)
            {
                var isTargetType = question.QuestionTypeId == (long)QuestionType.Essay ||
                                   question.QuestionTypeId == (long)QuestionType.MCQ ||
                                   question.QuestionTypeId == (long)QuestionType.TrueAndFalse;

                var isMcq = question.QuestionTypeId == (long)QuestionType.MCQ;

                if (question.Details != null)
                {
                    foreach (var detail in question.Details)
                    {
                        detail.Body = GetWrappedContent(detail.Body, direction, isTargetType);
                        detail.ModelAnswer = GetWrappedContent(detail.ModelAnswer, direction, isTargetType);

                        if (detail.Choices != null)
                        {
                            foreach (var choice in detail.Choices)
                            {
                                choice.Text = GetWrappedContent(choice.Text, direction, isMcq);
                            }
                        }
                    }
                }
            }
        }

        private static string GetWrappedContent(string content, string direction, bool wrap)
        {
            if (!wrap || string.IsNullOrWhiteSpace(content))
            {
                return content;
            }

            var trimmed = content.Trim();
            if ((trimmed.StartsWith($"<div style='direction: {direction}'><p>", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith($"<div style=\"direction: {direction}\"><p>", StringComparison.OrdinalIgnoreCase)) &&
                trimmed.EndsWith("</p></div>", StringComparison.OrdinalIgnoreCase))
            {
                return content;
            }

            return $"<div style='direction: {direction}'><p>{content}</p></div>";
        }

        private readonly record struct ImageHashes(string? Sha256Hash, string? AverageHash);

        #endregion
    }
}