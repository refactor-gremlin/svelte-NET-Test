using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using FluentAssertions;
using MySvelteApp.Server.Features.Pokemon.GetRandomPokemon;
using MySvelteApp.Server.Shared.Common.Results;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.Features.Pokemon.GetRandomPokemon;

public class GetRandomPokemonQueryTests : TestBase
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly GetRandomPokemonQuery _handler;

    public GetRandomPokemonQueryTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _handler = new GetRandomPokemonQuery(_httpClient);
    }

    [Fact]
    public async Task HandleAsync_ValidResponse_ReturnsSuccessResult()
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
        var result = await _handler.HandleAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("pikachu");
        result.Value.Type.Should().Be("electric");
        result.Value.Image.Should().Be("https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/25.png");
    }

    [Fact]
    public async Task HandleAsync_HttpClientError_ReturnsFailureResult()
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

        // Act
        var result = await _handler.HandleAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ApiErrorType.BadRequest);
    }

    [Fact]
    public async Task HandleAsync_InvalidJsonResponse_ReturnsFailureResult()
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

        // Act
        var result = await _handler.HandleAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ApiErrorType.BadRequest);
    }

    [Fact]
    public async Task HandleAsync_ZeroPokemonCount_ReturnsFailureResult()
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

        // Act
        var result = await _handler.HandleAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ApiErrorType.BadRequest);
    }

    [Fact]
    public async Task HandleAsync_MultipleTypes_ReturnsCorrectTypeString()
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
        var result = await _handler.HandleAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Type.Should().Contain("grass");
        result.Value.Type.Should().Contain("poison");
    }

    [Fact]
    public async Task HandleAsync_WithCancellationToken_RespectsCancellation()
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
        await _handler.Invoking(async x => await x.HandleAsync(cancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }
}

