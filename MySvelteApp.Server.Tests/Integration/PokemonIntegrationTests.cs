using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using FluentAssertions;
using MySvelteApp.Server.Shared.Infrastructure.Persistence;
using MySvelteApp.Server.Features.Pokemon.GetRandomPokemon;
using MySvelteApp.Server.Shared.Common.DTOs.Responses;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.Integration;

/// <summary>
/// Pokemon integration tests using the standardized template.
/// Tests Pokemon API endpoints in isolation.
/// </summary>
public class PokemonIntegrationTests : IntegrationTestTemplate
{
    public PokemonIntegrationTests(WebApplicationFactory<Program> factory) 
        : base(factory) { }

    [Fact]
    public async Task GetRandomPokemon_ReturnsValidPokemonResponse()
    {
        // Act
        var response = await Client.GetAsync("/pokemon/random");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<GetRandomPokemonResponse>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Name.Should().NotBeNullOrEmpty();
        apiResponse.Data.Type.Should().NotBeNullOrEmpty();
        apiResponse.Data.Image.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetRandomPokemon_MultipleRequests_ReturnsValidResponses()
    {
        // Act
        var responses = await Task.WhenAll(
            Client.GetAsync("/pokemon/random"),
            Client.GetAsync("/pokemon/random")
        );

        // Assert
        responses.Should().AllSatisfy(response => response.EnsureSuccessStatusCode());

        var pokemons = await Task.WhenAll(
            responses.Select(r => r.Content.ReadFromJsonAsync<ApiResponse<GetRandomPokemonResponse>>())
        );

        pokemons.Should().AllSatisfy(apiResponse =>
        {
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Name.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetRandomPokemon_ReturnsCorrectContentType()
    {
        // Act
        var response = await Client.GetAsync("/pokemon/random");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }
}
