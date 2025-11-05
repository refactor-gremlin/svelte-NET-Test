using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using FluentAssertions;
using MySvelteApp.Server.Infrastructure.Persistence;
using MySvelteApp.Server.Application.Pokemon.DTOs;
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
        var response = await Client.GetAsync("/RandomPokemon");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var pokemon = await response.Content.ReadFromJsonAsync<RandomPokemonDto>();
        pokemon.Should().NotBeNull();
        pokemon!.Name.Should().NotBeNullOrEmpty();
        pokemon.Type.Should().NotBeNullOrEmpty();
        pokemon.Image.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetRandomPokemon_MultipleRequests_ReturnsValidResponses()
    {
        // Act
        var responses = await Task.WhenAll(
            Client.GetAsync("/RandomPokemon"),
            Client.GetAsync("/RandomPokemon")
        );

        // Assert
        responses.Should().AllSatisfy(response => response.EnsureSuccessStatusCode());

        var pokemons = await Task.WhenAll(
            responses.Select(r => r.Content.ReadFromJsonAsync<RandomPokemonDto>())
        );

        pokemons.Should().AllSatisfy(pokemon =>
        {
            pokemon.Should().NotBeNull();
            pokemon!.Name.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetRandomPokemon_ReturnsCorrectContentType()
    {
        // Act
        var response = await Client.GetAsync("/RandomPokemon");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }
}
