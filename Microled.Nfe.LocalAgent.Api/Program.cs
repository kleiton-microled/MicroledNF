using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microled.Nfe.LocalAgent.Api.Configuration;
using Microled.Nfe.LocalAgent.Api.Endpoints;
using Microled.Nfe.LocalAgent.Api.Services;
using Microled.Nfe.Service.Application.Configuration;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Application.NfseSpTax;
using Microled.Nfe.Service.Application.Services;
using Microled.Nfe.Service.Application.UseCases;
using Microled.Nfe.Service.Application.Validators;
using Microled.Nfe.Service.Business.Services;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Infra.Client;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Interfaces;
using Microled.Nfe.Service.Infra.Mapping;
using Microled.Nfe.Service.Infra.Repositories;
using Microled.Nfe.Service.Infra.Services;
using Microled.Nfe.Service.Infra.Storage;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    var json = options.SerializerOptions;
    json.PropertyNameCaseInsensitive = true;
    json.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    json.Converters.Add(new JsonStringEnumConverter());
});

var localAgentOptions = builder.Configuration
    .GetSection(LocalAgentOptions.SectionName)
    .Get<LocalAgentOptions>() ?? new LocalAgentOptions();

builder.WebHost.UseUrls($"http://localhost:{localAgentOptions.Port}");

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.Configure<LocalAgentOptions>(
    builder.Configuration.GetSection(LocalAgentOptions.SectionName));
builder.Services.Configure<NfeIntegrationOptions>(
    builder.Configuration.GetSection(NfeIntegrationOptions.SectionName));
builder.Services.Configure<NfeValidationOptions>(
    builder.Configuration.GetSection(NfeValidationOptions.SectionName));
builder.Services.Configure<WebServiceProbeOptions>(
    builder.Configuration.GetSection(WebServiceProbeOptions.SectionName));
builder.Services.Configure<AccessDatabaseOptions>(
    builder.Configuration.GetSection(AccessDatabaseOptions.SectionName));
builder.Services.Configure<LocalCertificateProfileStorageOptions>(options =>
{
    options.DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Microled",
        "Nfe",
        "localagent");
    options.FileName = "profiles.json";
});
builder.Services.AddOptions<NfeServiceOptions>()
    .Configure<IConfiguration>((options, configuration) =>
    {
        configuration.GetSection("NfeServiceOptions").Bind(options);
        configuration.GetSection("Certificate").Bind(options.Certificate);
    });
builder.Services.Configure<IbptCargaTributariaOptions>(
    builder.Configuration.GetSection(IbptCargaTributariaOptions.SectionName));

var allowedOrigins = localAgentOptions.AllowedOrigins.Count > 0
    ? localAgentOptions.AllowedOrigins.ToArray()
    : ["http://localhost:4200", "http://127.0.0.1:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalAgentCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .WithMethods("GET", "POST")
            .WithHeaders("Content-Type", "Authorization");
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Microled NFE Local Agent API",
        Version = "v1",
        Description = "Minimal API local para certificados e processamento de RPS/NFS-e."
    });
});
builder.Services.AddValidatorsFromAssemblyContaining<SendRpsRequestDtoValidator>();

