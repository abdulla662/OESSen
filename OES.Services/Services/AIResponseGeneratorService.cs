using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OES.Helper.General;
using OES.Interface.Interfaces;
using OES.Services.AIFeatures.Telemetry;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static OES.Helper.Dtos.Document.Response.ExtractionResult;

namespace OES.Services.Services
{
    public class AIResponseGeneratorService : IAIResponseGeneratorService
    {
        private readonly IChatClient _chatClient;
        private readonly AISettings _settings;
        private readonly ILogger<AIResponseGeneratorService> _logger;
        private readonly AIAssetStorage _aiAssetStorage;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public AIResponseGeneratorService(IChatClient chatClient, IOptions<AISettings> aiSettings, ILogger<AIResponseGeneratorService> logger, AIAssetStorage aiAssetStorage)
        {
            _chatClient = chatClient;
            _logger = logger;
            _settings = aiSettings.Value;
            _aiAssetStorage = aiAssetStorage;
        }

        private static int EstimateTokenCount(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return (int)Math.Ceiling(text.Length / 3.0);
        }

        public async Task<T?> GenerateAsync<T>(
            string systemPrompt,
            string userPrompt,
            IEnumerable<ExtractedImage>? images = null,
            string? additionalImageInstruction = null,
            int? maxOutputTokens = null,
            CancellationToken ct = default) where T : class
        {
            var sw = Stopwatch.StartNew();

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["OutputType"] = typeof(T).Name
            });

            using var activity = AIActivitySource.StartStructuredOutputGeneration(typeof(T).Name);

            try
            {
                _logger.LogInformation("Starting structured output generation");

                var messages = BuildMessages(systemPrompt, userPrompt, images, additionalImageInstruction);
                var chatOptions = BuildChatOptions(systemPrompt, userPrompt, maxOutputTokens);

                var response = await _chatClient.GetResponseAsync<T>(messages, chatOptions, useJsonSchemaResponseFormat: true, cancellationToken: ct);

                activity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogInformation("Structured output generation completed");

                AIMetrics.Requests.Add(1, new KeyValuePair<string, object?>("output_type", typeof(T).Name));

                return response.Result;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
            finally
            {
                sw.Stop();

                AIMetrics.RequestDuration.Record(sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("output_type", typeof(T).Name));
            }
        }

        public async Task<T?> GenerateWithFallbackAsync<T>(
            string systemPrompt,
            string userPrompt,
            IEnumerable<ExtractedImage>? images = null,
            string? additionalImageInstruction = null,
            int? maxOutputTokens = null,
            CancellationToken ct = default) where T : class
        {
            try
            {
                var response = await GenerateAsync<T>(systemPrompt, userPrompt, images, additionalImageInstruction, maxOutputTokens, ct);

                return response;
            }
            catch (Exception ex) when (ex is NotSupportedException or JsonException or InvalidOperationException)
            {
                var messages = BuildMessages(systemPrompt, userPrompt, images, additionalImageInstruction);

                var chatOptions = BuildChatOptions(systemPrompt, userPrompt, maxOutputTokens);

                var response = await _chatClient.GetResponseAsync(messages, chatOptions, cancellationToken: ct);

                var json = ExtractJson(response.Text);

                var result = JsonSerializer.Deserialize<T>(json, JsonOptions);

                return result;
            }
        }

