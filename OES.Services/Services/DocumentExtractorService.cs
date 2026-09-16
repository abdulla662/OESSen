using Docnet.Core;
using Docnet.Core.Models;
using Docnet.Core.Readers;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OES.Helper.Dtos.Document.Request;
using OES.Helper.Dtos.Document.Response;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using OES.Services.AIFeatures.Telemetry;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Tesseract;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using DocMath = DocumentFormat.OpenXml.Math;
using Drawing = DocumentFormat.OpenXml.Drawing;
using PdfPage = UglyToad.PdfPig.Content.Page;

namespace OES.Services.Services
{
    public class DocumentExtractorService : IDocumentExtractorService
    {
        private const string ImagePlaceholder = "[image]";
        private const int TargetOcrDpi = 200;
        private const int MinOcrImageDimension = 20;
        private const float WordConfidenceThreshold = 50f;
        private const int MinPlausibleWordLength = 2;
        private const float SingleWordConfidenceThreshold = 60f;
        private const double MinConfidentWordRatio = 0.4;
        private const int MinMeaningfulChars = 3;
        private const double NonTextInkCoverageThreshold = 0.3;
        private const byte InkPixelThreshold = 180;

        private readonly ILogger<DocumentExtractorService> _logger;
        private readonly IDoclingClient _doclingClient;
        private readonly DoclingSettings _doclingSettings;
        private readonly AIAssetStorage _aiAssetStorage;

        public DocumentExtractorService(
            ILogger<DocumentExtractorService> logger,
            IDoclingClient doclingClient,
            IOptions<DoclingSettings> doclingSettings,
            AIAssetStorage aiAssetStorage)
        {
            _logger = logger;
            _doclingClient = doclingClient;
            _doclingSettings = doclingSettings.Value;
            _aiAssetStorage = aiAssetStorage;
        }

