using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MySvelteApp.Server.Infrastructure.Persistence;
using MySvelteApp.Server.Application.Authentication.DTOs;
using MySvelteApp.Server.Application.Pokemon.DTOs;
using MySvelteApp.Server.Presentation.Models.Auth;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.Integration;

/// <summary>
/// End-to-end integration tests using the standardized template.
/// Tests complete user flows across multiple features.
/// </summary>
public class EndToEndIntegrationTests : IntegrationTestTemplate
{
    public EndToEndIntegrationTests(WebApplicationFactory<Program> factory) 
        : base(factory) { }

    [Fact]
    public async Task PokemonFeature_WorksCorrectly()
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
    public async Task PokemonEndpoint_WorksCorrectly()
    {
        // Act
        var response = await Client.GetAsync("/RandomPokemon");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var pokemon = await response.Content.ReadFromJsonAsync<RandomPokemonDto>();
        pokemon.Should().NotBeNull();
        pokemon!.Name.Should().NotBeNullOrEmpty();
    }
}
