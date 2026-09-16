using Aspire.ServiceDefaults;
using Hangfire;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;
using OES.API.Extensions;
using OES.API.GRPCService;
using OES.API.Hubs;
using OES.API.MiddleWare;
using OES.Helper.General;
using OES.Interface.GenericMemoryCacheRepository;
using OES.Services.GenericMemoryCacheRepository;
using OES.Services.Services.HealthChecks;
using SharedHelper.General;
using System.Globalization;
using System.Text.Json.Serialization;

const string CORS_POLICY_NAME = "OesCorsPolicy";

var builder = WebApplication.CreateBuilder(args);

CentralizedUrlHelper.InitializeUrlsFromApplicationSettings(builder.Configuration);

builder.AddServiceDefaults();

builder.Services.AddAppContext(builder.Configuration);

var allowedOrigins = new[]
{
    CentralizedUrlHelper.SsoBlazorBaseUrl.TrimEnd('/'),
    CentralizedUrlHelper.OesBlazorBaseUrl.TrimEnd('/'),
    CentralizedUrlHelper.CesBlazorBaseUrl.TrimEnd('/'),
    CentralizedUrlHelper.ProctoringHubBlazorBaseUrl.TrimEnd('/'),
    CentralizedUrlHelper.EvaluationBlazorBaseUrl.TrimEnd('/')
};

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(CORS_POLICY_NAME,
            policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Content-Type");
            }
        );
    }
);

builder.Services.AddRabbitMQ(builder);
builder.Services.AddGrpc();
builder.Services.AddSignalR();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddHttpContextAccessor();
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddAppContext(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddServicesLifeTime();
builder.Services.AddExcelMappingExtension();
builder.Services.AddMemoryCache();
builder.Services.AddAIService(builder.Configuration);
builder.Services.AddHealthChecks().AddCheck<AIChatHealthCheck>("ai", tags: ["ai", "llm"]);
builder.Services.AddSingleton<IMemoryCacheRepository, MemoryCacheRepository>();
builder.Services.AddScoped<FilterParamsValues>();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new List<CultureInfo>
    {
        new(LanguageCode.ENGLISH_CODE),
        new(LanguageCode.ARABIC_CODE)
    };

    options.DefaultRequestCulture = new RequestCulture(LanguageCode.ENGLISH_CODE);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    options.RequestCultureProviders =
    [
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    ];
});
builder.Services.AddLocalization(options => options.ResourcesPath = "../OES.Helper/ResourceFiles");
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddResponseCompression(opts => opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]));
builder.Services.AddHangfireOesServices(builder.Configuration);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
    ForwardedHeaders.XForwardedFor |
    ForwardedHeaders.XForwardedProto;
});

builder.Services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));

var app = builder.Build();

app.UseForwardedHeaders();

await app.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseSecurityHeaders();

app.UseConfiguredStaticFiles();

app.UseResponseCompression();

app.UseMiddleware<ExceptionMiddleware>();

//app.UseMiddleware<XContentTypeOptionsMiddleware>();

app.UseCors(CORS_POLICY_NAME);

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new AllowAllConnectionsAuthorizationFilter()],
    DashboardTitle = "Oes System - Candidate Import Dashboard",
    DarkModeEnabled = true
});

app.UseContentSecurityPolicy(reportOnly: true);

app.UseGrpcWeb();

app.UseHttpsRedirection();

_ = app.Use(async (context, next) =>
{
    context.Response.Headers.XFrameOptions = "DENY";
    await next();
});

app.UseMiddleware<EncryptionMiddleware>();

var localizeOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>();
app.UseRequestLocalization(localizeOptions.Value);

app.UseMiddleware<HtmlSanitizationMiddleware>();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapGrpcService<ItemBankTreeGrpcService>().EnableGrpcWeb().RequireCors(CORS_POLICY_NAME);

app.MapGrpcService<IloTreeGrpcService>().EnableGrpcWeb().RequireCors(CORS_POLICY_NAME);

app.MapGrpcService<OrganizationStructureGrpcService>().EnableGrpcWeb().RequireCors(CORS_POLICY_NAME);

app.MapHub<NotificationHub>("/notificationHub").RequireCors(CORS_POLICY_NAME);

app.MapHub<SyncDashboardHub>("/syncDashboardHub").RequireCors(CORS_POLICY_NAME);

app.MapHealthChecks("/health/ai", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ai")
});

app.Run();