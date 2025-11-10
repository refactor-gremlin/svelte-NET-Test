# Feature Test Setup Guide

Quick guide to add tests for new features.

## Test Structure

```
Features/{Group}/{FeatureName}/
├── {FeatureName}HandlerTests.cs
└── {FeatureName}EndpointTests.cs
```

## 1. Add Test Data

Add to `GenericTestDataFactory.cs`:

```csharp
public static YourRequest CreateYourRequest(string name = "test")
{
    return new YourRequest { Name = name };
}
```

## 2. Handler Tests

```csharp
public class YourFeatureHandlerTests
{
    private readonly Mock<IYourRepository> _mockRepo = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly YourFeatureHandler _handler;

    public YourFeatureHandlerTests()
    {
        _handler = new YourFeatureHandler(_mockRepo.Object, _mockUow.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidInput_ReturnsSuccess()
    {
        var request = GenericTestDataFactory.CreateYourRequest();
        _mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YourEntity());
        _mockUow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        _mockUow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

## 3. Endpoint Tests

```csharp
public class YourFeatureEndpointTests : ControllerTestTemplate<YourFeatureEndpoint>
{
    private readonly Mock<YourFeatureHandler> _mockHandler = new();

    protected override YourFeatureEndpoint CreateController()
    {
        return new YourFeatureEndpoint(_mockHandler.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsOk()
    {
        var request = GenericTestDataFactory.CreateYourRequest();
        var response = new YourFeatureResponse { Id = 1 };
        _mockHandler.Setup(x => x.HandleAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<YourFeatureResponse>.Success(response));

        var result = await Controller.Handle(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _mockHandler.Verify(x => x.HandleAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

## 4. Repository Tests (if needed)

```csharp
public class YourRepositoryTests : TestBase
{
    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsEntity()
    {
        var entity = GenericTestDataFactory.CreateYourEntity();
        DbContext.YourEntities.Add(entity);
        await DbContext.SaveChangesAsync();

        var repo = new YourRepository(DbContext);
        var result = await repo.GetByIdAsync(entity.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
    }
}
```

## Common Mocks

| Dependency | Mock Setup |
|------------|------------|
| `IUnitOfWork` | `_mockUow.Setup(x => x.SaveChangesAsync(...)).ReturnsAsync(1);` |
| `IOptions<T>` | `_mockOptions.Setup(x => x.Value).Returns(settings);` |
| `HttpClient` | Use `Mock<HttpMessageHandler>` with `HttpClientTestUtilities` |

## Naming

- **Test Classes**: `{FeatureName}HandlerTests`, `{FeatureName}EndpointTests`
- **Test Methods**: `{MethodName}_Scenario_ExpectedResult`

## Examples

- `Features/Auth/RegisterUser/` - Handler and endpoint examples
- `Features/Pokemon/GetRandomPokemon/` - Handler and endpoint examples
- `Shared/Infrastructure/Persistence/UserRepositoryTests.cs` - Repository example
