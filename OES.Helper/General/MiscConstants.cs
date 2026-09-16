namespace OES.Helper.General
{
    public static class MiscConstants
    {
        // WARNING: Don't localize this string until you calculate the risk of doing so; discuss team lead regarding this issue.
        public static string ManualQuestionsSelectionPool { get; } = "ManualQuestionsSelectionPool#57284631";

        public const string TemplatesFolder = "/files/Templates";

        public const string PageCssManagerRegister = "pageCssManager.register";

        public const string PageCssManagerUnregister = "pageCssManager.unregister";

        public const string ApplyExamStyleSheet = "StaticExamSimulationSheet.css";

        public const string LAST_SYNC_BATCH_ID_KEY = "oes:last_sync_batch_id";

        public const string ApplicationJsonContentType = "application/json";

        public const string PayloadEncryptionEnabled = "Security:PayloadEncryptionEnabled";

        public const string MinioSection = "Storage:Minio";

        public const string CBTApiSettingsSection = "IntegrationAPIs:CBTApiSettings";

        public const long CommonQuestionExhaustionCount = 2147483600;

        public const int QuestionBodyExpandingThreshold = 300;

        public const int QuestionBodyPreviewThreshold = 90;

        public const string SamlCookieName = "SsoSamlCookie";

        public const string AutoBypassQuestionComment = "Question bypassed automatically";

        public const string OrganizationSignature = "CAREERFIRST.SA";

        public const string EmailDomain = "{0}@etec.gov.sa";

        public const string DefaultSystemUser = "DefaultSystemUser";

        public const decimal MinAdaptiveDecimalValue = 0.000000000000001M;

        public const string ToggleArabicNumerals = "toggleArabicNumerals";

        public const string ScrollToElement = "scrollToElement";

        public const string Rtl = "rtl";

        public const string Ltr = "ltr";

        public const string AudioTypePrefix = "audio/";

        public const string AppCulture = "appCulture";

        public const string IsCreatePage = "IsCreatePage";

        public const string PerformViewBtnClick = "PerformViewBtnClick";

        public const string PerformEditBtnClick = "PerformEditBtnClick";

        public const string OpenInNewTab = "openInNewTab";

        public const string ExamScreenFormId = "ExamScreenFormId";

        public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public const string ExportedQuestionsFileName = "ExportedQuestions.xlsx";

        public const string CandidatesFileName = "Candidates.xlsx";

        public const string DownloadFileUsingFetch = "downloadFileUsingFetch";

        public const string IsApprovedQuestion = "IsApproved";

        public const string MinioAutoPurgeRuleId = "auto-purge-weekly";

        public const string MinioLifecycleEnabledStatus = "Enabled";

        public const string MinioLifecycleDisabledStatus = "Disabled";

        public const string File = "File";

        public const string AuditLogsStorageKey = "AuditLogs";

        public const string AuditLogsRegister = "auditLogs.register";

        public const string AuditLogsUnRegister = "auditLogs.unregister";

        public const string XOriginalClientIpHeader = "X-Original-Client-IP";

        public const string CfConnectingIpHeader = "CF-Connecting-IP";

        public const string XForwardedForHeader = "X-Forwarded-For";

        // Security Headers
        public const string XContentTypeOptionsHeader = "X-Content-Type-Options";

        public const string XContentTypeOptionsHeaderValue = "nosniff";

        // Encryption
        public const string DeveloperDisableEncryption = "OES_DEV_DISABLE_ENCRYPTION";

        public const string JwtBearerScheme = "Bearer";

        public const string OllamaClient = "OllamaClient";

        public const string AIGeneratorAuthor = "AI Generator";

        public const string TessDataFolder = "tessdata";

        public const string DefaultOcrLanguage = "eng+ara";

        public const long AIQuestionsGenerationMaxFileSizeInBytes = (long)(2.5 * 1024 * 1024);

        public const string XAPIKey = "X-API-Key";

        public const string UploadDocumentContent = "Document/UploadDocumentContent";

        public const string GetAIAsset = "api/AIAsset/GetAIAsset";

        public const string DoclingJsonFormat = "json";

        public const string DoclingOcrTesseract = "tesseract";

        public const string DoclingLangArabic = "ara";

        public const string DoclingLangEnglish = "eng";

        public const string DoclingImageExportModeEmbedded = "embedded";
    }
}