        public async Task<ExtractionResult> ExtractAsync(Stream stream, string fileName, ExtractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            Activity? activity = null;

            try
            {
                if (stream is null)
                {
                    return ExtractionResult.Failed(Resource.FileRequired);
                }

                options ??= new ExtractionOptions();

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return ExtractionResult.Failed(Resource.FileNameRequired);
                }

                var documentType = GetDocumentType(fileName);
                if (documentType == DocumentFileType.Unknown)
                {
                    return ExtractionResult.Failed(Resource.UnsupportedFileType);
                }

                using var scope = _logger.BeginScope(new Dictionary<string, object>
                {
                    ["FileName"] = fileName,
                    ["DocumentType"] = documentType.ToString(),
                    ["OcrEnabled"] = options.EnableOcr
                });

                using var currentActivity = AIActivitySource.StartDocumentExtraction(fileName, documentType.ToString());

                activity = currentActivity;

                _logger.LogInformation("Starting document extraction");

                Func<TesseractEngine>? engineFactory = null;

                if (options.EnableOcr)
                {
                    var tessDataPath = Path.Combine(AppContext.BaseDirectory, MiscConstants.TessDataFolder);
                    if (!Directory.Exists(tessDataPath))
                    {
                        _logger.LogWarning("OCR requested but Tesseract data path not found at {Path}", tessDataPath);
                        activity?.SetStatus(ActivityStatusCode.Error, "OCR data not found");
                        return ExtractionResult.Failed(Resource.OCRDisable);
                    }

                    var language = string.IsNullOrWhiteSpace(options.OcrLanguage) ? MiscConstants.DefaultOcrLanguage : options.OcrLanguage;
                    engineFactory = () => new TesseractEngine(tessDataPath, language, EngineMode.LstmOnly);
                }

                ExtractionResult result;

                if (documentType == DocumentFileType.Txt)
                {
                    result = await ExtractTxtAsync(stream, cancellationToken);
                }
                else if (options.UseDocling)
                {
                    result = await ExtractHybridAsync(stream, fileName, documentType, engineFactory, cancellationToken);
                }
                else
                {
                    using var engine = engineFactory?.Invoke();
                    result = documentType switch
                    {
                        DocumentFileType.Docx => ExtractDocx(stream, engine),
                        DocumentFileType.Pptx => ExtractPptx(stream, engine),
                        DocumentFileType.Pdf => ExtractPdf(stream, engine),
                        _ => ExtractionResult.Failed(Resource.UnsupportedFileType)
                    };
                }

                activity?.SetTag("ai.extraction.used_ocr", result.UsedOcr);
                activity?.SetTag("ai.extraction.page_count", result.PageCount);
                activity?.SetStatus(result.Success ? ActivityStatusCode.Ok : ActivityStatusCode.Error);

                _logger.LogInformation("Document extraction completed. Success={Success} UsedOcr={UsedOcr}", result.Success, result.UsedOcr);

                return result;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        private static async Task<ExtractionResult> ExtractTxtAsync(Stream stream, CancellationToken cancellationToken)
        {
            ResetStream(stream);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var text = await reader.ReadToEndAsync(cancellationToken);
            return ExtractionResult.Succeeded(text);
        }

        #region Docx

        private static ExtractionResult ExtractDocx(Stream stream, TesseractEngine? engine)
        {
            var (content, usedOcr, pageCount) = ExtractDocxContent(stream, engine);

            return content.Count == 0
                ? ExtractionResult.Failed(Resource.DocumentBodyNotFound)
                : ExtractionResult.Succeeded(content, pageCount, usedOcr);
        }

        private static (List<ExtractionResult.ExtractedContent> Content, bool UsedOcr, int PageCount) ExtractDocxContent(Stream stream, TesseractEngine? engine)
        {
            ResetStream(stream);
            using var document = WordprocessingDocument.Open(stream, false);
            var body = document.MainDocumentPart?.Document?.Body;
            if (body is null)
            {
                return (new List<ExtractionResult.ExtractedContent>(), false, 1);
            }

            var builder = new StringBuilder();
            var didUseOcr = false;

            foreach (var element in body.Elements())
            {
                switch (element)
                {
                    case Paragraph paragraph:
                        ExtractParagraph(paragraph, document.MainDocumentPart!, builder, engine, ref didUseOcr);
                        break;

                    case Table table:
                        ExtractTable(builder, table, document.MainDocumentPart!, engine, ref didUseOcr);
                        break;
                }
            }

            var text = NormalizeText(builder.ToString());

            var content = string.IsNullOrWhiteSpace(text)
                ? new List<ExtractionResult.ExtractedContent>()
                : [
                    new() { Type = ExtractedContentType.Text, Content = text, PageNumber = 1 }
                ];

            return (content, didUseOcr, 1);
        }

        #endregion

        #region Pptx

        private static ExtractionResult ExtractPptx(Stream stream, TesseractEngine? engine)
        {
            var (content, usedOcr, pageCount) = ExtractPptxContent(stream, engine);

            return content.Count == 0
                ? ExtractionResult.Failed(Resource.DocumentBodyNotFound)
                : ExtractionResult.Succeeded(content, pageCount, usedOcr);
        }

        private static (List<ExtractionResult.ExtractedContent> Content, bool UsedOcr, int PageCount) ExtractPptxContent(Stream stream, TesseractEngine? engine)
        {
            ResetStream(stream);
            using var presentation = PresentationDocument.Open(stream, false);
            var presentationPart = presentation.PresentationPart;
            if (presentationPart?.Presentation?.SlideIdList is null)
            {
                return (new List<ExtractionResult.ExtractedContent>(), false, 0);
            }

            var content = new List<ExtractionResult.ExtractedContent>();
            var slideCount = 0;
            var didUseOcr = false;

            foreach (var slideId in presentationPart.Presentation.SlideIdList.Elements<SlideId>())
            {
                var slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
                slideCount++;

                var builder = new StringBuilder();

                builder.AppendLine($"Slide {slideCount}");

                foreach (var text in slidePart.Slide.Descendants<Drawing.Text>())
                {
                    if (!string.IsNullOrWhiteSpace(text.Text))
                    {
                        builder.AppendLine(text.Text);
                    }
                }

                if (engine != null)
                {
                    foreach (var imagePart in slidePart.ImageParts)
                    {
                        byte[] imageBytes;
                        using (var imageStream = imagePart.GetStream())
                        using (var memory = new MemoryStream())
                        {
                            imageStream.CopyTo(memory);
                            imageBytes = memory.ToArray();
                        }

                        AppendOcrResult(builder, ExtractTextFromImage(engine, imageBytes), ref didUseOcr);
                    }
                }

                var slideText = NormalizeText(builder.ToString());

                if (!string.IsNullOrWhiteSpace(slideText))
                {
                    content.Add(new ExtractionResult.ExtractedContent
                    {
                        Type = ExtractedContentType.Text,
                        Content = slideText,
                        PageNumber = slideCount
                    });
                }
            }

            return (content, didUseOcr, slideCount);
        }

        #endregion

        #region Pdf

        private static ExtractionResult ExtractPdf(Stream stream, TesseractEngine? engine)
        {
            var (content, usedOcr, pageCount) = ExtractPdfContent(stream, engine);
            return ExtractionResult.Succeeded(content, pageCount, usedOcr);
        }

        private static (List<ExtractionResult.ExtractedContent> Content, bool UsedOcr, int PageCount) ExtractPdfContent(Stream stream, TesseractEngine? engine)
        {
            ResetStream(stream);
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            var pdfBytes = memory.ToArray();

            using var document = PdfDocument.Open(new MemoryStream(pdfBytes));
            var content = new List<ExtractionResult.ExtractedContent>();
            var didUseOcr = false;
            var pageNumber = 0;

            IDocReader? docReader = null;

            try
            {
                foreach (var page in document.GetPages())
                {
                    pageNumber++;

                    var builder = new StringBuilder();

                    var pageHadContent = false;

                    var embeddedText = page.Text;

                    var forceOcr = !string.IsNullOrWhiteSpace(embeddedText) && ContainsArabic(embeddedText);

                    if (!string.IsNullOrWhiteSpace(embeddedText) && !forceOcr)
                    {
                        builder.AppendLine(embeddedText);
                        pageHadContent = true;
                    }

                    if (engine != null)
                    {
                        IEnumerable<IPdfImage> images;
                        try
                        {
                            images = page.GetImages().ToList();
                        }
                        catch
                        {
                            images = [];
                        }

                        foreach (var image in images)
                        {
                            if (image.WidthInSamples < MinOcrImageDimension || image.HeightInSamples < MinOcrImageDimension)
                            {
                                continue;
                            }

                            pageHadContent = true;
                            AppendOcrResult(builder, ExtractTextFromPdfImage(engine, image), ref didUseOcr);
                        }
                    }

                    if ((!pageHadContent || forceOcr) && engine != null)
                    {
                        docReader ??= DocLib.Instance.GetDocReader(pdfBytes, ComputeRenderDimensions(page));

                        using var pageReader = docReader.GetPageReader(pageNumber - 1);
                        var imageBytes = RenderPdfPageToPng(pageReader);
                        var pageOcrText = ExtractTextFromImage(engine, imageBytes);

                        if (!string.IsNullOrWhiteSpace(pageOcrText))
                        {
                            builder.AppendLine(pageOcrText);
                            didUseOcr = true;
                        }
                        else if (forceOcr && !string.IsNullOrWhiteSpace(embeddedText))
                        {
                            builder.AppendLine(embeddedText);
                            pageHadContent = true;
                        }
                        else
                        {
                            builder.AppendLine(ImagePlaceholder);
                        }
                    }

                    var pageText = NormalizeText(builder.ToString());

                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        content.Add(new ExtractionResult.ExtractedContent
                        {
                            Type = ExtractedContentType.Text,
                            Content = pageText,
                            PageNumber = pageNumber
                        });
                    }
                }
            }
            finally
            {
                docReader?.Dispose();
            }

            return (content, didUseOcr, document.NumberOfPages);
        }

