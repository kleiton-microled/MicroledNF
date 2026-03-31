using FluentValidation;
using Microled.Nfe.LocalAgent.Api.Configuration;
using Microled.Nfe.LocalAgent.Api.Endpoints;
using Microled.Nfe.LocalAgent.Api.Services;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Application.Services;
using Microled.Nfe.Service.Application.UseCases;
using Microled.Nfe.Service.Application.Validators;
using Microled.Nfe.Service.Business.Services;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Infra.Client;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Interfaces;
using Microled.Nfe.Service.Infra.Repositories;
using Microled.Nfe.Service.Infra.Services;
using Microled.Nfe.Service.Infra.Storage;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<IRpsXmlValidationExportService, RpsXmlValidationExportService>();
builder.Services.AddScoped<IWebServiceProbeService, WebServiceProbeService>();
builder.Services.AddScoped<CertificateUnlockService>();
builder.Services.AddScoped<LocalRpsProcessingService>();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient(nameof(NfeSoapClient), (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<NfeServiceOptions>>().Value;
    var endpoint = !string.IsNullOrWhiteSpace(options.BaseUrl)
        ? options.BaseUrl
        : options.UseProduction
            ? options.ProductionEndpoint
            : options.TestEndpoint;

    if (string.IsNullOrWhiteSpace(endpoint))
    {
        throw new InvalidOperationException(
            "NfeServiceOptions:BaseUrl or the environment-specific endpoint must be configured.");
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

    return new NfeSoapClient(
        httpClient,
        logger,
        options,
        xmlSerializer,
        soapEnvelopeBuilder,
        certificateProvider,
        rpsSignatureService);
});

builder.Services.AddScoped<ISendRpsUseCase, SendRpsUseCase>();
builder.Services.AddScoped<IConsultNfeUseCase, ConsultNfeUseCase>();
builder.Services.AddScoped<ICancelNfeUseCase, CancelNfeUseCase>();
builder.Services.AddScoped<IListCertificatesUseCase, ListCertificatesUseCase>();
builder.Services.AddScoped<ISelectCertificateUseCase, SelectCertificateUseCase>();
builder.Services.AddScoped<IUpsertCompanyCertificateProfileUseCase, UpsertCompanyCertificateProfileUseCase>();
builder.Services.AddScoped<IGetActiveCertificateProfileUseCase, GetActiveCertificateProfileUseCase>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("LocalAgentCors");

app.MapHealthEndpoints();
app.MapCertificatesEndpoints();
app.MapNfeEndpoints();
app.MapRpsEndpoints();
app.MapWebServiceProbeEndpoints();

app.Run();
