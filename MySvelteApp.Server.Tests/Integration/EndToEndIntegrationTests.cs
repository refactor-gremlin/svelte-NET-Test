using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MySvelteApp.Server.Shared.Infrastructure.Persistence;
using MySvelteApp.Server.Features.Pokemon.GetRandomPokemon;
using MySvelteApp.Server.Shared.Common.DTOs.Responses;
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
    public async Task PokemonEndpoint_WorksCorrectly()
    {
        // Act
        var response = await Client.GetAsync("/pokemon/random");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<GetRandomPokemonResponse>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Name.Should().NotBeNullOrEmpty();
    }
}