        #endregion

        #region Hybrid (local text/OCR + Docling structure)

        private async Task<ExtractionResult> ExtractHybridAsync(Stream stream, string fileName, DocumentFileType documentType, Func<TesseractEngine>? engineFactory, CancellationToken cancellationToken)
        {
            ResetStream(stream);
            await using var bufferedStream = new MemoryStream();
            await stream.CopyToAsync(bufferedStream, cancellationToken);
            var fileBytes = bufferedStream.ToArray();

            var localTask = Task.Run(() =>
            {
                using var localEngine = engineFactory?.Invoke();
                using var localStream = new MemoryStream(fileBytes, writable: false);

                return documentType switch
                {
                    DocumentFileType.Docx => ExtractDocxContent(localStream, localEngine),
                    DocumentFileType.Pptx => ExtractPptxContent(localStream, localEngine),
                    DocumentFileType.Pdf => ExtractPdfContent(localStream, localEngine),
                    _ => (new List<ExtractionResult.ExtractedContent>(), false, 1)
                };
            }, cancellationToken);

            var doclingTask = RunDoclingSafeAsync(fileBytes, fileName, engineFactory, cancellationToken);

            await Task.WhenAll(localTask, doclingTask);

            var (localContent, localUsedOcr, localPageCount) = await localTask;
            var doclingResult = await doclingTask;

            if (localContent.Count == 0 && !doclingResult.Success)
            {
                return ExtractionResult.Failed(Resource.DocumentBodyNotFound);
            }

            var merged = localContent
                .Concat(doclingResult.Content)
                .OrderBy(c => c.PageNumber)
                .ToList();

            if (merged.Count == 0)
            {
                return ExtractionResult.Failed(Resource.DocumentBodyNotFound);
            }

            var pageCount = Math.Max(localPageCount, doclingResult.PageCount);
            var usedOcr = localUsedOcr || doclingResult.UsedOcr;

            return ExtractionResult.Succeeded(merged, pageCount: Math.Max(pageCount, 1), usedOcr: usedOcr);
        }

