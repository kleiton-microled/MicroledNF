using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Application.UseCases;
using Microled.Nfe.Service.Business.Services;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Infra.Client;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Interfaces;
using Microled.Nfe.Service.Infra.Repositories;
using Microled.Nfe.Service.Infra.Services;
using Microled.Nfe.Service.Infra.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microled.Nfe.Service.Console;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables()
                      .AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Configure options
                services.Configure<NfeServiceOptions>(
                    context.Configuration.GetSection(NfeServiceOptions.SectionName));
                services.Configure<AccessDatabaseOptions>(
                    context.Configuration.GetSection(AccessDatabaseOptions.SectionName));
                services.Configure<WebServiceProbeOptions>(
                    context.Configuration.GetSection(WebServiceProbeOptions.SectionName));
                services.Configure<NfeValidationOptions>(
                    context.Configuration.GetSection(NfeValidationOptions.SectionName));

                // Register Domain services
                services.AddScoped<IRpsSignatureService, RpsSignatureService>();
                services.AddScoped<INfeCancellationSignatureService, NfeCancellationSignatureService>();
                services.AddScoped<ICertificateProvider, CertificateProvider>();

                // Register Infrastructure services
                services.AddScoped<IXmlSerializerService, XmlSerializerService>();
                services.AddScoped<ISoapEnvelopeBuilder, SoapEnvelopeBuilder>();
                services.AddScoped<IAccessRpsRepository, AccessRpsRepository>();
                services.AddScoped<IRpsXmlValidationExportService, RpsXmlValidationExportService>();

                // Register INfeGateway (real SOAP client, not fake for console)
                services.AddHttpClient<INfeGateway, NfeSoapClient>((serviceProvider, client) =>
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

                // Register Application use cases
                services.AddScoped<ISendRpsUseCase, SendRpsUseCase>();
                services.AddScoped<IConsultNfeUseCase, ConsultNfeUseCase>();
                services.AddScoped<ICancelNfeUseCase, CancelNfeUseCase>();

                // Register Console runner
                services.AddScoped<IRpsConsoleRunner, RpsConsoleRunner>();

                // Register Web Service Probe service (uses IHttpClientFactory internally)
                services.AddScoped<IWebServiceProbeService, WebServiceProbeService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

        var host = builder.Build();

        try
        {
            using var scope = host.Services.CreateScope();
            var probeOptions = scope.ServiceProvider.GetRequiredService<IOptions<WebServiceProbeOptions>>().Value;

            // Check if probe mode is enabled
            if (probeOptions.EnableProbe)
            {
                var probeService = scope.ServiceProvider.GetRequiredService<IWebServiceProbeService>();
                await probeService.RunAsync(CancellationToken.None);
                // Exit after probe completes
                return;
            }

            // Normal execution: run RPS console runner
            var runner = scope.ServiceProvider.GetRequiredService<IRpsConsoleRunner>();
            await runner.RunAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogCritical(ex, "Application terminated unexpectedly");
            Environment.ExitCode = 1;
        }
        finally
        {
            await host.StopAsync();
        }
    }
}
