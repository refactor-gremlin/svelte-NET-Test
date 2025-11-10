using Microsoft.Extensions.Diagnostics.HealthChecks;
using MySvelteApp.Server.Shared.Infrastructure.Persistence;

namespace MySvelteApp.Server.Shared.Infrastructure.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;

    public DatabaseHealthCheck(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            if (canConnect)
            {
                return HealthCheckResult.Healthy("Database is available.");
            }

            return HealthCheckResult.Unhealthy("Database is not available.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check failed.", ex);
        }
    }
}

