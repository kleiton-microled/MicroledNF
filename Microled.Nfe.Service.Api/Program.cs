using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microled.Nfe.Service.Api.Configuration;
using Microled.Nfe.Service.Api.HealthChecks;
using Microled.Nfe.Service.Api.Middleware;
using Microled.Nfe.Service.Api.Services;
using Microled.Nfe.Service.Application.Configuration;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Application.Services;
using Microled.Nfe.Service.Application.UseCases;
using Microled.Nfe.Service.Application.Validators;
using Microled.Nfe.Service.Business.Services;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Infra.Client;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Interfaces;
using Microled.Nfe.Service.Infra.Mapping;
using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Application.UseCases.NotasFiscais;
using Microled.Nfe.Service.Infra.Persistence;
using Microled.Nfe.Service.Infra.Repositories;
using Microled.Nfe.Service.Infra.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configure options
builder.Services.Configure<NfeServiceOptions>(
    builder.Configuration.GetSection(NfeServiceOptions.SectionName));
builder.Services.Configure<IbptCargaTributariaOptions>(
    builder.Configuration.GetSection(IbptCargaTributariaOptions.SectionName));
builder.Services.Configure<AsyncBatchPollingOptions>(
    builder.Configuration.GetSection(AsyncBatchPollingOptions.SectionName));
builder.Services.Configure<LocalAgentInstallerOptions>(
    builder.Configuration.GetSection(LocalAgentInstallerOptions.SectionName));
builder.Services.AddSingleton<ILocalAgentInstallerService, LocalAgentInstallerService>();
builder.Services.Configure<LocalCertificateProfileStorageOptions>(options =>
{
    options.DataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "certificates");
});

var nfeDatabaseConnection = builder.Configuration.GetConnectionString("NfeDatabase");
if (string.IsNullOrWhiteSpace(nfeDatabaseConnection))
{
    throw new InvalidOperationException(
        "Connection string 'NfeDatabase' is not configured. Set ConnectionStrings:NfeDatabase in appsettings.");
}

builder.Services.AddDbContext<NfeDbContext>(options =>
    options.UseNpgsql(nfeDatabaseConnection));

// Add services to the container (camelCase + string enums — same contract as LocalAgent MainApiNotaFiscalClient)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDevCors", policy =>
    {
        policy
            .WithOrigins("https://app.amktechsistemas.com.br")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Microled NFS-e Service API",
        Version = "v1",
        Description = "API for issuing NFS-e (Nota Fiscal de Serviços Eletrônica) for São Paulo City Hall",
        Contact = new OpenApiContact
        {
            Name = "Microled",
            Email = "support@microled.com"
        }
    });

    foreach (var xmlFile in GetSwaggerXmlFiles())
    {
        c.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
    }
});

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<SendRpsRequestDtoValidator>();

// Register Domain services
builder.Services.AddScoped<IRpsSignatureService, RpsSignatureService>();
builder.Services.AddScoped<INfeCancellationSignatureService, NfeCancellationSignatureService>();

// Register Infrastructure services
builder.Services.AddScoped<ICertificateDiscoveryService, WindowsCertificateDiscoveryService>();
builder.Services.AddScoped<ICompanyCertificateProfileRepository, JsonCompanyCertificateProfileRepository>();
builder.Services.AddScoped<INotaFiscalRepository, NotaFiscalRepository>();
builder.Services.AddScoped<INotaFiscalFlowPersistenceService, NotaFiscalFlowPersistenceService>();
builder.Services.AddScoped<IAsyncRpsProtocolPersistenceOrchestrator, AsyncRpsProtocolPersistenceOrchestrator>();
builder.Services.AddScoped<ICertificateProvider, CertificateProvider>();
builder.Services.AddScoped<IXmlSerializerService>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<XmlSerializerService>>();
    var options = serviceProvider.GetRequiredService<IOptions<NfeServiceOptions>>();
    var certificateProvider = serviceProvider.GetRequiredService<ICertificateProvider>();
    return new XmlSerializerService(logger, options, certificateProvider);
});
builder.Services.AddScoped<ISoapEnvelopeBuilder, SoapEnvelopeBuilder>();
builder.Services.AddScoped<IEnvioLoteRpsPedidoMapper, EnvioLoteRpsPedidoMapper>();
builder.Services.AddScoped<ConsultaNfeXsdValidator>();
builder.Services.AddScoped<CancelamentoNfeXsdValidator>();