        #endregion

        #region Docling

        private async Task<ExtractionResult> RunDoclingSafeAsync(byte[] fileBytes, string fileName, Func<TesseractEngine>? engineFactory, CancellationToken cancellationToken)
        {
            try
            {
                using var doclingEngine = engineFactory?.Invoke();
                using var doclingStream = new MemoryStream(fileBytes, writable: false);
                return await ExtractWithDoclingAsync(doclingStream, fileName, doclingEngine, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Docling structural pass failed for {FileName}; continuing with local text/OCR only.", fileName);
                return ExtractionResult.Succeeded([]);
            }
        }

        private static readonly JsonSerializerOptions DoclingJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        private async Task<ExtractionResult> ExtractWithDoclingAsync(Stream stream, string fileName, TesseractEngine? engine, CancellationToken cancellationToken)
        {
            ResetStream(stream);

            string rawJson;
            try
            {
                rawJson = await _doclingClient.ConvertAsync(stream, fileName, needOcr: false, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Docling conversion failed for {FileName}", fileName);
                return ExtractionResult.Failed(Resource.UnsupportedFileType);
            }

            cancellationToken.ThrowIfCancellationRequested();

            DoclingConvertResponse? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<DoclingConvertResponse>(rawJson, DoclingJsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Docling response for {FileName}", fileName);
                return ExtractionResult.Failed(Resource.UnsupportedFileType);
            }

            var document = parsed?.Document?.JsonContent;
            if (document is null)
            {
                return ExtractionResult.Failed(Resource.DocumentBodyNotFound);
            }

            var content = new List<ExtractionResult.ExtractedContent>();
            var imageIndex = 0;
            var usedOcr = false;

            await WalkDoclingNodesAsync(document.Body.Children, document, content, () => ++imageIndex, engine, () => usedOcr = true, cancellationToken);

            var maxPage = content.Select(c => c.PageNumber).DefaultIfEmpty(1).Max();
            var pageCount = document.Pages?.Count > 0 ? document.Pages.Count : maxPage;

            return ExtractionResult.Succeeded(content, pageCount: Math.Max(pageCount, 1), usedOcr: usedOcr);
        }

        private async Task WalkDoclingNodesAsync(
            List<DoclingRef> children,
            DoclingDocument document,
            List<ExtractionResult.ExtractedContent> content,
            Func<int> nextImageIndex,
            TesseractEngine? engine,
            Action markUsedOcr,
            CancellationToken cancellationToken
        )
        {
            foreach (var childRef in children)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await WalkDoclingNodeAsync(childRef.Ref, document, content, nextImageIndex, engine, markUsedOcr, cancellationToken);
            }
        }

        private async Task WalkDoclingNodeAsync(
            string selfRef,
            DoclingDocument document,
            List<ExtractionResult.ExtractedContent> content,
            Func<int> nextImageIndex,
            TesseractEngine? engine,
            Action markUsedOcr,
            CancellationToken cancellationToken
        )
        {
            var (collection, idx) = ParseDoclingRef(selfRef);

            switch (collection)
            {
                case DoclingCollection.Texts:
                    var text = document.Texts.ElementAtOrDefault(idx);
                    if (text is null || text.ContentLayer == "furniture") return;

                    var mappedType = MapDoclingTextLabel(text.Label);

                    if (mappedType is not (ExtractedContentType.Formula or ExtractedContentType.Code))
                    {
                        return;
                    }

                    var rawText = mappedType == ExtractedContentType.Formula
                        ? (!string.IsNullOrEmpty(text.Text) ? text.Text : text.Orig)
                        : (!string.IsNullOrEmpty(text.Orig) ? text.Orig : text.Text);

                    if (string.IsNullOrWhiteSpace(rawText)) return;

                    var processedText = rawText;

                    if (mappedType == ExtractedContentType.Formula)
                    {
                        var trimmed = rawText.Trim();

                        trimmed = trimmed.Trim('$');

                        processedText = $"[Latex][{trimmed}]";
                    }

                    content.Add(new ExtractionResult.ExtractedContent
                    {
                        Type = mappedType,
                        Content = processedText,
                        PageNumber = text.Prov?.FirstOrDefault()?.PageNo ?? 1,
                        Reference = selfRef
                    });
                    break;

                case DoclingCollection.Pictures:
                    var picture = document.Pictures.ElementAtOrDefault(idx);
                    if (picture?.Image?.Uri is null) return;

                    await ProcessDoclingPictureAsync(picture, selfRef, content, nextImageIndex, engine, markUsedOcr, cancellationToken);
                    break;

                case DoclingCollection.Tables:
                    var table = document.Tables.ElementAtOrDefault(idx);
                    if (table is null) return;

                    var tableContent = ConvertDoclingTableToMarkdown(table);

                    content.Add(new ExtractionResult.ExtractedContent
                    {
                        Type = ExtractedContentType.Table,
                        Content = tableContent,
                        PageNumber = table.Prov?.FirstOrDefault()?.PageNo ?? 1,
                        Reference = selfRef
                    });
                    break;

                case DoclingCollection.Groups:
                    var group = document.Groups.ElementAtOrDefault(idx);
                    if (group is null) return;
                    await WalkDoclingNodesAsync(group.Children, document, content, nextImageIndex, engine, markUsedOcr, cancellationToken);
                    break;
            }
        }

        private async Task ProcessDoclingPictureAsync(
            DoclingPictureItem picture,
            string selfRef,
            List<ExtractionResult.ExtractedContent> content,
            Func<int> nextImageIndex,
            TesseractEngine? engine,
            Action markUsedOcr,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pageNo = picture.Prov?.FirstOrDefault()?.PageNo ?? 1;
            var (imageBytes, extension, contentType) = DecodeDataUri(picture.Image!.Uri!);

            var ocrText = string.Empty;
            var hasVisualContent = false;
            var hasValidText = engine != null && TryExtractOcrText(engine, imageBytes, out ocrText, out hasVisualContent);

            if (hasValidText)
            {
                markUsedOcr();
                content.Add(new ExtractionResult.ExtractedContent
                {
                    Type = ExtractedContentType.Text,
                    Content = ocrText,
                    PageNumber = pageNo,
                    Reference = selfRef
                });

                if (!hasVisualContent)
                {
                    return;
                }
            }

            var index = nextImageIndex();
            var savedImage = SaveDoclingImage(imageBytes, extension, contentType, index);

            content.Add(new ExtractionResult.ExtractedContent
            {
                Type = ExtractedContentType.Image,
                Content = string.Empty,
                PageNumber = pageNo,
                Reference = selfRef,
                Image = savedImage
            });

            await Task.CompletedTask;
        }

        private static ExtractedContentType MapDoclingTextLabel(string? label) =>
            label?.ToLowerInvariant() switch
            {
                "formula" => ExtractedContentType.Formula,
                "code" => ExtractedContentType.Code,
                "list_item" => ExtractedContentType.ListItem,
                "title" or "section_header" or "section_header_level_1" => ExtractedContentType.Heading,
                _ => ExtractedContentType.Text
            };

        private static (DoclingCollection Collection, int Index) ParseDoclingRef(string selfRef)
        {
            if (string.IsNullOrEmpty(selfRef))
                return (DoclingCollection.Unknown, -1);

            ReadOnlySpan<char> span = selfRef.AsSpan();

            while (span.Length > 0 && (span[0] == '#' || span[0] == '/'))
            {
                span = span.Slice(1);
            }

            int slashIdx = span.IndexOf('/');
            if (slashIdx < 0)
                return (DoclingCollection.Unknown, -1);

            var collectionSpan = span.Slice(0, slashIdx);
            var indexSpan = span.Slice(slashIdx + 1);

            if (!int.TryParse(indexSpan, out int idx))
                return (DoclingCollection.Unknown, -1);

            DoclingCollection collection = DoclingCollection.Unknown;
            if (collectionSpan.Equals("texts", StringComparison.Ordinal))
                collection = DoclingCollection.Texts;
            else if (collectionSpan.Equals("pictures", StringComparison.Ordinal))
                collection = DoclingCollection.Pictures;
            else if (collectionSpan.Equals("tables", StringComparison.Ordinal))
                collection = DoclingCollection.Tables;
            else if (collectionSpan.Equals("groups", StringComparison.Ordinal))
                collection = DoclingCollection.Groups;

            return (collection, idx);
        }

        private static (byte[] Bytes, string Extension, string ContentType) DecodeDataUri(string dataUri)
        {
            var base64Data = dataUri;
            var extension = ".png";
            var contentType = "image/png";

            if (dataUri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var commaIndex = dataUri.IndexOf(',');

                if (commaIndex < 0)
                    throw new InvalidOperationException("Invalid image data URI.");

                var header = dataUri[..commaIndex];
                base64Data = dataUri[(commaIndex + 1)..];

                if (header.Contains("jpeg", StringComparison.OrdinalIgnoreCase) ||
                    header.Contains("jpg", StringComparison.OrdinalIgnoreCase))
                {
                    extension = ".jpg";
                    contentType = "image/jpeg";
                }
                else if (header.Contains("gif", StringComparison.OrdinalIgnoreCase))
                {
                    extension = ".gif";
                    contentType = "image/gif";
                }
                else if (header.Contains("webp", StringComparison.OrdinalIgnoreCase))
                {
                    extension = ".webp";
                    contentType = "image/webp";
                }
            }

            return (Convert.FromBase64String(base64Data), extension, contentType);
        }

        private ExtractionResult.ExtractedImage SaveDoclingImage(byte[] bytes, string extension, string contentType, int index)
        {
            using var image = Image.Load(bytes);

            var width = image.Width;
            var height = image.Height;

            var imageId = _aiAssetStorage.Save(bytes, contentType);

            return new ExtractionResult.ExtractedImage
            {
                Reference = $"/{MiscConstants.GetAIAsset}?assetId={imageId}",
                FileName = $"image_{index:D3}{extension}",
                ContentType = contentType,
                DocumentId = imageId,
                Width = width,
                Height = height
            };
        }

        #endregion


        #region Helper Methods

        private static void ExtractParagraph(Paragraph paragraph, MainDocumentPart mainPart, StringBuilder builder, TesseractEngine? engine, ref bool didUseOcr)
        {
            foreach (var run in paragraph.Elements<Run>())
            {
                if (!string.IsNullOrWhiteSpace(run.InnerText))
                {
                    builder.Append(run.InnerText);
                }
            }

            foreach (var mathText in paragraph.Descendants<DocMath.Text>())
            {
                if (!string.IsNullOrWhiteSpace(mathText.Text))
                {
                    builder.Append(mathText.Text);
                }
            }

            foreach (var blip in paragraph.Descendants<Drawing.Blip>())
            {
                var embed = blip.Embed?.Value;
                if (string.IsNullOrWhiteSpace(embed) || mainPart.GetPartById(embed) is not ImagePart imagePart)
                    continue;

                builder.AppendLine();

                if (engine != null)
                {
                    byte[] imageBytes;
                    using (var imageStream = imagePart.GetStream())
                    using (var memory = new MemoryStream())
                    {
                        imageStream.CopyTo(memory);
                        imageBytes = memory.ToArray();
                    }

                    AppendOcrResult(builder, ExtractTextFromImage(engine, imageBytes), ref didUseOcr);
                }
                else
                {
                    builder.AppendLine(ImagePlaceholder);
                }
            }

            builder.AppendLine();
        }

        private static void ExtractTable(StringBuilder builder, Table table, MainDocumentPart mainPart, TesseractEngine? engine, ref bool didUseOcr)
        {
            foreach (var row in table.Elements<TableRow>())
            {
                foreach (var cell in row.Elements<TableCell>())
                {
                    foreach (var paragraph in cell.Elements<Paragraph>())
                    {
                        ExtractParagraph(paragraph, mainPart, builder, engine, ref didUseOcr);
                    }
                    builder.Append('\t');
                }
                builder.AppendLine();
            }
            builder.AppendLine();
        }

        private static void AppendOcrResult(StringBuilder builder, string ocrText, ref bool didUseOcr)
        {
            if (!string.IsNullOrWhiteSpace(ocrText))
            {
                builder.AppendLine(ocrText);
                didUseOcr = true;
            }
            else
            {
                builder.AppendLine(ImagePlaceholder);
            }
        }

        private static DocumentFileType GetDocumentType(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".txt" => DocumentFileType.Txt,
                ".docx" => DocumentFileType.Docx,
                ".pdf" => DocumentFileType.Pdf,
                ".pptx" => DocumentFileType.Pptx,
                _ => DocumentFileType.Unknown
            };
        }

