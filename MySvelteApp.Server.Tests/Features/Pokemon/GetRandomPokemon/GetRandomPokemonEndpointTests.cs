using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using MySvelteApp.Server.Features.Pokemon.GetRandomPokemon;
using MySvelteApp.Server.Shared.Common.DTOs.Responses;
using MySvelteApp.Server.Shared.Common.Results;
using MySvelteApp.Server.Tests.TestFixtures;
using System.Threading.Tasks;
using System.Threading;

namespace MySvelteApp.Server.Tests.Features.Pokemon.GetRandomPokemon;

public class GetRandomPokemonEndpointTests : ControllerTestTemplate<GetRandomPokemonEndpoint>
{
    private readonly Mock<GetRandomPokemonQuery> _mockHandler = new();

    protected override GetRandomPokemonEndpoint CreateController()
    {
        return new GetRandomPokemonEndpoint(_mockHandler.Object);
    }

    [Fact]
    public async Task Handle_ValidHandlerResponse_ReturnsOkResultWithGetRandomPokemonResponse()
    {
        // Arrange
        var expectedPokemon = GenericTestDataFactory.CreateRandomPokemonDto(
            name: "pikachu",
            type: "electric",
            image: "https://example.com/pikachu.png"
        );

        _mockHandler.Setup(x => x.HandleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<GetRandomPokemonResponse>.Success(new GetRandomPokemonResponse
            {
                Name = expectedPokemon.Name,
                Type = expectedPokemon.Type,
                Image = expectedPokemon.Image
            }));

        // Act
        var result = await Controller.Handle(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<GetRandomPokemonResponse>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Name.Should().Be(expectedPokemon.Name);
        apiResponse.Data.Type.Should().Be(expectedPokemon.Type);
        apiResponse.Data.Image.Should().Be(expectedPokemon.Image);
    }

    [Fact]
    public async Task Handle_HandlerReturnsFailure_ReturnsBadRequest()
    {
        // Arrange
        _mockHandler.Setup(x => x.HandleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<GetRandomPokemonResponse>.Failure("Failed to retrieve Pokemon"));

        // Act
        var result = await Controller.Handle(CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        errorResponse.Message.Should().Contain("Failed to retrieve Pokemon");
    }

    [Fact]
    public async Task Handle_WithValidCancellationToken_PassesCancellationTokenToHandler()
    {
        // Arrange
        var expectedPokemon = GenericTestDataFactory.CreateRandomPokemonDto();
        var cancellationToken = new CancellationToken();

        _mockHandler.Setup(x => x.HandleAsync(cancellationToken))
            .ReturnsAsync(ApiResult<GetRandomPokemonResponse>.Success(new GetRandomPokemonResponse
            {
                Name = expectedPokemon.Name,
                Type = expectedPokemon.Type,
                Image = expectedPokemon.Image
            }));

        // Act
        var result = await Controller.Handle(cancellationToken);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockHandler.Verify(x => x.HandleAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancelledCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var cancellationToken = cts.Token;

        _mockHandler.Setup(x => x.HandleAsync(cancellationToken))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Controller.Invoking(async c => await c.Handle(cancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Handle_MultipleCalls_CallsHandlerEachTime()
    {
        // Arrange
        var pokemon1 = GenericTestDataFactory.CreateRandomPokemonDto(name: "pikachu");
        var pokemon2 = GenericTestDataFactory.CreateRandomPokemonDto(name: "charmander");

        _mockHandler.SetupSequence(x => x.HandleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<GetRandomPokemonResponse>.Success(new GetRandomPokemonResponse
            {
                Name = pokemon1.Name,
                Type = pokemon1.Type,
                Image = pokemon1.Image
            }))
            .ReturnsAsync(ApiResult<GetRandomPokemonResponse>.Success(new GetRandomPokemonResponse
            {
                Name = pokemon2.Name,
                Type = pokemon2.Type,
                Image = pokemon2.Image
            }));

        // Act
        var result1 = await Controller.Handle(CancellationToken.None);
        var result2 = await Controller.Handle(CancellationToken.None);

        // Assert
        result1.Should().BeOfType<OkObjectResult>();
        result2.Should().BeOfType<OkObjectResult>();

        var okResult1 = result1.Should().BeOfType<OkObjectResult>().Subject;
        var okResult2 = result2.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse1 = okResult1.Value.Should().BeOfType<ApiResponse<GetRandomPokemonResponse>>().Subject;
        var apiResponse2 = okResult2.Value.Should().BeOfType<ApiResponse<GetRandomPokemonResponse>>().Subject;

        apiResponse1.Data.Name.Should().Be("pikachu");
        apiResponse2.Data.Name.Should().Be("charmander");

        _mockHandler.Verify(x => x.HandleAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WithDifferentPokemonTypes_VerifiesTypeFormatting()
    {
        // Arrange
        var multiTypePokemon = GenericTestDataFactory.CreateRandomPokemonDto(
            name: "bulbasaur",
            type: "grass, poison"
        );

        _mockHandler.Setup(x => x.HandleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<GetRandomPokemonResponse>.Success(new GetRandomPokemonResponse
            {
                Name = multiTypePokemon.Name,
                Type = multiTypePokemon.Type,
                Image = multiTypePokemon.Image
            }));

        // Act
        var result = await Controller.Handle(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<GetRandomPokemonResponse>>().Subject;

        apiResponse.Data.Type.Should().Be("grass, poison");
        apiResponse.Data.Name.Should().Be("bulbasaur");
    }
}

