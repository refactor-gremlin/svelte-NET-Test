# Feature Test Setup Guide

Quick guide to add tests for new backend features. Follow these steps to ensure consistent test patterns.

## Quick Start Checklist

### 1. Create Test Structure
```
MySvelteApp.Server.Tests/
├── Application/
│   └── YourFeature/           # Create this folder
│       ├── YourFeatureServiceTests.cs
│       └── YourFeatureDtoTests.cs
├── Infrastructure/
│   └── YourFeature/           # Create this folder (if needed)
│       └── YourFeatureRepositoryTests.cs
├── Presentation/
│   └── Controllers/           # Add tests here
│       └── YourFeatureControllerTests.cs
└── Integration/
    └── YourFeatureIntegrationTests.cs
```

### 2. Add Test Data Factory Methods
Add methods to `GenericTestDataFactory.cs`:

```csharp
// Add your entity factory
public static YourEntity CreateYourEntity(
    int id = 1,
    string name = "test-name")
{
    return new YourEntity
    {
        Id = id,
        Name = name
    };
}

// Add your DTO factory  
public static YourRequest CreateYourRequest(
    string name = "test-name")
{
    return new YourRequest
    {
        Name = name
    };
}
```

### 3. Create Service Tests
Use the `ServiceTestTemplate<TService>` template:

```csharp
public class YourFeatureServiceTests : ServiceTestTemplate<IYourFeatureService>
{
    protected override IYourFeatureService CreateService()
    {
        return new YourFeatureService(
            // Inject your dependencies
            MockRepository.Object,
            MockLogger.Object
        );
    }

    private readonly Mock<IYourRepository> MockRepository = new();
    private readonly Mock<ILogger<YourFeatureService>> MockLogger = new();

    [Fact]
    public async Task YourMethod_ValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var input = GenericTestDataFactory.CreateYourRequest();
        var expected = GenericTestDataFactory.CreateYourEntity();
        
        MockRepository.Setup(x => x.GetByIdAsync(input.Id))
            .ReturnsAsync(expected);

        // Act
        var result = await Service.YourMethod(input);

        // Assert
        result.Should().Be(expected);
        MockRepository.Verify(x => x.GetByIdAsync(input.Id), Times.Once);
    }
}
```

### 4. Create Controller Tests
Use the `ControllerTestTemplate<TController>` template:

```csharp
public class YourFeatureControllerTests : ControllerTestTemplate<YourFeatureController>
{
    protected override YourFeatureController CreateController()
    {
        return new YourFeatureController(MockService.Object);
    }

    private readonly Mock<IYourFeatureService> MockService = new();

    [Fact]
    public async Task Get_ValidId_ReturnsOkResult()
    {
        // Arrange
        var expected = GenericTestDataFactory.CreateYourEntity();
        MockService.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(expected);

        // Act
        var result = await Controller.Get(1);

        // Assert
        result.Should().BeOfType<ActionResult<YourEntity>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var entity = okResult.Value.Should().BeOfType<YourEntity>().Subject;
        entity.Should().BeEquivalentTo(expected);
    }
}
```

### 5. Create Repository Tests (if needed)
Use the `RepositoryTestTemplate<TRepository, TEntity, TId>` template:

```csharp
public class YourFeatureRepositoryTests : RepositoryTestTemplate<IYourRepository, YourEntity, int>
{
    protected override IYourRepository CreateRepository()
    {
        return new YourRepository(DbContext);
    }

    [Fact]
    public async Task AddAsync_ValidEntity_ReturnsEntity()
    {
        // Arrange
        var entity = GenericTestDataFactory.CreateYourEntity();
        var repository = CreateRepository();

        // Act
        var result = await repository.AddAsync(entity);

        // Assert
        result.Should().Be(entity);
        DbContext.Set<YourEntity>().Should().Contain(entity);
    }
}
```

### 6. Create Integration Tests
Use the `IntegrationTestTemplate` template:

```csharp
public class YourFeatureIntegrationTests : IntegrationTestTemplate
{
    public YourFeatureIntegrationTests(WebApplicationFactory<Program> factory) 
        : base(factory) { }

    [Fact]
    public async Task CompleteFlow_WorksCorrectly()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateYourRequest();

        // Act
        var response = await Client.PostAsJsonAsync("/api/yourfeature", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<YourResponse>();
        result.Should().NotBeNull();
    }
}
```

## Naming Patterns

### Test Classes
- `{FeatureName}ServiceTests.cs`
- `{FeatureName}ControllerTests.cs` 
- `{FeatureName}RepositoryTests.cs`
- `{FeatureName}IntegrationTests.cs`

### Test Methods
- `{MethodName}_ValidInput_ReturnsExpectedResult`
- `{MethodName}_InvalidInput_ReturnsError`
- `{MethodName}_NotFound_ReturnsNull`
- `{MethodName}_Unauthorized_ReturnsUnauthorized`

## Common Patterns

### Async Methods
```csharp
[Fact]
public async Task YourAsyncMethod_ValidInput_ReturnsResult()
{
    // Arrange
    // Act
    var result = await Service.YourAsyncMethod(input);
    
    // Assert
    result.Should().NotBeNull();
}
```

### Exception Testing
```csharp
[Fact]
public async Task YourMethod_InvalidInput_ThrowsException()
{
    // Arrange
    var invalidInput = new YourRequest { /* invalid data */ };

    // Act & Assert
    await Service.Invoking(async x => await x.YourMethod(invalidInput))
        .Should().ThrowAsync<ExpectedException>();
}
```

### Mock Verification
```csharp
// Verify method was called
MockService.Verify(x => x.YourMethod(input), Times.Once);

// Verify method was never called
MockService.Verify(x => x.YourMethod(It.IsAny<YourType>()), Times.Never);
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific feature tests
dotnet test --filter "YourFeature"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Need Help?

Check existing tests in:
- `Application/Authentication/` - Service layer examples
- `Presentation/Controllers/` - Controller examples  
- `TestFixtures/TestTemplates.cs` - Template examples
- `Integration/AuthenticationIntegrationTests.cs` - Integration examples