builder.Services.AddScoped<IRpsSignatureService, RpsSignatureService>();
builder.Services.AddScoped<INfeCancellationSignatureService, NfeCancellationSignatureService>();
builder.Services.AddScoped<ICertificateDiscoveryService, WindowsCertificateDiscoveryService>();
builder.Services.AddScoped<ICompanyCertificateProfileRepository, JsonCompanyCertificateProfileRepository>();
builder.Services.AddScoped<ICertificateProvider, CertificateProvider>();
builder.Services.AddScoped<IRpsBatchPreparationService, RpsBatchPreparationService>();
builder.Services.AddScoped<IXmlSerializerService>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<XmlSerializerService>>();
    var options = serviceProvider.GetRequiredService<IOptions<NfeServiceOptions>>();
    var certificateProvider = serviceProvider.GetRequiredService<ICertificateProvider>();
    return new XmlSerializerService(logger, options, certificateProvider);
});
builder.Services.AddScoped<ISoapEnvelopeBuilder, SoapEnvelopeBuilder>();
builder.Services.AddScoped<IEnvioLoteRpsPedidoMapper, EnvioLoteRpsPedidoMapper>();
builder.Services.AddScoped<IRpsXmlValidationExportService, RpsXmlValidationExportService>();
builder.Services.AddScoped<IWebServiceProbeService, WebServiceProbeService>();
builder.Services.AddScoped<IAccessRpsRepository, AccessRpsRepository>();
builder.Services.AddScoped<AccessRpsPayloadMapper>();
builder.Services.AddScoped<ConsultaNfeXsdValidator>();
builder.Services.AddScoped<CancelamentoNfeXsdValidator>();
builder.Services.AddScoped<CertificateUnlockService>();
builder.Services.AddScoped<LocalRpsProcessingService>();
builder.Services.AddSingleton<INfseSpFederalTaxRuleProvider, NfseSpFederalTaxRuleProvider>();
builder.Services.AddScoped<INfseSpTaxCalculationService, NfseSpTaxCalculationService>();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient(nameof(NfeSoapClient), (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<NfeServiceOptions>>().Value;
    var endpoint = options.GetPrimaryEndpoint();

    if (string.IsNullOrWhiteSpace(endpoint))
    {
        throw new InvalidOperationException(
            "NfeServiceOptions must define BaseUrl, the query endpoint, or the async send endpoint for the current environment.");
    }

    client.BaseAddress = new Uri(endpoint);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
})
.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
{
    var certificateProvider = serviceProvider.GetRequiredService<ICertificateProvider>();
    var certificate = certificateProvider.GetCertificate();

    var handler = new HttpClientHandler
    {
        ClientCertificateOptions = ClientCertificateOption.Manual,
        SslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                       System.Security.Authentication.SslProtocols.Tls13
    };
    handler.ClientCertificates.Add(certificate);

    return handler;
});

builder.Services.AddScoped<INfeGateway>(serviceProvider =>
{
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient(nameof(NfeSoapClient));
    var logger = serviceProvider.GetRequiredService<ILogger<NfeSoapClient>>();
    var options = serviceProvider.GetRequiredService<IOptions<NfeServiceOptions>>();
    var xmlSerializer = serviceProvider.GetRequiredService<IXmlSerializerService>();
    var soapEnvelopeBuilder = serviceProvider.GetRequiredService<ISoapEnvelopeBuilder>();
    var certificateProvider = serviceProvider.GetRequiredService<ICertificateProvider>();
    var rpsSignatureService = serviceProvider.GetRequiredService<IRpsSignatureService>();
    var pedidoMapper = serviceProvider.GetRequiredService<IEnvioLoteRpsPedidoMapper>();
    var consultaNfeXsdValidator = serviceProvider.GetRequiredService<ConsultaNfeXsdValidator>();
    var cancelamentoNfeXsdValidator = serviceProvider.GetRequiredService<CancelamentoNfeXsdValidator>();

    return new NfeSoapClient(
        httpClient,
        logger,
        options,
        xmlSerializer,
        soapEnvelopeBuilder,
        pedidoMapper,
        certificateProvider,
        rpsSignatureService,
        consultaNfeXsdValidator,
        cancelamentoNfeXsdValidator);
});

builder.Services.AddScoped<ISendRpsUseCase, SendRpsUseCase>();
builder.Services.AddScoped<IConsultBatchStatusUseCase, ConsultBatchStatusUseCase>();
builder.Services.AddScoped<IConsultNfeUseCase, ConsultNfeUseCase>();
builder.Services.AddScoped<ICancelNfeUseCase, CancelNfeUseCase>();
builder.Services.AddScoped<IListCertificatesUseCase, ListCertificatesUseCase>();
builder.Services.AddScoped<ISelectCertificateUseCase, SelectCertificateUseCase>();
builder.Services.AddScoped<IUpsertCompanyCertificateProfileUseCase, UpsertCompanyCertificateProfileUseCase>();
builder.Services.AddScoped<IGetActiveCertificateProfileUseCase, GetActiveCertificateProfileUseCase>();

