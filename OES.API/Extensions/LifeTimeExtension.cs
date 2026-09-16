using Microsoft.Extensions.Options;
using OES.Api.Hubs;
using OES.API.BackgroundServices;
using OES.API.GRPCService;
using OES.API.Hubs.HubServices;
using OES.Helper.Dtos.AIItemBankGenerator.Request;
using OES.Helper.Dtos.AIItemBankGenerator.Response;
using OES.Helper.Dtos.AIQuestionGenerator.Request;
using OES.Helper.Dtos.AIQuestionGenerator.Response;
using OES.Helper.General;
using OES.Helper.General.DocLibBackEndHttpClientHelper;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;
using OES.Interface.Repository;
using OES.Interface.UnitOfWork;
using OES.Services.MappingProfile;
using OES.Services.ParallelService;
using OES.Services.Prompt;
using OES.Services.Repository;
using OES.Services.Services;
using OES.Services.Services.Reports;
using OES.Services.UnitOfWork;
using OES.Services.Validators;
using SharedHelper.General;

namespace OES.API.Extensions
{
    public static class LifeTimeExtension
    {
        public static IServiceCollection AddServicesLifeTime(this IServiceCollection services)
        {
            services.AddSingleton<IApiResponse, ApiResponse>();
            services.AddScoped(typeof(IRepository<,>), typeof(RepositoryService<,>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ICommonService, CommonService>();
            services.AddScoped(typeof(IPaginationSearchModel), typeof(PaginationSearchModel));
            services.AddAutoMapper(typeof(MappingProfile).Assembly);
            services.AddScoped<IItemBankService, ItemBankService>();
            services.AddScoped<IValidatorItemBankService, ValidatorItemBankService>();
            services.AddScoped<IILOService, ILOService>();
            services.AddScoped<IILOValidatorService, ILOValidatorService>();
            services.AddScoped<IScheduleService, ScheduleService>();
            services.AddScoped<IQuestionService, QuestionService>();
            services.AddScoped<IQuestionValidatorService, QuestionValidatorService>();
            services.AddScoped<IItemBankLevelService, ItemBankLevelService>();
            services.AddScoped<FilterParamsValues>();
            services.AddScoped<IAPIMemoryCach, APIMemoryCach>();
            services.AddScoped<IUserProfileService, UserProfileService>();
            services.AddScoped<IPageService, PageService>();
            services.AddScoped<IAppRoleService, AppRoleService>();
            services.AddScoped<IPathsService, PathsService>();
            services.AddScoped<ISeederService, SeederService>();
            services.AddScoped<IQuestionTypeService, QuestionTypeService>();
            services.AddScoped<IQuestionCategoryService, QuestionCategoryService>();
            services.AddScoped<IDeltaTypeService, DeltaTypeService>();
            services.AddScoped<IDifficultyProfileService, DifficultyProfileService>();
            services.AddScoped<ITextEditorService, TextEditorService>();
            services.AddScoped<ISubjectService, SubjectService>();
            services.AddScoped<IDifficultyLevelService, DifficultyLevelService>();
            services.AddScoped<ILanguageService, LanguageService>();
            services.AddScoped<IQuestionLayoutService, QuestionLayoutService>();
            services.AddScoped<IQuestionMetadataService, QuestionMetadataService>();
            services.AddScoped<IQCommentService, QCommentService>();
            services.AddScoped<IQTISerializeService, QTISerializeService>();
            services.AddScoped<IUserProfileGRPCService, UserProfileGRPCService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IUserSyncService, UserSyncService>();
            services.AddScoped<IAppGroupService, AppGroupService>();
            services.AddScoped<IAppGroupValidatorService, AppGroupValidatorService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IPaperService, PaperService>();
            services.AddScoped<ITemplateService, TemplateService>();
            services.AddScoped<IMarkingSchemeService, MarkingSchemeService>();
            services.AddScoped<ISectionSummaryService, SectionSummaryService>();
            services.AddSingleton<INotificationHubService, NotificationHubService>();
            services.AddScoped<DocLibBackEndHttpClientHelper>();
            services.AddScoped<ParallelQueryService>();
            services.AddScoped<IBlockService, BlockService>();
            services.AddScoped<ISectionService, SectionService>();
            services.AddScoped<ITransitionProfileService, TransitionProfileService>();
            services.AddScoped<ITransitionLevelService, TransitionLevelService>();
            services.AddScoped<IItemBankPointService, ItemBankPointService>();
            services.AddScoped<IPaperSettingsService, PaperSettingsService>();
            services.AddScoped<ISecurityConfigurationService, SecurityConfigurationService>();
            services.AddScoped<IVenueService, VenueService>();
            services.AddScoped<ICandidateService, CandidateService>();
            services.AddScoped<ISchedulePaperService, SchedulePaperService>();
            services.AddScoped<IOrganizationStructureService, OrganizationStructureService>();
            services.AddScoped<IQuestionDistributionValidationService, QuestionDistributionValidationService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IQuestionHtmlHelperService, QuestionHtmlHelperService>();
            services.AddScoped<IFormService, FormService>();
            services.AddScoped<IFormQuestionScoreService, FormQuestionScoreService>();
            services.AddScoped<IExamServerService, ExamServerService>();
            services.AddScoped<ICandidateBatchImportHistoryService, CandidateBatchImportHistoryService>();
            services.AddScoped<IQuestionInstructionTemplateService, QuestionInstructionTemplateService>();
            services.AddScoped<IQuestionUploadTemplateService, QuestionUploadTemplateService>();
            services.AddScoped<IJobManagementService, JobManagementService>();
            services.AddSingleton<IPayloadStorageService, MinioPayloadStorageService>();
            services.AddScoped<ICandidatesResultService, CandidatesResultService>();
            services.AddScoped<IQualityCheckCommitteeService, QualityCheckCommitteeService>();
            services.AddScoped<IEquationTemplateService, EquationTemplateService>();
            services.AddScoped<ICBTCandidatesSyncService, CBTCandidatesSyncService>();
            services.AddScoped<IAutoSyncSchedulesService, AutoSyncSchedulesService>();
            services.AddScoped<IPdfGeneratorService, PdfGeneratorService>();
            services.AddScoped<IOesRoleTemplateService, OesRoleTemplateService>();
            services.AddScoped<IAutoPermissionAssignmentService, AutoPermissionAssignmentService>();
            services.AddScoped<IItemBankAuthorizationService, ItemBankAuthorizationService>();
            services.AddHostedService<AutoSyncHostedService>();
            services.AddScoped<IQueueSuspendService, QueueSuspendService>();
            services.AddScoped<IMediaSettingService, MediaSettingService>();
            services.AddScoped<ICBTSyncSettingService, CBTSyncSettingService>();
            services.AddScoped<IAnalyticalReportsService, AnalyticalReportsService>();
            services.AddScoped<IAnalyticalReportsService, AnalyticalReportsService>();
            services.AddScoped<IVerificationReportsService, VerificationReportsService>();
            services.AddScoped<IResultsReportsService, ResultsReportsService>();
            services.AddScoped<IDisabilityService, DisabilityService>();
            services.AddScoped<ISyncNotificationService, SyncNotificationService>();
            services.AddScoped<IClientIpProviderService, ClientIpProviderService>();
            services.AddScoped<IAuditLogsService, AuditLogsService>();
            services.AddScoped<IEvaluationSyncService, EvaluationSyncService>();
            services.AddScoped<IAIFeatureAccessService, AIFeatureAccessService>();
            services.AddScoped<IDocumentExtractorService, DocumentExtractorService>();
            services.AddSingleton<IPromptTemplate<AIQuestionGenerationPromptContextDto>, QuestionGenerationPrompt>();
            services.AddSingleton<IPromptTemplate<AIItemBankGenerationPromptContextDto>, ItemBankGenerationPrompt>();
            services.AddScoped<IAIResponseGeneratorService, AIResponseGeneratorService>();
            services.AddScoped<IAIQuestionGenerationService, AIQuestionGenerationService>();
            services.AddScoped<IAIItemBankGenerationService, AIItemBankGenerationService>();
            services.AddSingleton<AIAssetStorage>();
            services.AddSingleton<MathContentProcessingService>();
            services.AddHostedService<AIWarmupHostedService>();
            services.AddScoped<IAIResponseResultValidator<AIGeneratedQuestionsResultDto, AIQuestionGenerationPromptContextDto>, QuestionGenerationValidator>();
            services.AddScoped<IAIResponseResultValidator<AIGeneratedItemBankWrapperDto, AIItemBankGenerationPromptContextDto>, ItemBankGenerationValidator>();
            services.AddTransient<OriginalClientIpHandler>();
            services.AddHttpClient();
            services.AddHttpClient("DocLibApi", client =>
            {
                client.BaseAddress = new Uri(CentralizedUrlHelper.DocLibApiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(100);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
            });
            services.AddHttpClient("CesApi", client =>
            {
                client.BaseAddress = new Uri(CentralizedUrlHelper.CesApiBaseUrl);
                client.Timeout = TimeSpan.FromHours(5);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
            });
            services.AddHttpClient("SsoHttpClient", client =>
            {
                client.BaseAddress = new Uri(CentralizedUrlHelper.SsoApiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(100);
            })
            .AddHttpMessageHandler<OriginalClientIpHandler>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
            });

            services.AddOptions<DoclingSettings>().BindConfiguration(nameof(DoclingSettings));
            services.AddHttpClient<IDoclingClient, DoclingClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<DoclingSettings>>().Value;
                var baseUrl = settings.BaseUrl;

                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds > 0 ? settings.TimeoutSeconds : 1000);
            });

            services.AddControllers();

            return services;
        }
    }
}