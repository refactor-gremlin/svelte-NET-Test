using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using MySvelteApp.Server.Features.Auth.GetCurrentUser;
using MySvelteApp.Server.Features.Auth.LoginUser;
using MySvelteApp.Server.Features.Auth.RegisterUser;
using MySvelteApp.Server.Features.Pokemon.GetRandomPokemon;
using MySvelteApp.Server.Shared.Common.Interfaces;
using MySvelteApp.Server.Shared.Domain.Events;
using MySvelteApp.Server.Shared.Domain.Services;
using MySvelteApp.Server.Shared.Infrastructure.Authentication;
using MySvelteApp.Server.Shared.Infrastructure.Configuration;
using MySvelteApp.Server.Shared.Infrastructure.HealthChecks;
using MySvelteApp.Server.Shared.Infrastructure.Persistence;
using MySvelteApp.Server.Shared.Infrastructure.Persistence.Repositories;
using MySvelteApp.Server.Shared.Infrastructure.Security;
using MySvelteApp.Server.Shared.Presentation.Filters;
using FluentValidation;
using System.Text;

namespace MySvelteApp.Server.Shared.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFeatureHandlers(this IServiceCollection services)
    {
        // Auth features
        services.AddScoped<RegisterUserCommand>();
        services.AddScoped<LoginUserCommand>();
        services.AddScoped<GetCurrentUserQuery>();
        
        // Pokemon features
        services.AddHttpClient<GetRandomPokemonQuery>();
        
        return services;
    }
    
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
    
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IUserDomainService, UserDomainService>();
        return services;
    }
    
    public static IServiceCollection AddDomainEvents(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
        return services;
    }
    
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSettings<JwtSettings>();
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
                };
            });
        
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
        
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        
        return services;
    }
    
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("MySvelteAppDb"));
        
        return services;
    }
    
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
        });
        
        // Auto-register FluentValidation validators
        services.AddValidatorsFromAssemblyContaining<RegisterUserValidator>();
        
        return services;
    }
    
    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database")
            .AddCheck<ExternalApiHealthCheck>("external_api");
        
        return services;
    }
    
    /// <summary>
    /// Helper method to register a feature service with its implementation.
    /// </summary>
    /// <typeparam name="TService">The service interface</typeparam>
    /// <typeparam name="TImplementation">The service implementation</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="lifetime">Service lifetime (default: Scoped)</param>
    public static IServiceCollection AddFeatureService<TService, TImplementation>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TService : class
        where TImplementation : class, TService
    {
        services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), lifetime));
        return services;
    }
}