var app = builder.Build();

var startupLogger = app.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("Microled.Nfe.LocalAgent.Api.Startup");
var nfeOptions = app.Services.GetRequiredService<IOptions<NfeServiceOptions>>().Value;
var integrationOptions = app.Services.GetRequiredService<IOptions<NfeIntegrationOptions>>().Value;
var validationOptions = app.Services.GetRequiredService<IOptions<NfeValidationOptions>>().Value;
var certificateStorageOptions = app.Services.GetRequiredService<IOptions<LocalCertificateProfileStorageOptions>>().Value;
var resolvedQueryEndpoint = nfeOptions.GetQueryEndpoint();
var resolvedSendEndpoint = nfeOptions.GetSendEndpoint();
var localUrl = $"http://localhost:{localAgentOptions.Port}";

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("LocalAgentCors");

app.Use(async (context, next) =>
{
    var httpLog = context.RequestServices.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Microled.Nfe.LocalAgent.Http");
    httpLog.LogInformation(
        ">>> {Method} {Path} ContentLength={ContentLength}",
        context.Request.Method,
        context.Request.Path,
        context.Request.ContentLength);
    await next();
    httpLog.LogInformation("<<< {StatusCode} {Path}", context.Response.StatusCode, context.Request.Path);
});

app.MapHealthEndpoints();
app.MapAccessEndpoints();
app.MapCertificatesEndpoints();
app.MapNfeEndpoints();
app.MapRpsEndpoints();
app.MapWebServiceProbeEndpoints();

app.Lifetime.ApplicationStarted.Register(() =>
{
    startupLogger.LogInformation("========================================");
    startupLogger.LogInformation("Microled.Nfe.LocalAgent.Api started");
    startupLogger.LogInformation("Local URL: {LocalUrl}", localUrl);
    startupLogger.LogInformation("ASP.NET Environment: {AspNetEnvironment}", app.Environment.EnvironmentName);
    startupLogger.LogInformation("NFS-e Environment: {NfeEnvironment}", nfeOptions.Environment);
    startupLogger.LogInformation("NFS-e Query/Cancel Endpoint: {ResolvedQueryEndpoint}", resolvedQueryEndpoint ?? "(not configured)");
    startupLogger.LogInformation(
        "NFS-e Send Endpoint: {ResolvedSendEndpoint} ({ContractMode})",
        resolvedSendEndpoint ?? "(not configured)",
        nfeOptions.UseAsyncSendContract() ? "async" : "sync");
    startupLogger.LogInformation("UseProduction: {UseProduction}", nfeOptions.UseProduction);
    startupLogger.LogInformation("TimeoutSeconds: {TimeoutSeconds}", nfeOptions.TimeoutSeconds);
    startupLogger.LogInformation("Certificate Mode: {CertificateMode}", nfeOptions.Certificate.Mode);
    startupLogger.LogInformation(
        "Certificate Store: {StoreLocation}/{StoreName}",
        nfeOptions.Certificate.StoreLocation ?? "CurrentUser",
        nfeOptions.Certificate.StoreName ?? "My");
    startupLogger.LogInformation(
        "Selected certificate profile file: {ProfileFilePath}",
        Path.Combine(certificateStorageOptions.DataDirectory, certificateStorageOptions.FileName));
    startupLogger.LogInformation(
        "RPS Output Directory: {RpsOutputDirectory}",
        integrationOptions.RpsOutputDirectory ?? "(not configured)");
    startupLogger.LogInformation(
        "Validation Output Directory: {ValidationOutputDirectory}",
        validationOptions.OutputDirectory);
    startupLogger.LogInformation(
        "Allowed Origins: {AllowedOrigins}",
        string.Join(", ", allowedOrigins));
    startupLogger.LogInformation("========================================");
});

app.Run();
