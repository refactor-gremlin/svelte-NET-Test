namespace MySvelteApp.Server.Infrastructure.Configuration;

public class CorsSettings
{
    public const string SectionName = "Cors";
    
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public bool AllowAnyHeader { get; set; } = true;
    public bool AllowAnyMethod { get; set; } = true;
    public bool AllowCredentials { get; set; } = true;
}

