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
// For entities, use the generic method or add a specific factory:
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

// For DTOs, add specific factory methods:
public static YourRequest CreateYourRequest(
    string name = "test-name")
{
    return new YourRequest
    {
        Name = name
    };
}

// Or use the generic method for simple cases:
var entity = GenericTestDataFactory.CreateEntityWithProperties<YourEntity>(e => 
{
    e.Id = 1;
    e.Name = "test";
});
```

### 3. Create Service Tests
Use the `ServiceTestTemplate<TService>` template:

**Important**: If your service uses repositories, you must also mock `IUnitOfWork`:

```csharp
public class YourFeatureServiceTests : ServiceTestTemplate<IYourFeatureService>
{
    private readonly Mock<IYourRepository> _mockRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<ILogger<YourFeatureService>> _mockLogger = new();

    protected override IYourFeatureService CreateService()
    {
        return new YourFeatureService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task YourMethod_ValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var input = GenericTestDataFactory.CreateYourRequest();
        var expected = GenericTestDataFactory.CreateYourEntity();
        
        _mockRepository.Setup(x => x.GetByIdAsync(input.Id))
            .ReturnsAsync(expected);
        _mockRepository.Setup(x => x.AddAsync(It.IsAny<YourEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await Service.YourMethod(input);

        // Assert
        result.Should().BeEquivalentTo(expected);
        _mockRepository.Verify(x => x.GetByIdAsync(input.Id), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### 4. Create Controller Tests
Use the `ControllerTestTemplate<TController>` template:

```csharp
public class YourFeatureControllerTests : ControllerTestTemplate<YourFeatureController>
{
    private readonly Mock<IYourFeatureService> _mockService = new();

    protected override YourFeatureController CreateController()
    {
        return new YourFeatureController(_mockService.Object);
    }

    [Fact]
    public async Task Get_ValidId_ReturnsOkResult()
    {
        // Arrange
        var expected = GenericTestDataFactory.CreateYourEntity();
        _mockService.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(expected);

        // Act
        var result = await Controller.Get(1);

        // Assert
        var response = ControllerAssertionUtilities.AssertOkResult<YourEntity>(result);
        response.Should().BeEquivalentTo(expected);
        _mockService.Verify(x => x.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task Get_Unauthorized_ReturnsUnauthorized()
    {
        // Arrange
        SetupAuthenticatedUser(userId: 1); // For authenticated endpoints
        _mockService.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync((YourEntity?)null);

        // Act
        var result = await Controller.Get(1);

        // Assert
        ControllerAssertionUtilities.AssertNotFoundResult(result);
    }
}
```

### 5. Create Repository Tests (if needed)
Use the `RepositoryTestTemplate<TRepository, TEntity, TId>` template:

**Note**: Repositories no longer call `SaveChangesAsync` directly. Use `IUnitOfWork` in services instead.

```csharp
public class YourFeatureRepositoryTests : RepositoryTestTemplate<IYourRepository, YourEntity, int>
{
    protected override IYourRepository CreateRepository()
    {
        return new YourRepository(DbContext);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsEntity()
    {
        // Arrange
        var expected = GenericTestDataFactory.CreateYourEntity(id: 1);
        await AddEntityAsync(expected);

        // Act
        var result = await Repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task AddAsync_ValidEntity_AddsToContext()
    {
        // Arrange
        var entity = GenericTestDataFactory.CreateYourEntity();

        // Act
        await Repository.AddAsync(entity);

        // Assert
        // Note: Entity is added to context but not saved
        // Saving is handled by UnitOfWork in the service layer
        var saved = await GetEntityAsync<YourEntity>(entity.Id);
        // Entity won't exist until UnitOfWork.SaveChangesAsync is called
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
        AssertSuccessResponse(response);
        var result = await HttpClientTestHelpers.ReadJsonAsync<YourResponse>(response);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_WithAuthentication_ReturnsData()
    {
        // Arrange
        var registerRequest = GenericTestDataFactory.CreateRegisterRequest();
        var registerResponse = await Client.PostAsJsonAsync("/Auth/register", registerRequest);
        var authResult = await HttpClientTestHelpers.ReadJsonAsync<AuthSuccessResponse>(registerResponse);
        
        SetBearerToken(authResult!.Token);

        // Act
        var response = await Client.GetAsync("/api/yourfeature");

        // Assert
        AssertSuccessResponse(response);
    }
}
```

## Adding Configuration Classes

If your feature needs configuration settings:

### 1. Create Configuration Class

```csharp
// In Infrastructure/Configuration/YourFeatureSettings.cs
public class YourFeatureSettings
{
    public const string SectionName = "YourFeature";
    
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
```

### 2. Register Configuration

Add to `Infrastructure/Configuration/ConfigurationExtensions.cs`:

```csharp
services.Configure<YourFeatureSettings>(configuration.GetSection(YourFeatureSettings.SectionName));
```

### 3. Add to appsettings.json

```json
{
  "YourFeature": {
    "ApiKey": "your-api-key",
    "BaseUrl": "https://api.example.com",
    "TimeoutSeconds": 30
  }
}
```

### 4. Test Configuration

```csharp
public class YourServiceTests
{
    private readonly Mock<IOptions<YourFeatureSettings>> _mockSettings = new();

    public YourServiceTests()
    {
        var settings = new YourFeatureSettings
        {
            ApiKey = "test-key",
            BaseUrl = "https://test.example.com",
            TimeoutSeconds = 30
        };
        _mockSettings.Setup(x => x.Value).Returns(settings);
    }
}
```

## Adding Validators

If your feature needs input validation:

### 1. Create Validator

```csharp
// In Application/YourFeature/Validators/YourRequestValidator.cs
public class YourRequestValidator : AbstractValidator<YourRequest>
{
    public YourRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(3).WithMessage("Name must be at least 3 characters long.");
        
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Please enter a valid email address.");
    }
}
```

### 2. Test Validator

```csharp
public class YourRequestValidatorTests
{
    private readonly YourRequestValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void Validate_InvalidName_ReturnsValidationError(string name)
    {
        // Arrange
        var request = new YourRequest { Name = name };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
}
```

Validators are automatically discovered and registered. Validation runs automatically via `ValidationFilter`.

## Registering Services via Extensions

Services are registered using extension methods in `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`:

### Application Services

```csharp
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IYourFeatureService, YourFeatureService>(); // Add here
    return services;
}
```

### Infrastructure Services

```csharp
public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
{
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IYourRepository, YourRepository>(); // Add here
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    return services;
}
```

## Adding Health Checks

If your feature needs health monitoring:

### 1. Create Health Check

```csharp
// In Infrastructure/HealthChecks/YourFeatureHealthCheck.cs
public class YourFeatureHealthCheck : IHealthCheck
{
    private readonly IYourService _yourService;

    public YourFeatureHealthCheck(IYourService yourService)
    {
        _yourService = yourService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check your service
            await _yourService.CheckHealthAsync(cancellationToken);
            return HealthCheckResult.Healthy("Your feature is available.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Your feature is not available.", ex);
        }
    }
}
```

### 2. Register Health Check

Add to `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`:

```csharp
public static IServiceCollection AddHealthCheckServices(this IServiceCollection services)
{
    services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database")
        .AddCheck<YourFeatureHealthCheck>("your_feature"); // Add here
    
    return services;
}
```

### 3. Test Health Check

```csharp
public class YourFeatureHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ServiceAvailable_ReturnsHealthy()
    {
        // Arrange
        var mockService = new Mock<IYourService>();
        mockService.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var healthCheck = new YourFeatureHealthCheck(mockService.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), 
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
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
- `Application/Authentication/` - Service layer examples with UnitOfWork
- `Infrastructure/Authentication/JwtTokenGeneratorTests.cs` - Configuration testing example
- `Presentation/Controllers/` - Controller examples  
- `TestFixtures/TestTemplates.cs` - Template examples
- `Integration/AuthenticationIntegrationTests.cs` - Integration examples

## Quick Reference

### Required Mocks for Services with Database Operations
- `Mock<IYourRepository>` - Repository interface
- `Mock<IUnitOfWork>` - Unit of Work (required!)
- Other service dependencies

### Required Mocks for Services with Configuration
- `Mock<IOptions<YourSettings>>` - Configuration options
- Other service dependencies

### Service Registration Locations
- Application services → `AddApplicationServices()`
- Infrastructure services → `AddInfrastructureServices()`
- Authentication → `AddAuthenticationServices()`
- Database → `AddDatabaseServices()`
- External APIs → `AddExternalServices()`
- Health checks → `AddHealthCheckServices()`
