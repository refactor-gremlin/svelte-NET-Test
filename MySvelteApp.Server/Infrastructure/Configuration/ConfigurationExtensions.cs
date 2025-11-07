using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MySvelteApp.Server.Infrastructure.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddConfigurationSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<CorsSettings>(configuration.GetSection(CorsSettings.SectionName));
        services.Configure<LoggingSettings>(configuration.GetSection(LoggingSettings.SectionName));
        
        // Validate required settings
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        return services;
    }
    
    public static JwtSettings GetJwtSettings(this IConfiguration configuration)
    {
        var settings = new JwtSettings();
        configuration.GetSection(JwtSettings.SectionName).Bind(settings);
        return settings;
    }
    
    public static CorsSettings GetCorsSettings(this IConfiguration configuration)
    {
        var settings = new CorsSettings();
        configuration.GetSection(CorsSettings.SectionName).Bind(settings);
        return settings;
    }
    
    public static LoggingSettings GetLoggingSettings(this IConfiguration configuration)
    {
        var settings = new LoggingSettings();
        configuration.GetSection(LoggingSettings.SectionName).Bind(settings);
        return settings;
    }
}

