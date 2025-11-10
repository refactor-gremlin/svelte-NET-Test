using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace MySvelteApp.Server.Shared.Infrastructure.Configuration;

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
    
    public static T GetSettings<T>(this IConfiguration configuration) where T : class, new()
    {
        var settings = new T();
        var sectionName = GetSectionName<T>();
        configuration.GetSection(sectionName).Bind(settings);
        return settings;
    }
    
    private static string GetSectionName<T>()
    {
        var type = typeof(T);
        var sectionNameField = type.GetField("SectionName", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        
        if (sectionNameField != null && sectionNameField.FieldType == typeof(string))
        {
            return (string)sectionNameField.GetValue(null)!;
        }
        
        // Fallback to type name if SectionName constant not found
        return type.Name.Replace("Settings", "");
    }
    
    // Keep existing methods for backward compatibility, but mark as obsolete
    [Obsolete("Use GetSettings<JwtSettings>() instead")]
    public static JwtSettings GetJwtSettings(this IConfiguration configuration)
    {
        return configuration.GetSettings<JwtSettings>();
    }
    
    [Obsolete("Use GetSettings<CorsSettings>() instead")]
    public static CorsSettings GetCorsSettings(this IConfiguration configuration)
    {
        return configuration.GetSettings<CorsSettings>();
    }
    
    [Obsolete("Use GetSettings<LoggingSettings>() instead")]
    public static LoggingSettings GetLoggingSettings(this IConfiguration configuration)
    {
        return configuration.GetSettings<LoggingSettings>();
    }
}

