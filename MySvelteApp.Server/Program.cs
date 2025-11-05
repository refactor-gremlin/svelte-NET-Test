using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MySvelteApp.Server.Application.Authentication;
using MySvelteApp.Server.Application.Common.Interfaces;
using MySvelteApp.Server.Application.Pokemon;

using MySvelteApp.Server.Infrastructure.Authentication;
using MySvelteApp.Server.Infrastructure.External;
using MySvelteApp.Server.Infrastructure.Persistence;
using MySvelteApp.Server.Infrastructure.Persistence.Repositories;
using MySvelteApp.Server.Infrastructure.Security;

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

        const string WebsiteClientOrigin = "website_client";

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(WebsiteClientOrigin, policy =>
            {
                policy
                    .WithOrigins("http://localhost:5173", "http://localhost:3000", "http://web:3000", "http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "your-issuer",
                    ValidAudience = builder.Configuration["Jwt:Audience"] ?? "your-audience",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "your-secret-key-here"))
                };
            });

        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        builder.Services.AddControllers();

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

        var promtailUrl = builder.Configuration["LOKI_PUSH_URL"] ?? "http://localhost:3101/loki/api/v1/push";
        var apiServiceName = builder.Configuration["OTEL_SERVICE_NAME"] ?? "mysvelteapp-api";
        var environmentName = builder.Environment.EnvironmentName ?? "Development";

        builder.Host.UseSerilog((_, _, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("service", apiServiceName)
                .Enrich.WithProperty("env", environmentName.ToLowerInvariant())
                .WriteTo.Console()
                .WriteTo.GrafanaLoki(promtailUrl);
        });

        var serviceName = apiServiceName;
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4318/v1/traces";
        var otlpProtocol = builder.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"] ?? "http/protobuf";

        builder.Services.AddOpenTelemetry().WithTracing(tracing =>
        {
            tracing
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                .AddAspNetCoreInstrumentation(options => { options.RecordException = true; })
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                    options.Protocol = string.Equals(otlpProtocol, "grpc", StringComparison.OrdinalIgnoreCase)
                        ? OtlpExportProtocol.Grpc
                        : OtlpExportProtocol.HttpProtobuf;
                });
        });

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("MySvelteAppDb"));

        builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddHttpClient<IRandomPokemonService, PokeApiRandomPokemonService>();


        var app = builder.Build();

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
        app.MapControllers();
        app.Run();
    }
}
