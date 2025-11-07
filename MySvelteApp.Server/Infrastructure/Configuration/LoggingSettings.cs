namespace MySvelteApp.Server.Infrastructure.Configuration;

public class LoggingSettings
{
    public const string SectionName = "Logging";
    
    public string LokiPushUrl { get; set; } = "http://localhost:3101/loki/api/v1/push";
    public string OtelServiceName { get; set; } = "mysvelteapp-api";
    public string OtelExporterOtlpEndpoint { get; set; } = "http://localhost:4318/v1/traces";
    public string OtelExporterOtlpProtocol { get; set; } = "http/protobuf";
}

