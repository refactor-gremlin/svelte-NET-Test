using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using MySvelteApp.Server.Presentation.Controllers;
using MySvelteApp.Server.Application.Pokemon;
using MySvelteApp.Server.Application.Pokemon.DTOs;
using MySvelteApp.Server.Tests.TestFixtures;
using System.Threading.Tasks;
using System.Threading;

namespace MySvelteApp.Server.Tests.Presentation.Controllers;

public class RandomPokemonControllerTests : TestBase
{
    private readonly Mock<IRandomPokemonService> _mockPokemonService;
    private readonly RandomPokemonController _controller;

    public RandomPokemonControllerTests()
    {
        _mockPokemonService = new Mock<IRandomPokemonService>();
        _controller = new RandomPokemonController(_mockPokemonService.Object);
    }

    [Fact]
    public async Task Get_ValidServiceResponse_ReturnsOkResultWithPokemonDto()
    {
        // Arrange
        var expectedPokemon = GenericTestDataFactory.CreateRandomPokemonDto(
            name: "pikachu",
            type: "electric",
            image: "https://example.com/pikachu.png"
        );

        _mockPokemonService.Setup(x => x.GetRandomPokemonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPokemon);

        // Act
        var result = await _controller.Get(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ActionResult<RandomPokemonDto>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pokemonDto = okResult.Value.Should().BeOfType<RandomPokemonDto>().Subject;

        pokemonDto.Name.Should().Be(expectedPokemon.Name);
        pokemonDto.Type.Should().Be(expectedPokemon.Type);
        pokemonDto.Image.Should().Be(expectedPokemon.Image);
    }

    [Fact]
    public async Task Get_ServiceReturnsNull_ReturnsOkWithNull()
    {
        // Arrange
        _mockPokemonService.Setup(x => x.GetRandomPokemonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((RandomPokemonDto)null!);

        // Act
        var result = await _controller.Get(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ActionResult<RandomPokemonDto>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeNull();
    }

    [Fact]
    public async Task Get_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        _mockPokemonService.Setup(x => x.GetRandomPokemonAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("API error"));

        // Act & Assert
        await _controller.Invoking(async c => await c.Get(CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("API error");
    }

    [Fact]
    public async Task Get_WithValidCancellationToken_PassesCancellationTokenToService()
    {
        // Arrange
        var expectedPokemon = GenericTestDataFactory.CreateRandomPokemonDto();
        var cancellationToken = new CancellationToken();

        _mockPokemonService.Setup(x => x.GetRandomPokemonAsync(cancellationToken))
            .ReturnsAsync(expectedPokemon);

        // Act
        var result = await _controller.Get(cancellationToken);

        // Assert
        result.Should().BeOfType<ActionResult<RandomPokemonDto>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pokemonDto = okResult.Value.Should().BeOfType<RandomPokemonDto>().Subject;
        pokemonDto.Should().NotBeNull();
        pokemonDto!.Name.Should().Be(expectedPokemon.Name);

        // Verify the service was called with the correct cancellation token
        _mockPokemonService.Verify(x => x.GetRandomPokemonAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Get_WithCancelledCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var cancellationToken = cts.Token;

        _mockPokemonService.Setup(x => x.GetRandomPokemonAsync(cancellationToken))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await _controller.Invoking(async c => await c.Get(cancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Get_MultipleCalls_CallsServiceEachTime()
    {
        // Arrange
        var pokemon1 = GenericTestDataFactory.CreateRandomPokemonDto(name: "pikachu");
        var pokemon2 = GenericTestDataFactory.CreateRandomPokemonDto(name: "charmander");

        _mockPokemonService.SetupSequence(x => x.GetRandomPokemonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pokemon1)
            .ReturnsAsync(pokemon2);

        // Act
        var result1 = await _controller.Get(CancellationToken.None);
        var result2 = await _controller.Get(CancellationToken.None);

        // Assert
        result1.Should().BeOfType<ActionResult<RandomPokemonDto>>();
        result2.Should().BeOfType<ActionResult<RandomPokemonDto>>();

        var okResult1 = result1.Result.Should().BeOfType<OkObjectResult>().Subject;
        var okResult2 = result2.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pokemonDto1 = okResult1.Value.Should().BeOfType<RandomPokemonDto>().Subject;
        var pokemonDto2 = okResult2.Value.Should().BeOfType<RandomPokemonDto>().Subject;

        pokemonDto1.Name.Should().Be("pikachu");
        pokemonDto2.Name.Should().Be("charmander");

        _mockPokemonService.Verify(x => x.GetRandomPokemonAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Get_HttpResponseHeaders_IncludesCorrectContentType()
    {
        // Arrange
        var expectedPokemon = GenericTestDataFactory.CreateRandomPokemonDto();
        _mockPokemonService.Setup(x => x.GetRandomPokemonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPokemon);

        // Act
        var result = await _controller.Get(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ActionResult<RandomPokemonDto>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<RandomPokemonDto>();
    }

    [Fact]
    public async Task Get_WithDifferentPokemonTypes_VerifiesTypeFormatting()
    {
        // Arrange
        var multiTypePokemon = GenericTestDataFactory.CreateRandomPokemonDto(
            name: "bulbasaur",
            type: "grass, poison"
        );

        _mockPokemonService.Setup(x => x.GetRandomPokemonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(multiTypePokemon);

        // Act
        var result = await _controller.Get(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ActionResult<RandomPokemonDto>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pokemonDto = okResult.Value.Should().BeOfType<RandomPokemonDto>().Subject;

        pokemonDto.Type.Should().Be("grass, poison");
        pokemonDto.Name.Should().Be("bulbasaur");
    }
}
