using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Localization;
using MudExtensions.Services;
using OES.Blazor;
using OES.Blazor.Extensions;
using OES.Blazor.Protos;
using OES.Blazor.Services.Implementation.CandidateBatchImportHistory;
using OES.Blazor.Services.Interfaces.CandidateBatchImportHistory;
using OES.Helper.General;
using OES.Helper.Interfaces;
using SharedHelper.General;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

CentralizedUrlHelper.InitializeUrlsFromApplicationSettings(builder.Configuration);

// Add root components
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add services required by the application
builder.Services.RegisterServices(builder.HostEnvironment);
builder.Services.AddMudExtensions();

// Configure GRPC Clients:
var grpcUri = CentralizedUrlHelper.OesApiBaseUrl;

builder.Services.AddGrpcClient<ItemBankTreeService.ItemBankTreeServiceClient>(options
    => options.Address = new Uri(grpcUri)).ConfigurePrimaryHttpMessageHandler(()
    => new GrpcWebHandler(GrpcWebMode.GrpcWebText, new HttpClientHandler())).ConfigureChannel(channelOptions =>
{
    channelOptions.MaxReceiveMessageSize = int.MaxValue;
    channelOptions.MaxSendMessageSize = int.MaxValue;
});

builder.Services.AddGrpcClient<IloTreeService.IloTreeServiceClient>(options
    => options.Address = new Uri(grpcUri)).ConfigurePrimaryHttpMessageHandler(()
    => new GrpcWebHandler(GrpcWebMode.GrpcWebText, new HttpClientHandler())).ConfigureChannel(channelOptions =>
{
    channelOptions.MaxReceiveMessageSize = int.MaxValue;
    channelOptions.MaxSendMessageSize = int.MaxValue;
});

builder.Services.AddGrpcClient<OrganizationStructureTreeService.OrganizationStructureTreeServiceClient>(options
    => options.Address = new Uri(grpcUri)).ConfigurePrimaryHttpMessageHandler(()
    => new GrpcWebHandler(GrpcWebMode.GrpcWebText, new HttpClientHandler())).ConfigureChannel(channelOptions =>
{
    channelOptions.MaxReceiveMessageSize = int.MaxValue;
    channelOptions.MaxSendMessageSize = int.MaxValue;
});

builder.Services.AddHttpClient("SsoHttpClient", client =>
{
    client.BaseAddress = new Uri(CentralizedUrlHelper.SsoApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(100);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
});

//builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddScoped<CultureService>();
builder.Services.AddScoped(typeof(IRowMapper), typeof(RowMapper));
builder.Services.AddScoped<IBlazCandidateBatchImportHistoryService, BlazCandidateBatchImportHistoryService>();

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

if (builder.HostEnvironment.IsProduction())
{
    builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.None);
    builder.Logging.SetMinimumLevel(LogLevel.Warning);
}


var host = builder.Build();


// Retrieve the culture from local storage and set it:
var cultureService = host.Services.GetRequiredService<CultureService>();
var culture = await cultureService.GetCultureAsync();
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(culture);
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(culture);
CultureInfo.DefaultThreadCurrentCulture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
CultureInfo.DefaultThreadCurrentCulture.DateTimeFormat.LongDatePattern = "dd/MM/yyyy";
await host.RunAsync();