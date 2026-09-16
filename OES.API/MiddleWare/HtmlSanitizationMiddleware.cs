using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Ganss.Xss;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace OES.API.MiddleWare
{
    public class HtmlSanitizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<HtmlSanitizationMiddleware> _logger;
        private readonly HtmlSanitizer _sanitizer;
        private readonly bool _enforceSsrfHostCheck;
        private const int MaxDataUriLength = 20_000_000;

        private static readonly HashSet<string> DangerousSchemes =
            new(StringComparer.OrdinalIgnoreCase) { "javascript", "vbscript", "data", "file", "blob", "about", "filesystem", "chrome", "chrome-extension", "moz-extension", "resource", "ms-appx", "ms-appx-web", "view-source", "jar", "mhtml", "livescript", "mocha", "disk", "shell", "ws", "wss", "ftp", "gopher" };

        private static readonly HashSet<string> AllowedSchemesForObjectData =
            new(StringComparer.OrdinalIgnoreCase) { "http", "https" };

        private static readonly HashSet<string> DangerousMimeTypes =
            new(StringComparer.OrdinalIgnoreCase) { "text/html", "application/xhtml+xml", "image/svg+xml", "text/xml", "application/xml", "application/xslt+xml", "application/rss+xml", "application/atom+xml", "application/mathml+xml", "text/xsl", "application/javascript", "application/ecmascript", "application/x-javascript", "text/javascript", "text/ecmascript", "text/vbscript", "text/jscript", "application/x-ecmascript", "application/wasm", "application/x-shockwave-flash", "application/x-silverlight-app", "application/x-java-applet", "application/java-archive", "application/x-java-jnlp-file", "application/vnd.ms-htmlhelp", "application/hta", "application/x-msdownload", "application/x-msdos-program", "application/x-sh", "application/x-csh", "text/x-sh", "text/x-python", "text/x-perl", "application/x-httpd-php", "application/x-mshelp", "message/rfc822", "multipart/related", "multipart/x-mixed-replace" };

        private static readonly HashSet<string> AllowedDataImageMimeTypes =
            new(StringComparer.OrdinalIgnoreCase) { "image/png", "image/jpeg", "image/jpg", "image/gif", "image/webp" };

        private static readonly Regex DataImageUriPattern =
            new(@"^data:(?<mime>[a-zA-Z0-9.+-]+/[a-zA-Z0-9.+-]+);base64,", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RgbColorPattern =
            new(@"^rgba?\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*(?:,\s*([\d.]+)\s*)?\)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex HexColorPattern =
            new(@"^#(?<hex>[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled);

        private static readonly Regex ZeroWithUnitPattern =
            new(@"(?<![\d.])0(?:px|em|rem|%|pt|pc|in|cm|mm|ex|ch|vw|vh|vmin|vmax)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ColorLikeValuePattern =
            new(@"^(#[0-9a-fA-F]{3,8}|(rgb|rgba|hsl|hsla|hwb|lab|lch|oklab|oklch|color)\s*\(.*\)|[a-zA-Z]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex ImportantSuffixPattern =
            new(@"\s*!\s*important\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly HashSet<string> ColorValuedProperties =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "color", "background-color", "border-color", "outline-color",
                "text-decoration-color", "caret-color", "fill", "stroke"
            };

        private static readonly string[] DangerousCssValueSubstrings =
            ["expression(", "javascript:", "vbscript:", "livescript:", "mocha:", "url(", "behavior:", "-moz-binding", "@import"];

        private static readonly HashSet<string> SensitiveFieldNames =
            new(StringComparer.OrdinalIgnoreCase) { "password", "currentpassword", "newpassword", "confirmpassword", "token", "secret", "apikey" };

        private static readonly HashSet<string> SafeToDropAttributes =
            new(StringComparer.OrdinalIgnoreCase) { "class", "lang", "xml:lang", "align", "valign", "border", "cellpadding", "cellspacing", "id", "name", "title", "dir" };

        private static readonly Regex WordNamespaceAttributePattern =
            new(@"^(o|v|w|m|st1|xmlns)(:|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex WordNamespaceElementPattern =
            new(@"^(o|v|w|m|st1|xml)(:|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public HtmlSanitizationMiddleware(RequestDelegate next, ILogger<HtmlSanitizationMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _sanitizer = new HtmlSanitizer();

            _enforceSsrfHostCheck = !env.IsDevelopment();

            ConfigureSanitizer();
        }

        private void ConfigureSanitizer()
        {
            _sanitizer.AllowedTags.Add("audio");
            _sanitizer.AllowedTags.Add("video");
            _sanitizer.AllowedTags.Add("source");
            _sanitizer.AllowedTags.Add("figure");
            _sanitizer.AllowedTags.Add("figcaption");
            _sanitizer.AllowedTags.Add("object");
            _sanitizer.AllowedTags.Add("b");
            _sanitizer.AllowedTags.Add("strong");
            _sanitizer.AllowedTags.Add("i");
            _sanitizer.AllowedTags.Add("em");
            _sanitizer.AllowedTags.Add("u");
            _sanitizer.AllowedTags.Add("span");
            _sanitizer.AllowedTags.Add("p");
            _sanitizer.AllowedTags.Add("br");
            _sanitizer.AllowedTags.Add("ul");
            _sanitizer.AllowedTags.Add("ol");
            _sanitizer.AllowedTags.Add("li");

            _sanitizer.AllowDataAttributes = true;

            _sanitizer.AllowedAttributes.Add("controls");
            _sanitizer.AllowedAttributes.Add("autoplay");
            _sanitizer.AllowedAttributes.Add("loop");
            _sanitizer.AllowedAttributes.Add("muted");
            _sanitizer.AllowedAttributes.Add("preload");
            _sanitizer.AllowedAttributes.Add("poster");
            _sanitizer.AllowedAttributes.Add("class");
            _sanitizer.AllowedAttributes.Add("style");
            _sanitizer.AllowedAttributes.Add("src");
            _sanitizer.AllowedAttributes.Add("data");
            _sanitizer.AllowedAttributes.Add("type");
            _sanitizer.AllowedAttributes.Add("contenteditable");
            _sanitizer.AllowedAttributes.Add("unselectable");

            _sanitizer.UriAttributes.Add("data");

            _sanitizer.AllowedSchemes.Clear();
            _sanitizer.AllowedSchemes.Add("https");
            _sanitizer.AllowedSchemes.Add("http");
            _sanitizer.AllowedSchemes.Add("data");

            _sanitizer.FilterUrl += OnFilterUrl;

            _sanitizer.AllowedCssProperties.Add("direction");
            _sanitizer.AllowedCssProperties.Add("text-align");
            _sanitizer.AllowedCssProperties.Add("height");
            _sanitizer.AllowedCssProperties.Add("width");
            _sanitizer.AllowedCssProperties.Add("display");
            _sanitizer.AllowedCssProperties.Add("vertical-align");
            _sanitizer.AllowedCssProperties.Add("color");
            _sanitizer.AllowedCssProperties.Add("background-color");
            _sanitizer.AllowedCssProperties.Add("font-weight");
            _sanitizer.AllowedCssProperties.Add("font-family");
            _sanitizer.AllowedCssProperties.Add("font-size");
            _sanitizer.AllowedCssProperties.Add("margin");
            _sanitizer.AllowedCssProperties.Add("margin-top");
            _sanitizer.AllowedCssProperties.Add("margin-bottom");
            _sanitizer.AllowedCssProperties.Add("margin-left");
            _sanitizer.AllowedCssProperties.Add("margin-right");
            _sanitizer.AllowedCssProperties.Add("padding");
            _sanitizer.AllowedCssProperties.Add("padding-top");
            _sanitizer.AllowedCssProperties.Add("padding-bottom");
            _sanitizer.AllowedCssProperties.Add("padding-left");
            _sanitizer.AllowedCssProperties.Add("padding-right");
            _sanitizer.AllowedCssProperties.Add("line-height");
            _sanitizer.AllowedCssProperties.Add("letter-spacing");
            _sanitizer.AllowedCssProperties.Add("text-indent");
            _sanitizer.AllowedCssProperties.Add("white-space");
            _sanitizer.AllowedCssProperties.Add("list-style-type");
            _sanitizer.AllowedCssProperties.Add("font-style");
            _sanitizer.AllowedCssProperties.Add("text-decoration");

            _sanitizer.PostProcessNode += (sender, e) =>
            {
                if (e.Node is IElement element &&
                    element.TagName.Equals("object", StringComparison.OrdinalIgnoreCase))
                {
                    ValidateObjectElement(element);
                }
            };
        }

        private void OnFilterUrl(object? sender, FilterUrlEventArgs e)
        {
            var url = e.SanitizedUrl;

            if (string.IsNullOrEmpty(url) || !url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var isAllowedTag = e.Tag is not null &&
                (e.Tag.TagName.Equals("img", StringComparison.OrdinalIgnoreCase) || e.Tag.TagName.Equals("object", StringComparison.OrdinalIgnoreCase));

            if (!isAllowedTag || !IsAllowedDataImageUri(url))
            {
                e.SanitizedUrl = null;
            }
        }

        private static bool IsAllowedDataImageUri(string url)
        {
            if (url.Length > MaxDataUriLength)
            {
                return false;
            }

            var match = DataImageUriPattern.Match(url);
            if (!match.Success)
            {
                return false;
            }

            var mime = match.Groups["mime"].Value;
            return AllowedDataImageMimeTypes.Contains(mime);
        }

        private void ValidateObjectElement(IElement element)
        {
            var dataAttr = element.GetAttribute("data");
            var typeAttr = element.GetAttribute("type");

            if (string.IsNullOrWhiteSpace(dataAttr))
            {
                RemoveUnsafeObject(element, dataAttr, typeAttr, "missing data attribute");
                return;
            }

            if (dataAttr.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsAllowedDataImageUri(dataAttr))
                {
                    RemoveUnsafeObject(element, dataAttr, typeAttr, "disallowed or invalid data image URI");
                    return;
                }
                if (!string.IsNullOrWhiteSpace(typeAttr) && !AllowedDataImageMimeTypes.Contains(typeAttr.Trim()))
                {
                    RemoveUnsafeObject(element, dataAttr, typeAttr, $"disallowed type '{typeAttr}' for data URI");
                    return;
                }
                return;
            }

            if (!Uri.TryCreate(dataAttr, UriKind.Absolute, out var uri))
            {
                RemoveUnsafeObject(element, dataAttr, typeAttr, "data is not an absolute URL");
                return;
            }

            if (!AllowedSchemesForObjectData.Contains(uri.Scheme) ||
                DangerousSchemes.Contains(uri.Scheme))
            {
                RemoveUnsafeObject(element, dataAttr, typeAttr, $"disallowed scheme '{uri.Scheme}'");
                return;
            }

            if (_enforceSsrfHostCheck && IsBlockedHost(uri.Host))
            {
                RemoveUnsafeObject(element, dataAttr, typeAttr, $"blocked host '{uri.Host}'");
                return;
            }

            if (!string.IsNullOrWhiteSpace(typeAttr) &&
                DangerousMimeTypes.Contains(typeAttr.Trim()))
            {
                RemoveUnsafeObject(element, dataAttr, typeAttr, $"disallowed type '{typeAttr}'");
                return;
            }
        }

        private static bool IsBlockedHost(string host)
        {
            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                return true;

            if (!IPAddress.TryParse(host, out var ip))
                return false;

            if (IPAddress.IsLoopback(ip)) return true;
            if (ip.Equals(IPAddress.Any)) return true;

            var bytes = ip.GetAddressBytes();

            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && bytes.Length == 4)
            {
                if (bytes[0] == 169 && bytes[1] == 254) return true;
                if (bytes[0] == 10) return true;
                if (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) return true;
                if (bytes[0] == 192 && bytes[1] == 168) return true;
                if (bytes[0] == 100 && bytes[1] is >= 64 and <= 127) return true;
            }
            else if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal)
            {
                return true;
            }

            return false;
        }

        private void RemoveUnsafeObject(IElement element, string? data, string? type, string reason)
        {
            _logger.LogWarning("Removed <object> element — {Reason}. data={Data}, type={Type}", reason, data, type);

            element.Remove();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (IsWriteMethod(context))
            {
                if (IsJsonRequest(context))
                {
                    if (!await SanitizeJsonBodyAsync(context))
                        return;
                }
                else if (IsFormRequest(context))
                {
                    if (!await SanitizeFormBodyAsync(context))
                        return;
                }
            }

            await _next(context);
        }

        private static bool IsJsonRequest(HttpContext context)
        {
            return context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static bool IsFormRequest(HttpContext context)
        {
            return context.Request.HasFormContentType;
        }

        private static bool IsWriteMethod(HttpContext context)
        {
            return HttpMethods.IsPost(context.Request.Method)
                || HttpMethods.IsPut(context.Request.Method)
                || HttpMethods.IsPatch(context.Request.Method);
        }

        private async Task<bool> SanitizeFormBodyAsync(HttpContext context)
        {
            try
            {
                var form = await context.Request.ReadFormAsync();

                foreach (var field in form)
                {
                    if (SensitiveFieldNames.Contains(field.Key))
                    {
                        continue;
                    }

                    foreach (var value in field.Value)
                    {
                        var original = value ?? string.Empty;
                        var sanitized = SanitizePreservingOriginal(original);

                        if (!string.Equals(original, sanitized, StringComparison.Ordinal))
                        {
                            _logger.LogWarning("Sanitized form field '{FieldName}'. Original: {OriginalValue}, Sanitized: {SanitizedValue}",
                                field.Key,
                                original,
                                sanitized);

                            return await RejectHarmfulRequestAsync(context);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Form data sanitization failed. Path: {Path}", context.Request.Path);
                return true;
            }
        }

        private async Task<bool> SanitizeJsonBodyAsync(HttpContext context)
        {
            context.Request.EnableBuffering();

            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);

            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                context.Request.Body.Position = 0;
                return true;
            }

            try
            {
                var jsonNode = JsonNode.Parse(body);

                if (jsonNode is null)
                {
                    context.Request.Body.Position = 0;
                    return true;
                }

                bool wasModified;

                try
                {
                    wasModified = SanitizeJsonNode(jsonNode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "HTML sanitization failed while processing request body. Path: {Path}", context.Request.Path);

                    context.Request.Body.Position = 0;
                    return true;
                }

                if (!wasModified)
                {
                    context.Request.Body.Position = 0;
                    return true;
                }

                return await RejectHarmfulRequestAsync(context);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Request body could not be parsed as JSON. Path: {Path}", context.Request.Path);

                context.Request.Body.Position = 0;
                return true;
            }
        }

        private bool SanitizeJsonNode(JsonNode node)
        {
            var modified = false;

            if (node is JsonObject jsonObject)
            {
                foreach (var property in jsonObject.ToList())
                {
                    if (SensitiveFieldNames.Contains(property.Key))
                    {
                        continue;
                    }

                    var value = property.Value;

                    if (value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var stringValue))
                    {
                        var sanitizedValue = SanitizePreservingOriginal(stringValue);

                        if (!string.Equals(stringValue, sanitizedValue, StringComparison.Ordinal))
                        {
                            jsonObject[property.Key] = sanitizedValue;
                            modified = true;
                        }
                    }
                    else if (value is not null)
                    {
                        if (SanitizeJsonNode(value))
                        {
                            modified = true;
                        }
                    }
                }
            }
            else if (node is JsonArray jsonArray)
            {
                for (var i = 0; i < jsonArray.Count; i++)
                {
                    var item = jsonArray[i];

                    if (item is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var stringValue))
                    {
                        var sanitizedValue = SanitizePreservingOriginal(stringValue);

                        if (!string.Equals(stringValue, sanitizedValue, StringComparison.Ordinal))
                        {
                            jsonArray[i] = sanitizedValue;
                            modified = true;
                        }
                    }
                    else if (item is not null)
                    {
                        if (SanitizeJsonNode(item))
                        {
                            modified = true;
                        }
                    }
                }
            }

            return modified;
        }

        private static bool IsOnlyNormalizationDifference(string original, string sanitized)
        {
            if (string.Equals(original, sanitized, StringComparison.Ordinal))
                return true;

            if (string.IsNullOrEmpty(sanitized) && !string.IsNullOrEmpty(original))
                return false;

            try
            {
                var parser = new HtmlParser();

                var originalDocument = parser.ParseDocument(original);
                var sanitizedDocument = parser.ParseDocument(sanitized);

                return AreEquivalent(originalDocument.Body, sanitizedDocument.Body);
            }
            catch
            {
                return false;
            }
        }

        private static (string Value, bool Important) SplitImportant(string raw)
        {
            var trimmed = raw.Trim();
            var match = ImportantSuffixPattern.Match(trimmed);

            if (!match.Success)
                return (trimmed, false);

            return (trimmed[..match.Index].TrimEnd(), true);
        }

        private static string NormalizeCssValue(string property, string value)
        {
            var (bareValue, important) = SplitImportant(value);

            var trimmed = Regex.Replace(bareValue.Trim(), @"\s+", " ");
            string normalized;

            if (property.Equals("font-family", StringComparison.OrdinalIgnoreCase))
            {
                normalized = string.Join(",", trimmed.Split(',').Select(f => f.Trim().Trim('"', '\'').ToLowerInvariant()));
            }
            else
            {
                var rgbMatch = RgbColorPattern.Match(trimmed);
                if (rgbMatch.Success)
                {
                    var r = rgbMatch.Groups[1].Value;
                    var g = rgbMatch.Groups[2].Value;
                    var b = rgbMatch.Groups[3].Value;

                    var alpha = 1d;
                    if (rgbMatch.Groups[4].Success)
                    {
                        double.TryParse(rgbMatch.Groups[4].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out alpha);
                    }

                    normalized = $"rgba({r},{g},{b},{alpha.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
                }
                else
                {
                    var hexMatch = HexColorPattern.Match(trimmed);
                    if (hexMatch.Success)
                    {
                        var hex = hexMatch.Groups["hex"].Value;

                        if (hex.Length == 3)
                        {
                            hex = string.Concat(hex.Select(c => new string(c, 2)));
                        }

                        var r = Convert.ToInt32(hex.Substring(0, 2), 16);
                        var g = Convert.ToInt32(hex.Substring(2, 2), 16);
                        var b = Convert.ToInt32(hex.Substring(4, 2), 16);

                        var alpha = 1d;
                        if (hex.Length == 8)
                        {
                            var a = Convert.ToInt32(hex.Substring(6, 2), 16);
                            alpha = Math.Round(a / 255d, 3);
                        }

                        normalized = $"rgba({r},{g},{b},{alpha.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
                    }
                    else
                    {
                        normalized = ZeroWithUnitPattern.Replace(trimmed, "0").ToLowerInvariant();
                    }
                }
            }

            return important ? $"{normalized} !important" : normalized;
        }

        private static bool IsColorLikeValue(string value)
        {
            var (bareValue, _) = SplitImportant(value);
            return ColorLikeValuePattern.IsMatch(bareValue.Trim());
        }

        private static bool IsDangerousCssDeclaration(string value)
        {
            foreach (var marker in DangerousCssValueSubstrings)
            {
                if (value.Contains(marker, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool AreEquivalentStyles(string? original, string? sanitized)
        {
            static Dictionary<string, string> ParseDeclarations(string? style)
            {
                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                if (string.IsNullOrWhiteSpace(style))
                    return result;

                foreach (var declaration in style.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var parts = declaration.Split(':', 2, StringSplitOptions.TrimEntries);

                    if (parts.Length != 2)
                        continue;

                    result[parts[0]] = parts[1].Trim();
                }

                return result;
            }

            var originalDeclarations = ParseDeclarations(original);
            var sanitizedDeclarations = ParseDeclarations(sanitized);

            foreach (var (property, sanitizedRawValue) in sanitizedDeclarations)
            {
                if (IsDangerousCssDeclaration(sanitizedRawValue))
                    return false;

                if (originalDeclarations.TryGetValue(property, out var originalRawValue))
                {
                    if (IsDangerousCssDeclaration(originalRawValue))
                        return false;

                    var normalizedOriginal = NormalizeCssValue(property, originalRawValue);
                    var normalizedSanitized = NormalizeCssValue(property, sanitizedRawValue);

                    if (string.Equals(normalizedOriginal, normalizedSanitized, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (ColorValuedProperties.Contains(property) &&
                        !IsColorLikeValue(originalRawValue))
                    {
                        return false;
                    }

                    continue;
                }
            }

            foreach (var (property, value) in originalDeclarations)
            {
                if (sanitizedDeclarations.ContainsKey(property))
                    continue;

                if (IsDangerousCssDeclaration(value))
                    return false;
            }

            return true;
        }

        private static bool IsSafeToDropAttribute(string name)
        {
            return SafeToDropAttributes.Contains(name) || WordNamespaceAttributePattern.IsMatch(name);
        }

        private static bool IsWordNoiseElement(INode node)
        {
            return node is IElement element && WordNamespaceElementPattern.IsMatch(element.TagName);
        }

        private static IEnumerable<INode> RelevantChildren(INode node)
        {
            return node.ChildNodes.Where(n => n is not IComment && !IsWordNoiseElement(n));
        }

        private static bool AreEquivalent(INode? original, INode? sanitized)
        {
            if (original == null || sanitized == null)
                return original == sanitized;

            if (original.NodeType != sanitized.NodeType)
                return false;

            if (original is IElement originalElement &&
                sanitized is IElement sanitizedElement)
            {
                if (!string.Equals(originalElement.TagName, sanitizedElement.TagName, StringComparison.OrdinalIgnoreCase))
                    return false;

                var attrNames = originalElement.Attributes
                    .Select(a => a.Name)
                    .Union(sanitizedElement.Attributes.Select(a => a.Name), StringComparer.OrdinalIgnoreCase);

                foreach (var attrName in attrNames)
                {
                    var originalValue = originalElement.GetAttribute(attrName);
                    var sanitizedValue = sanitizedElement.GetAttribute(attrName);

                    if (attrName.Equals("style", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!AreEquivalentStyles(originalValue, sanitizedValue))
                            return false;

                        continue;
                    }

                    if (string.Equals(originalValue, sanitizedValue, StringComparison.Ordinal))
                        continue;

                    if (sanitizedValue is null && IsSafeToDropAttribute(attrName))
                        continue;

                    return false;
                }
            }
            else if (original is IText originalText && sanitized is IText sanitizedText)
            {
                if (!string.Equals(originalText.Data, sanitizedText.Data, StringComparison.Ordinal))
                    return false;
            }

            var originalChildren = RelevantChildren(original).ToList();
            var sanitizedChildren = RelevantChildren(sanitized).ToList();

            if (originalChildren.Count != sanitizedChildren.Count)
                return false;

            for (var i = 0; i < originalChildren.Count; i++)
            {
                if (!AreEquivalent(originalChildren[i], sanitizedChildren[i]))
                    return false;
            }

            return true;
        }

        private string SanitizePreservingOriginal(string original)
        {
            var sanitized = _sanitizer.Sanitize(original);

            if (string.Equals(original, sanitized, StringComparison.Ordinal))
                return original;

            var isEquivalent = IsOnlyNormalizationDifference(original, sanitized);

            if (isEquivalent)
                return original;

            return sanitized;
        }

        private async Task<bool> RejectHarmfulRequestAsync(HttpContext context)
        {
            _logger.LogWarning("Request rejected because potentially harmful HTML content was detected. Path: {Path}", context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";

            var apiResponse = new ApiResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                CustomCodeStatus = CustomCodeStatus.SomethingWentWrong,
                Message = Resource.PotentiallyUnsafeContentDetected
            };

            await context.Response.WriteAsJsonAsync(apiResponse);

            return false;
        }
    }
}