        private static void ResetStream(Stream stream)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
        }

        private static string NormalizeText(string text)
        {
            return text
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Trim();
        }

        private static bool ContainsArabic(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            foreach (var c in text)
            {
                if ((c >= '\u0600' && c <= '\u06FF') ||
                    (c >= '\u0750' && c <= '\u077F') ||
                    (c >= '\uFB50' && c <= '\uFDFF') ||
                    (c >= '\uFE70' && c <= '\uFEFF'))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ExtractTextFromPdfImage(TesseractEngine engine, IPdfImage image)
        {
            byte[]? bytes = null;

            try
            {
                if (image.TryGetPng(out var png) && png.Length > 0)
                {
                    bytes = png;
                }
                else if (image.RawMemory.Length > 0)
                {
                    bytes = image.RawMemory.ToArray();
                }
            }
            catch
            {
                bytes = null;
            }

            return bytes is null || bytes.Length == 0 ? string.Empty : ExtractTextFromImage(engine, bytes);
        }

        private static string ExtractTextFromImage(TesseractEngine engine, byte[] imageBytes)
        {
            try
            {
                var text = TryOcr(engine, imageBytes, PageSegMode.Auto);
                return !string.IsNullOrWhiteSpace(text) ? text : TryOcr(engine, imageBytes, PageSegMode.SparseText);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string TryOcr(TesseractEngine engine, byte[] imageBytes, PageSegMode mode)
        {
            var result = RunOcrWithWords(engine, imageBytes, mode);
            return IsOcrTextValid(result.Text, result.Words) ? result.Text : string.Empty;
        }

        private readonly record struct WordInfo(string Text, float Confidence, int X, int Y, int Width, int Height);

        private readonly record struct OcrWordsResult(string Text, List<WordInfo> Words, int ImageWidth, int ImageHeight);

        private static OcrWordsResult RunOcrWithWords(TesseractEngine engine, byte[] imageBytes, PageSegMode mode)
        {
            using var pix = Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(pix, mode);

            var fullText = page.GetText()?.Trim() ?? string.Empty;
            var words = new List<WordInfo>();

            using (var iter = page.GetIterator())
            {
                iter.Begin();
                do
                {
                    var word = iter.GetText(PageIteratorLevel.Word);
                    if (string.IsNullOrWhiteSpace(word))
                        continue;

                    var confidence = iter.GetConfidence(PageIteratorLevel.Word);

                    if (iter.TryGetBoundingBox(PageIteratorLevel.Word, out var bounds))
                    {
                        words.Add(new WordInfo(word, confidence, bounds.X1, bounds.Y1, bounds.Width, bounds.Height));
                    }
                    else
                    {
                        words.Add(new WordInfo(word, confidence, 0, 0, 0, 0));
                    }
                }
                while (iter.Next(PageIteratorLevel.Word));
            }

            return new OcrWordsResult(fullText, words, pix.Width, pix.Height);
        }

        private static bool IsOcrTextValid(string fullText, List<WordInfo> words)
        {
            if (string.IsNullOrWhiteSpace(fullText) || words.Count == 0)
                return false;

            if (words.Count == 1)
            {
                var word = words[0];
                if (!IsPlausibleWord(word.Text) || word.Confidence < SingleWordConfidenceThreshold)
                    return false;
            }
            else
            {
                var confidentCount = words.Count(w => w.Confidence >= WordConfidenceThreshold && IsPlausibleWord(w.Text));
                if ((double)confidentCount / words.Count < MinConfidentWordRatio)
                    return false;
            }

            var meaningfulChars = fullText.Count(c => char.IsLetterOrDigit(c));
            return meaningfulChars >= MinMeaningfulChars;
        }

        private static bool IsPlausibleWord(string text)
        {
            if (text.Length >= MinPlausibleWordLength)
                return true;

            if (text.All(char.IsDigit))
                return true;

            if (text.Length == 1 && char.IsLetter(text[0]))
                return true;

            return false;
        }

        private static bool TryExtractOcrText(TesseractEngine engine, byte[] imageBytes, out string text, out bool hasSignificantVisualContent)
        {
            text = string.Empty;
            hasSignificantVisualContent = false;

            try
            {
                var result = RunOcrWithWords(engine, imageBytes, PageSegMode.Auto);

                if (string.IsNullOrWhiteSpace(result.Text))
                {
                    result = RunOcrWithWords(engine, imageBytes, PageSegMode.SparseText);
                }

                if (!IsOcrTextValid(result.Text, result.Words))
                {
                    return false;
                }

                var confidentWords = result.Words
                    .Where(w => w.Confidence >= WordConfidenceThreshold && IsPlausibleWord(w.Text))
                    .ToList();

                hasSignificantVisualContent = HasSignificantVisualContent(imageBytes, confidentWords, result.ImageWidth, result.ImageHeight);
                text = result.Text;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool HasSignificantVisualContent(byte[] imageBytes, List<WordInfo> confidentWords, int imageWidth, int imageHeight)
        {
            if (imageWidth <= 0 || imageHeight <= 0)
                return false;

            using var image = Image.Load<L8>(imageBytes);

            var width = image.Width;
            var height = image.Height;

            var textMask = new bool[width * height];
            foreach (var word in confidentWords)
            {
                var x0 = Math.Clamp(word.X, 0, width);
                var y0 = Math.Clamp(word.Y, 0, height);
                var x1 = Math.Clamp(word.X + word.Width, 0, width);
                var y1 = Math.Clamp(word.Y + word.Height, 0, height);

                for (var y = y0; y < y1; y++)
                {
                    var rowOffset = y * width;
                    for (var x = x0; x < x1; x++)
                    {
                        textMask[rowOffset + x] = true;
                    }
                }
            }

            long totalInk = 0;
            long inkInsideText = 0;

            image.ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    var rowOffset = y * width;

                    for (var x = 0; x < row.Length; x++)
                    {
                        if (row[x].PackedValue >= InkPixelThreshold)
                            continue;

                        totalInk++;
                        if (textMask[rowOffset + x])
                        {
                            inkInsideText++;
                        }
                    }
                }
            });

            if (totalInk == 0)
                return false;

            var outsideTextRatio = (double)(totalInk - inkInsideText) / totalInk;

            return outsideTextRatio > NonTextInkCoverageThreshold;
        }

        private static PageDimensions ComputeRenderDimensions(PdfPage page)
        {
            var widthPx = Math.Max(1, (int)Math.Round(page.Width / 72.0 * TargetOcrDpi));
            var heightPx = Math.Max(1, (int)Math.Round(page.Height / 72.0 * TargetOcrDpi));

            var dimOne = Math.Min(widthPx, heightPx);
            var dimTwo = Math.Max(widthPx, heightPx);
            return new PageDimensions(dimOne, dimTwo);
        }

        private static byte[] RenderPdfPageToPng(IPageReader pageReader)
        {
            var rawBytes = pageReader.GetImage();
            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();

            using var image = Image.LoadPixelData<Bgra32>(rawBytes, width, height);
            image.Mutate(x =>
            {
                x.Grayscale();
            });

            using var output = new MemoryStream();
            image.Save(output, new PngEncoder());
            return output.ToArray();
        }

        private static string ConvertDoclingTableToMarkdown(DoclingTableItem table)
        {
            if (table.Data?.TableCells == null ||
                table.Data.TableCells.Count == 0)
            {
                return string.Empty;
            }

            var rows = table.Data.NumRows;
            var cols = table.Data.NumCols;

            var grid = new string[rows, cols];

            foreach (var cell in table.Data.TableCells)
            {
                var row = cell.StartRowOffsetIdx;
                var col = cell.StartColOffsetIdx;

                if (row < 0 || row >= rows ||
                    col < 0 || col >= cols)
                {
                    continue;
                }

                grid[row, col] = cell.Text?.Trim() ?? string.Empty;
            }

            var sb = new StringBuilder();

            sb.Append('|');

            for (var col = 0; col < cols; col++)
            {
                sb.Append(' ');
                sb.Append(EscapeMarkdownTableCell(grid[0, col]));
                sb.Append(" |");
            }

            sb.AppendLine();

            sb.Append('|');

            for (var col = 0; col < cols; col++)
            {
                sb.Append(" --- |");
            }

            sb.AppendLine();

            for (var row = 1; row < rows; row++)
            {
                sb.Append('|');

                for (var col = 0; col < cols; col++)
                {
                    sb.Append(' ');
                    sb.Append(EscapeMarkdownTableCell(grid[row, col]));
                    sb.Append(" |");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string EscapeMarkdownTableCell(string? value)
        {
            return (value ?? string.Empty)
                .Replace("|", "\\|")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
        }

        #endregion
    }
}