using Microsoft.Extensions.Diagnostics.HealthChecks;
using MySvelteApp.Server.Application.Pokemon;

namespace MySvelteApp.Server.Infrastructure.HealthChecks;

public class ExternalApiHealthCheck : IHealthCheck
{
    private readonly IRandomPokemonService _pokemonService;

    public ExternalApiHealthCheck(IRandomPokemonService pokemonService)
    {
        _pokemonService = pokemonService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _pokemonService.GetRandomPokemonAsync(cancellationToken);
            return HealthCheckResult.Healthy("External Pokemon API is available.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("External Pokemon API is not responding.", ex);
        }
    }
}

