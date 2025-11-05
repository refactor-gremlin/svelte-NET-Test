using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using FluentAssertions;
using MySvelteApp.Server.Application.Pokemon;
using MySvelteApp.Server.Application.Pokemon.DTOs;
using MySvelteApp.Server.Infrastructure.External;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.Application.Pokemon;

public class PokeApiRandomPokemonServiceTests : TestBase
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly PokeApiRandomPokemonService _pokemonService;

    public PokeApiRandomPokemonServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _pokemonService = new PokeApiRandomPokemonService(_httpClient);
    }

    [Fact]
    public async Task GetRandomPokemonAsync_ValidResponse_ReturnsPokemonDto()
    {
        // Arrange
        var expectedPokemon = new
        {
            name = "pikachu",
            types = new[]
            {
                new { type = new { name = "electric" } }
            },
            sprites = new
            {
                front_default = "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/25.png"
            }
        };

        var pokemonResponse = HttpClientTestUtilities.CreateJsonResponse(expectedPokemon);
        var countResponse = HttpClientTestUtilities.CreateJsonResponse(new { count = 898 });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri != null && 
                    req.RequestUri.ToString().Contains("pokemon-species")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(countResponse);

        // Mock any Pokemon request to return the same response
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri != null && 
                    req.RequestUri.ToString().StartsWith("https://pokeapi.co/api/v2/pokemon/")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(pokemonResponse);

        // Act
        var result = await _pokemonService.GetRandomPokemonAsync();

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("pikachu");
        result.Type.Should().Be("electric");
        result.Image.Should().Be("https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/25.png");
    }

    [Fact]
    public async Task GetRandomPokemonAsync_HttpClientError_ThrowsException()
    {
        // Arrange
        var countResponse = HttpClientTestUtilities.CreateJsonResponse(new { count = 3 });
        var pokemonResponse = HttpClientTestUtilities.CreateTextResponse("Not Found", HttpStatusCode.NotFound);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri != null && 
                    req.RequestUri.ToString().Contains("pokemon-species")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(countResponse);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri != null && 
                    req.RequestUri.ToString().StartsWith("https://pokeapi.co/api/v2/pokemon/")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(pokemonResponse);

        // Act & Assert
        await _pokemonService.Invoking(async x => await x.GetRandomPokemonAsync())
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetRandomPokemonAsync_InvalidJsonResponse_ThrowsException()
    {
        // Arrange
        var countResponse = HttpClientTestUtilities.CreateJsonResponse(new { count = 3 });
        var pokemonResponse = HttpClientTestUtilities.CreateTextResponse("invalid json", HttpStatusCode.OK);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri != null && 
                    req.RequestUri.ToString().Contains("pokemon-species")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(countResponse);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri != null && 
                    req.RequestUri.ToString().StartsWith("https://pokeapi.co/api/v2/pokemon/")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(pokemonResponse);

        // Act & Assert
        await _pokemonService.Invoking(async x => await x.GetRandomPokemonAsync())
            .Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task GetRandomPokemonAsync_ZeroPokemonCount_HandlesGracefully()
    {
        // Arrange
        var countResponse = HttpClientTestUtilities.CreateJsonResponse(new { count = 0 });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri != null && 
                    req.RequestUri.ToString().Contains("pokemon-species")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(countResponse);

        // Act & Assert
        await _pokemonService.Invoking(async x => await x.GetRandomPokemonAsync())
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetRandomPokemonAsync_MultipleTypes_ReturnsCorrectTypeString()
    {
        // Arrange
        var expectedPokemon = new
        {
            name = "bulbasaur",
            types = new[]
            {
                new { type = new { name = "grass" } },
                new { type = new { name = "poison" } }
            },
            sprites = new
            {
                front_default = "bulbasaur.png"
            }
        };

        var pokemonResponse = HttpClientTestUtilities.CreateJsonResponse(expectedPokemon);
        var countResponse = HttpClientTestUtilities.CreateJsonResponse(new { count = 1 });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri != null && 
                    req.RequestUri.ToString().Contains("pokemon-species")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(countResponse);
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri != null && 
                    req.RequestUri.ToString().Contains("pokemon/1")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(pokemonResponse);

        // Act
        var result = await _pokemonService.GetRandomPokemonAsync();

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Contain("grass");
        result.Type.Should().Contain("poison");
    }

    [Fact]
    public async Task GetRandomPokemonAsync_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var cancellationToken = cts.Token;

        // Mock the HTTP handler to throw OperationCanceledException when cancellation is requested
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.Is<CancellationToken>(ct => ct.IsCancellationRequested))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await _pokemonService.Invoking(async x => await x.GetRandomPokemonAsync(cancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }
}
