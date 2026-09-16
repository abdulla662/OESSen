using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OES.Helper.Dtos.Document.Request;
using OES.Helper.General;
using OES.Interface.Interfaces;
using SharedHelper.General;
using System.Text.Json;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace OES.Services.Services
{
    public sealed class DoclingClient : IDoclingClient
    {
        private const string FilesParameter = "files";
        private const string ToFormatsParameter = "to_formats";
        private const string ImageExportModeParameter = "image_export_mode";
        private const string DoFormulaEnrichmentParameter = "do_formula_enrichment";
        private const string OptionsParameter = "options";

        private const string TaskIdProperty = "task_id";
        private const string TaskStatusProperty = "task_status";
        private const string ErrorMessageProperty = "error_message";

        private const string StatusSuccess = "success";
        private const string StatusFailure = "failure";

        private const string ContentTypePdf = "application/pdf";
        private const string ContentTypeDocx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        private const string ContentTypePptx = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
        private const string ContentTypeXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string ContentTypeHtml = "text/html";
        private const string ContentTypeTxt = "text/plain";
        private const string ContentTypeDefault = "application/octet-stream";

        private readonly HttpClient _httpClient;
        private readonly DoclingSettings _settings;
        private readonly ILogger<DoclingClient> _logger;

        private static readonly JsonSerializerOptions DoclingJsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public DoclingClient(HttpClient httpClient, IOptions<DoclingSettings> settings, ILogger<DoclingClient> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<string> ConvertAsync(Stream file, string fileName, bool needOcr, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(file);

            if (!file.CanRead)
                throw new ArgumentException("The provided file stream is not readable.", nameof(file));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name is required.", nameof(fileName));

            var endpoint = _settings.AsyncConvertEndpoint.TrimStart('/');

            var doFormulaEnrichment = true;

            _logger.LogInformation(
                "Sending document to Docling asynchronously. FileName: {FileName}, Endpoint: {Endpoint}",
                fileName,
                endpoint);

            using var content = new MultipartFormDataContent();

            using var fileContent = new StreamContent(file);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
            content.Add(fileContent, FilesParameter, fileName);

            content.Add(new StringContent(MiscConstants.DoclingJsonFormat), ToFormatsParameter);

            var options = new DoclingConvertRequestOptions
            {
                ToFormats = [MiscConstants.DoclingJsonFormat],
                DoOcr = needOcr,
                ForceOcr = needOcr,
                OcrEngine = MiscConstants.DoclingOcrTesseract,
                OcrLang = [MiscConstants.DoclingLangArabic, MiscConstants.DoclingLangEnglish],
                GeneratePictureImages = true,
                DoPictureDescription = false
            };

            content.Add(new StringContent(MiscConstants.DoclingImageExportModeEmbedded), ImageExportModeParameter);

            content.Add(new StringContent(doFormulaEnrichment.ToString().ToLowerInvariant()), DoFormulaEnrichmentParameter);

            content.Add(new StringContent(JsonSerializer.Serialize(options, DoclingJsonSerializerOptions)), OptionsParameter);

            try
            {
                using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogInformation(
                    "Docling submit response. FileName: {FileName}, StatusCode: {StatusCode}, Body: {Body}",
                    fileName,
                    response.StatusCode,
                    responseBody);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Docling async submission failed. FileName: {FileName}, StatusCode: {StatusCode}, Response: {Response}",
                        fileName,
                        response.StatusCode,
                        responseBody);

                    throw new HttpRequestException($"Docling async submission failed with status code {(int)response.StatusCode}: {responseBody}");
                }

                using var taskDocument = JsonDocument.Parse(responseBody);

                if (!taskDocument.RootElement.TryGetProperty(TaskIdProperty, out var taskIdElement))
                {
                    throw new InvalidOperationException($"Docling did not return {TaskIdProperty}. Response: {responseBody}");
                }

                var taskId = taskIdElement.GetString();

                if (string.IsNullOrWhiteSpace(taskId))
                {
                    throw new InvalidOperationException($"Docling returned an empty {TaskIdProperty}.");
                }

                _logger.LogInformation(
                    "Docling conversion submitted successfully. FileName: {FileName}, TaskId: {TaskId}",
                    fileName,
                    taskId);

                var startTime = DateTimeHelper.Now;
                var timeoutSeconds = _settings.TimeoutSeconds > 0 ? _settings.TimeoutSeconds : 1000;
                var statusEndpoint = $"{_settings.StatusEndpoint.TrimEnd('/')}/{taskId}?wait=10";
                var resultEndpoint = $"{_settings.ResultEndpoint.TrimEnd('/')}/{taskId}";

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var elapsedSeconds = (DateTimeHelper.Now - startTime).TotalSeconds;

                    if (elapsedSeconds >= timeoutSeconds)
                    {
                        throw new TimeoutException($"Docling conversion timed out after {timeoutSeconds} seconds. TaskId: {taskId}");
                    }

                    _logger.LogDebug("Checking Docling task status. TaskId: {TaskId}", taskId);

                    using var statusResponse = await _httpClient.GetAsync(statusEndpoint, cancellationToken);

                    var statusBody = await statusResponse.Content.ReadAsStringAsync(cancellationToken);

                    _logger.LogInformation(
                        "Docling raw status body. TaskId: {TaskId}, Body: {Body}",
                        taskId,
                        statusBody);

                    if (!statusResponse.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"Docling status request failed with status code {(int)statusResponse.StatusCode}: {statusBody}");
                    }

                    using var statusDocument = JsonDocument.Parse(statusBody);
                    var root = statusDocument.RootElement;

                    var status = root.TryGetProperty(TaskStatusProperty, out var statusElement)
                        ? statusElement.GetString()
                        : null;

                    _logger.LogInformation(
                        "Docling task status. TaskId: {TaskId}, Status: {Status}, Elapsed: {ElapsedSeconds:F0}s",
                        taskId,
                        status,
                        elapsedSeconds);

                    if (string.Equals(status, StatusSuccess, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation(
                            "Docling conversion completed. Fetching result. TaskId: {TaskId}",
                            taskId);

                        using var resultResponse = await _httpClient.GetAsync(resultEndpoint, cancellationToken);

                        var resultBody = await resultResponse.Content.ReadAsStringAsync(cancellationToken);

                        if (!resultResponse.IsSuccessStatusCode)
                        {
                            _logger.LogError(
                                "Failed to get Docling result. TaskId: {TaskId}, StatusCode: {StatusCode}, Response: {Response}",
                                taskId,
                                resultResponse.StatusCode,
                                resultBody);

                            throw new HttpRequestException($"Docling result request failed with status code {(int)resultResponse.StatusCode}: {resultBody}");
                        }

                        _logger.LogInformation(
                            "Docling result received successfully. FileName: {FileName}, TaskId: {TaskId}, BodyLength: {Length}",
                            fileName,
                            taskId,
                            resultBody.Length);

                        return resultBody;
                    }

                    if (string.Equals(status, StatusFailure, StringComparison.OrdinalIgnoreCase))
                    {
                        var error = root.TryGetProperty(ErrorMessageProperty, out var errorElement)
                            ? errorElement.GetString()
                            : null;

                        if (string.IsNullOrWhiteSpace(error))
                            error = "Unknown Docling processing error.";

                        _logger.LogError(
                            "Docling conversion failed. FileName: {FileName}, TaskId: {TaskId}, Error: {Error}",
                            fileName,
                            taskId,
                            error);

                        throw new InvalidOperationException($"Docling conversion failed. TaskId: {taskId}. Error: {error}");
                    }

                    await Task.Delay(
                        TimeSpan.FromSeconds(Math.Max(1, _settings.PollIntervalSeconds)),
                        cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Docling conversion was cancelled. FileName: {FileName}", fileName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while communicating with Docling. FileName: {FileName}", fileName);
                throw;
            }
        }

        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName);

            return extension.ToLowerInvariant() switch
            {
                ".pdf" => ContentTypePdf,
                ".docx" => ContentTypeDocx,
                ".pptx" => ContentTypePptx,
                ".xlsx" => ContentTypeXlsx,
                ".html" or ".htm" => ContentTypeHtml,
                ".txt" => ContentTypeTxt,
                _ => ContentTypeDefault
            };
        }
    }
}