        public async Task<TResult> GenerateWithValidationAsync<TResult, TContext>(
            string systemPrompt,
            string userPrompt,
            TContext context,
            IAIResponseResultValidator<TResult, TContext> validator,
            IEnumerable<ExtractedImage>? images = null,
            string? additionalImageInstruction = null,
            int? maxOutputTokens = null,
            int maxAttempts = 3,
            CancellationToken ct = default) where TResult : class
        {
            if (maxAttempts < 1) maxAttempts = 1;

            var originalUserPrompt = userPrompt;

            TResult? lastResult = null;
            List<string> lastErrors = [];

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["OutputType"] = typeof(TResult).Name,
                ["ValidatorType"] = validator.GetType().Name
            });

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                _logger.LogInformation("Validated generation attempt {Attempt}/{MaxAttempts}", attempt, maxAttempts);

                var userPromptForAttempt = attempt == 1
                    ? originalUserPrompt
                    : originalUserPrompt + BuildCompactFeedback(lastErrors);

                TResult? result;

                try
                {
                    result = await GenerateWithFallbackAsync<TResult>(systemPrompt, userPromptForAttempt, images, additionalImageInstruction, maxOutputTokens, ct) ?? throw new JsonException("AI returned no parsable result.");
                }
                catch (Exception ex) when (ex is NotSupportedException or JsonException)
                {
                    lastErrors = [ex.Message];
                    _logger.LogWarning(ex, "Attempt {Attempt} failed to produce parsable output", attempt);
                    continue;
                }

                lastResult = result;

                var validation = validator.Validate(result, context);
                if (validation.IsValid)
                {
                    _logger.LogInformation("Validation passed on attempt {Attempt}", attempt);
                    return result;
                }

                _logger.LogWarning("Validation failed on attempt {Attempt}: {Errors}", attempt, string.Join(" | ", validation.Errors));

                lastErrors = [.. validation.Errors];
            }

            _logger.LogError(
                "AI output failed validation after {MaxAttempts} attempts. Last errors: {Errors}",
                maxAttempts,
                string.Join(" | ", lastErrors));

            if (lastResult is null)
            {
                throw new InvalidOperationException(
                    $"AI did not produce any parsable output after {maxAttempts} attempts. Last errors: {string.Join(" | ", lastErrors)}");
            }

            return lastResult;
        }


        #region Helper Methods

        private static string BuildCompactFeedback(IReadOnlyList<string> errors)
        {
            if (errors.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("=== PREVIOUS ATTEMPT FAILED VALIDATION ===");
            sb.AppendLine("Generate a corrected, complete JSON result from scratch.");
            sb.AppendLine();
            sb.AppendLine("The validation errors below are HARD constraints and MUST be fixed in the next output:");

            foreach (var error in errors)
            {
                sb.AppendLine($"- {error}");
            }

            sb.AppendLine();
            sb.AppendLine("VALIDATION PRIORITY:");
            sb.AppendLine("1. Fix every validation error listed above.");
            sb.AppendLine("2. Never preserve extra content by violating a structural constraint.");
            sb.AppendLine("3. If completeness conflicts with a validation constraint, the validation constraint ALWAYS wins.");
            sb.AppendLine("4. Re-check the entire generated structure recursively before responding.");
            sb.AppendLine("5. Return only the corrected JSON result.");

            return sb.ToString();
        }

        private List<ChatMessage> BuildMessages(
            string systemPrompt,
            string userPrompt,
            IEnumerable<ExtractedImage>? images = null,
            string? additionalImageInstruction = null)
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, userPrompt)
            };

            if (images is not null)
            {
                foreach (var image in images)
                {
                    var temporaryImage = _aiAssetStorage.Get(image.DocumentId);

                    if (temporaryImage is null)
                    {
                        _logger.LogWarning("Image {DocumentId} not found in temporary storage — skipping", image.DocumentId);
                        continue;
                    }

                    _logger.LogInformation("Attaching image {ImageId} ({ContentType}, {Size} bytes)", temporaryImage.Id, temporaryImage.ContentType, temporaryImage.Data.Length);

                    var resized = ResizeImage(temporaryImage.Data, maxDimension: 1024);

                    messages.Add(new ChatMessage(ChatRole.User,
                    [
                        new TextContent($"This is the image with IMAGE_ID: {temporaryImage.Id}"),
                        new DataContent(resized, temporaryImage.ContentType)
                    ]));
                }

                if (images.Any() && !string.IsNullOrWhiteSpace(additionalImageInstruction))
                {
                    messages.Add(new ChatMessage(ChatRole.User, additionalImageInstruction));
                }
            }

            return messages;
        }

        private static string ExtractJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new JsonException("AI returned an empty response.");

            text = text.Trim();

            if ((text.StartsWith('{') && text.EndsWith('}')) ||
                (text.StartsWith('[') && text.EndsWith(']')))
            {
                return text;
            }

            if (text.StartsWith("```"))
            {
                var firstNewLine = text.IndexOf('\n');
                var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);

                if (firstNewLine >= 0 && lastFence > firstNewLine)
                {
                    text = text.Substring(firstNewLine + 1, lastFence - firstNewLine - 1).Trim();
                }
            }

            var start = text.IndexOfAny(['{', '[']);
            var end = text.LastIndexOfAny(['}', ']']);

            if (start >= 0 && end > start)
            {
                return text[start..(end + 1)];
            }

            throw new JsonException("No JSON found in AI response.");
        }

        private ChatOptions BuildChatOptions(string systemPrompt, string userPrompt, int? maxOutputTokens = null)
        {
            var estimatedInputTokens = EstimateTokenCount(systemPrompt) + EstimateTokenCount(userPrompt);

            var availableOutputTokens = Math.Max(_settings.MinOutputTokens, _settings.ContextLimit - estimatedInputTokens - _settings.SafetyMargin);

            var finalMaxOutputTokens = maxOutputTokens is > 0
                ? Math.Min(maxOutputTokens.Value, availableOutputTokens)
                : availableOutputTokens;

            return new ChatOptions
            {
                Temperature = _settings.Temperature,
                MaxOutputTokens = finalMaxOutputTokens
            };
        }

        private static byte[] ResizeImage(byte[] imageData, int maxDimension = 1024)
        {
            using var image = Image.Load(imageData);

            if (image.Width <= maxDimension && image.Height <= maxDimension)
            {
                return imageData;
            }

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxDimension, maxDimension)
            }));

            using var output = new MemoryStream();
            image.Save(output, new JpegEncoder { Quality = 85 });
            return output.ToArray();
        }

        #endregion
    }
}