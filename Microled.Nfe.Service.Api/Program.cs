using FluentValidation;
using FluentValidation.AspNetCore;
using Microled.Nfe.Service.Api.HealthChecks;
using Microled.Nfe.Service.Api.Middleware;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Application.UseCases;
using Microled.Nfe.Service.Application.Validators;
using Microled.Nfe.Service.Business.Services;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Infra.Client;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Interfaces;
using Microled.Nfe.Service.Infra.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure options
builder.Services.Configure<NfeServiceOptions>(
    builder.Configuration.GetSection(NfeServiceOptions.SectionName));

// Add services to the container
builder.Services.AddControllers();

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
});

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<SendRpsRequestDtoValidator>();

// Register Domain services
builder.Services.AddScoped<IRpsSignatureService, RpsSignatureService>();
builder.Services.AddScoped<INfeCancellationSignatureService, NfeCancellationSignatureService>();

// Register Infrastructure services
builder.Services.AddScoped<ICertificateProvider, CertificateProvider>();
builder.Services.AddScoped<IXmlSerializerService, XmlSerializerService>();

// Register Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<NfeHealthCheck>("nfe", tags: new[] { "nfe", "certificate", "configuration" });

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
    // Register HTTP client for SOAP calls
    builder.Services.AddHttpClient<INfeGateway, NfeSoapClient>((serviceProvider, client) =>
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
    });
}

// Register Application use cases
builder.Services.AddScoped<ISendRpsUseCase, SendRpsUseCase>();
builder.Services.AddScoped<IConsultNfeUseCase, ConsultNfeUseCase>();
builder.Services.AddScoped<ICancelNfeUseCase, CancelNfeUseCase>();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Microled NFS-e Service API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();

// Add global exception handler
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Map health check endpoint
app.MapHealthChecks("/health/nfe", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                data = e.Value.Data
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.Run();
