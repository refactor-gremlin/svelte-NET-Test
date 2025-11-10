using Microsoft.Extensions.Diagnostics.HealthChecks;
using MySvelteApp.Server.Features.Pokemon.GetRandomPokemon;

namespace MySvelteApp.Server.Shared.Infrastructure.HealthChecks;

public class ExternalApiHealthCheck : IHealthCheck
{
    private readonly GetRandomPokemonQuery _pokemonHandler;

    public ExternalApiHealthCheck(GetRandomPokemonQuery pokemonHandler)
    {
        _pokemonHandler = pokemonHandler;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _pokemonHandler.HandleAsync(cancellationToken);
            if (result.IsSuccess)
            {
                return HealthCheckResult.Healthy("External Pokemon API is available.");
            }
            return HealthCheckResult.Degraded("External Pokemon API returned an error.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("External Pokemon API is not responding.", ex);
        }
    }
}