// Register Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<NfeHealthCheck>("nfe", tags: new[] { "nfe", "certificate", "configuration" })
    .AddDbContextCheck<NfeDbContext>(
        name: "nfe_database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "database", "postgresql", "ready" });

// Register INfeGateway based on configuration
var useFakeGateway = builder.Configuration.GetValue<bool>("Features:UseFakeGateway", false);

if (useFakeGateway)
{
    builder.Services.AddScoped<INfeGateway, FakeNfeGateway>();
    builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Information);
    builder.Logging.AddFilter("Microled.Nfe.Service.Infra.Client.FakeNfeGateway", LogLevel.Information);
}
else
{
    
    // Register HTTP client factory for SOAP calls
    builder.Services.AddHttpClient(nameof(NfeSoapClient), (serviceProvider, client) =>
    {
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<NfeServiceOptions>>().Value;
        
        // Use BaseUrl if configured, otherwise fallback to ProductionEndpoint/TestEndpoint
        string endpoint;
        if (!string.IsNullOrEmpty(options.BaseUrl))
        {
            endpoint = options.BaseUrl;
        }
        else
        {
            endpoint = options.UseProduction ? options.ProductionEndpoint : options.TestEndpoint;
        }
        
        if (string.IsNullOrEmpty(endpoint))
        {
            throw new InvalidOperationException(
                "NfeService endpoint is not configured. Please set BaseUrl, ProductionEndpoint or TestEndpoint in appsettings.json");
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
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
        };
        handler.ClientCertificates.Add(certificate);

        return handler;
    });
    
    // Register NfeSoapClient with ICertificateProvider and IRpsSignatureService
    builder.Services.AddScoped<INfeGateway>(serviceProvider =>
    {
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(nameof(NfeSoapClient));
        var logger = serviceProvider.GetRequiredService<ILogger<NfeSoapClient>>();
        var options = serviceProvider.GetRequiredService<IOptions<NfeServiceOptions>>();
        var xmlSerializer = serviceProvider.GetRequiredService<IXmlSerializerService>();
        var soapEnvelopeBuilder = serviceProvider.GetRequiredService<ISoapEnvelopeBuilder>();
        var certificateProvider = serviceProvider.GetService<ICertificateProvider>();
        var rpsSignatureService = serviceProvider.GetService<IRpsSignatureService>();
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
}

// Register Application use cases
builder.Services.AddScoped<ISendRpsUseCase, SendRpsUseCase>();
builder.Services.AddScoped<IRpsBatchPreparationService, RpsBatchPreparationService>();
builder.Services.AddScoped<IConsultBatchStatusUseCase, ConsultBatchStatusUseCase>();
builder.Services.AddScoped<IConsultNfeUseCase, ConsultNfeUseCase>();
builder.Services.AddScoped<ICancelNfeUseCase, CancelNfeUseCase>();
builder.Services.AddScoped<IListCertificatesUseCase, ListCertificatesUseCase>();
builder.Services.AddScoped<ISelectCertificateUseCase, SelectCertificateUseCase>();
builder.Services.AddScoped<IUpsertCompanyCertificateProfileUseCase, UpsertCompanyCertificateProfileUseCase>();
builder.Services.AddScoped<IGetActiveCertificateProfileUseCase, GetActiveCertificateProfileUseCase>();
builder.Services.AddScoped<ICreateNotaFiscalUseCase, CreateNotaFiscalUseCase>();
builder.Services.AddScoped<IUpdateNotaFiscalAuthorizationUseCase, UpdateNotaFiscalAuthorizationUseCase>();
builder.Services.AddScoped<IUpdateNotaFiscalStatusUseCase, UpdateNotaFiscalStatusUseCase>();
builder.Services.AddScoped<IUpdateNotaFiscalPagamentoUseCase, UpdateNotaFiscalPagamentoUseCase>();
builder.Services.AddScoped<IAttachNotaFiscalPdfUseCase, AttachNotaFiscalPdfUseCase>();
builder.Services.AddScoped<IGetNotaFiscalByIdUseCase, GetNotaFiscalByIdUseCase>();
builder.Services.AddScoped<ISearchNotasFiscaisUseCase, SearchNotasFiscaisUseCase>();
builder.Services.AddScoped<IGetNotaFiscalXmlUseCase, GetNotaFiscalXmlUseCase>();
builder.Services.AddScoped<IGetNotaFiscalPdfUseCase, GetNotaFiscalPdfUseCase>();
builder.Services.AddScoped<IPersistRpsSendResultUseCase, PersistRpsSendResultUseCase>();
builder.Services.AddScoped<IPersistBatchStatusDataUseCase, PersistBatchStatusDataUseCase>();
builder.Services.AddScoped<IPersistConsultNfeResultUseCase, PersistConsultNfeResultUseCase>();
builder.Services.AddScoped<IPersistCancelNfeResultUseCase, PersistCancelNfeResultUseCase>();

// Add logging
builder.Services.AddLogging();
//builder.Services.Configure<ForwardedHeadersOptions>(options =>
//{
//    options.ForwardedHeaders =
//        ForwardedHeaders.XForwardedFor |
//        ForwardedHeaders.XForwardedProto;

//    options.KnownNetworks.Clear();
//    options.KnownProxies.Clear();
//});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NfeDbContext>();
    await db.Database.MigrateAsync();
}

app.UseForwardedHeaders();


// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Microled NFS-e Service API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

//app.UseHttpsRedirection();
app.UseCors("FrontendDevCors");

// Add global exception handler
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseAuthorization();

app.MapControllers();

var healthCheckOptions = new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteJsonResponse
};

app.MapHealthChecks("/health/nfe", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("nfe"),
    ResponseWriter = HealthCheckResponseWriter.WriteJsonResponse
});

app.MapHealthChecks("/health/database", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("database"),
    ResponseWriter = HealthCheckResponseWriter.WriteJsonResponse
});

app.MapHealthChecks("/health", healthCheckOptions);

app.Run();

static IEnumerable<string> GetSwaggerXmlFiles()
{
    var baseDirectory = AppContext.BaseDirectory;
    var assemblies = new[]
    {
        Assembly.GetExecutingAssembly().GetName().Name,
        typeof(SendRpsRequestDtoValidator).Assembly.GetName().Name
    };

    return assemblies
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .Select(name => Path.Combine(baseDirectory, $"{name}.xml"))
        .Where(File.Exists);
}
