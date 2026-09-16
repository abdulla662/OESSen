using static OES.Helper.Dtos.Document.Response.ExtractionResult;

namespace OES.Interface.Interfaces
{
    public interface IAIResponseGeneratorService
    {
        Task<T?> GenerateAsync<T>(
            string systemPrompt,
            string userPrompt,
            IEnumerable<ExtractedImage>? images = null,
            string? additionalImageInstruction = null,
            int? maxOutputTokens = null,
            CancellationToken ct = default) where T : class;

        Task<T?> GenerateWithFallbackAsync<T>(
            string systemPrompt,
            string userPrompt,
            IEnumerable<ExtractedImage>? images = null,
            string? additionalImageInstruction = null,
            int? maxOutputTokens = null,
            CancellationToken ct = default) where T : class;

        Task<TResult> GenerateWithValidationAsync<TResult, TContext>(
            string systemPrompt,
            string userPrompt,
            TContext context,
            IAIResponseResultValidator<TResult, TContext> validator,
            IEnumerable<ExtractedImage>? images = null,
            string? additionalImageInstruction = null,
            int? maxOutputTokens = null,
            int maxAttempts = 3,
            CancellationToken ct = default
        ) where TResult : class;
    }
}
