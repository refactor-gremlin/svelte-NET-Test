using Microsoft.OpenApi.Models;
using MySvelteApp.Server.Infrastructure.Configuration;
using MySvelteApp.Server.Infrastructure.DependencyInjection;
using MySvelteApp.Server.Presentation.Middleware;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.Grafana.Loki;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register configuration settings
        builder.Services.AddConfigurationSettings(builder.Configuration);

        // Configure CORS
        var corsSettings = builder.Configuration.GetCorsSettings();
        const string WebsiteClientOrigin = "website_client";
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(WebsiteClientOrigin, policy =>
            {
                var origins = corsSettings.AllowedOrigins.Length > 0 
                    ? corsSettings.AllowedOrigins 
                    : new[] { "http://localhost:5173", "http://localhost:3000", "http://web:3000" };
                
                policy.WithOrigins(origins);
                
                if (corsSettings.AllowAnyHeader)
                    policy.AllowAnyHeader();
                if (corsSettings.AllowAnyMethod)
                    policy.AllowAnyMethod();
                if (corsSettings.AllowCredentials)
                    policy.AllowCredentials();
            });
        });

        // Register application services
        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructureServices();
        builder.Services.AddAuthenticationServices(builder.Configuration);
        builder.Services.AddDatabaseServices();
        builder.Services.AddExternalServices();
        builder.Services.AddPresentationServices();
        builder.Services.AddHealthCheckServices();

        // Configure Swagger
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "MySvelteApp.Server", Version = "v1" });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter 'Bearer <token>'"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // Configure logging
        var loggingSettings = builder.Configuration.GetLoggingSettings();
        var environmentName = builder.Environment.EnvironmentName ?? "Development";

        builder.Host.UseSerilog((_, _, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("service", loggingSettings.OtelServiceName)
                .Enrich.WithProperty("env", environmentName.ToLowerInvariant())
                .WriteTo.Console()
                .WriteTo.GrafanaLoki(loggingSettings.LokiPushUrl);
        });

        // Configure OpenTelemetry
        builder.Services.AddOpenTelemetry().WithTracing(tracing =>
        {
            tracing
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(loggingSettings.OtelServiceName))
                .AddAspNetCoreInstrumentation(options => { options.RecordException = true; })
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(loggingSettings.OtelExporterOtlpEndpoint);
                    options.Protocol = string.Equals(loggingSettings.OtelExporterOtlpProtocol, "grpc", StringComparison.OrdinalIgnoreCase)
                        ? OtlpExportProtocol.Grpc
                        : OtlpExportProtocol.HttpProtobuf;
                });
        });

        var app = builder.Build();

        // Configure middleware pipeline
        app.UseExceptionHandling();
        app.UseCors(WebsiteClientOrigin);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseSerilogRequestLogging();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHealthChecks("/health");
        app.MapControllers();
        app.Run();
    }
}
