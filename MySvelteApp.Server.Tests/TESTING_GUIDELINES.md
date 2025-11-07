# Backend Testing Guidelines

This document provides guidelines and best practices for testing the backend application.

## Overview

The testing architecture follows a layered approach that mirrors the application's architecture:
- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test interaction between components
- **End-to-End Tests**: Test complete user flows

## Test Organization

### Directory Structure

```
MySvelteApp.Server.Tests/
├── Application/           # Service layer tests
│   ├── Authentication/
│   └── Pokemon/
├── Infrastructure/       # Infrastructure layer tests
│   ├── Authentication/
│   ├── External/
│   ├── Persistence/
│   └── Security/
├── Presentation/         # Controller tests
│   └── Controllers/
├── Integration/           # End-to-end tests
├── TestFixtures/          # Shared test utilities
└── TestResults/           # Test results and coverage
```

### Test Base Classes

#### TestBase
- **Purpose**: Base class for all tests requiring database access
- **Features**: Provides isolated in-memory database for each test
- **Usage**: `public class MyTests : TestBase`

#### Generic Test Templates
- **RepositoryTestTemplate**: For testing repositories
- **ServiceTestTemplate**: For testing services
- **ControllerTestTemplate**: For testing controllers
- **IntegrationTestTemplate**: For integration tests with WebApplicationFactory
- **ExternalApiServiceTestTemplate**: For testing external API services

See `TestFixtures/TestTemplates.cs` for complete templates and examples.

### Test Data Factories

#### GenericTestDataFactory
- **Purpose**: Creates test data for any entity type including Users, Auth DTOs, and Pokemon DTOs
- **Usage**: 
  - `var user = GenericTestDataFactory.CreateUser();`
  - `var request = GenericTestDataFactory.CreateLoginRequest();`
  - `var pokemon = GenericTestDataFactory.CreateRandomPokemonDto();`
  - For new entities: `var entity = GenericTestDataFactory.CreateEntityWithProperties<YourEntity>(e => { e.Name = "test"; });`

## Naming Conventions

### Test Classes
- **Pattern**: `{FeatureName}Tests` (e.g., `UserRepositoryTests`)
- **Example**: `PokemonControllerTests`, `AuthServiceTests`

### Test Methods
- **Pattern**: `{MethodName}_Scenario_ExpectedResult`
- **Examples**:
  - `AddAsync_ValidEntity_ReturnsSuccess`
  - `GetByIdAsync_NonExistentId_ReturnsNull`
  - `Login_ValidCredentials_ReturnsToken`

### Test Files
- **Unit Tests**: `{ClassName}.cs`
- **Integration Tests**: `{FeatureName}IntegrationTests.cs`
- **Template Classes**: `{TemplateName}.cs`

## Testing Patterns

### Repository Testing

```csharp
public class UserRepositoryTests : RepositoryTestTemplate<IUserRepository, User, int>
{
    protected override IUserRepository CreateRepository()
    {
        return new UserRepository(DbContext);
    }

    // ... tests using the template
}
```

### Service Testing

```csharp
public class AuthServiceTests : ServiceTestTemplate<IAuthService>
{
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly Mock<IPasswordHasher> _mockPasswordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _mockJwtTokenGenerator = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();

    protected override IAuthService CreateService()
    {
        return new AuthService(
            _mockUserRepository.Object,
            _mockPasswordHasher.Object,
            _mockJwtTokenGenerator.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest();
        var token = "test-token";
        
        _mockUserRepository.Setup(x => x.UsernameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockPasswordHasher.Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns(("hash", "salt"));
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mockJwtTokenGenerator.Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns(token);

        // Act
        var result = await Service.RegisterAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Token.Should().Be(token);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

**Important**: Services that use repositories must also mock `IUnitOfWork` since repositories no longer call `SaveChangesAsync` directly.

### Controller Testing

```csharp
public class PokemonControllerTests : ControllerTestTemplate<PokemonController>
{
    private readonly Mock<IPokemonService> _mockService = new();

    protected override PokemonController CreateController()
    {
        return new PokemonController(_mockService.Object);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsPokemon()
    {
        // Arrange
        var expectedPokemon = GenericTestDataFactory.CreateRandomPokemonDto();
        _mockService.Setup(x => x.GetRandomPokemonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPokemon);

        // Act
        var result = await Controller.Get(CancellationToken.None);

        // Assert
        var response = ControllerAssertionUtilities.AssertOkResult<RandomPokemonDto>(result);
        response.Should().BeEquivalentTo(expectedPokemon);
        _mockService.Verify(x => x.GetRandomPokemonAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

## Testing with Unit of Work

Services that perform database operations now use `IUnitOfWork` instead of calling `SaveChangesAsync` directly on repositories.

### Mocking UnitOfWork

```csharp
private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();

// Setup
_mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(1); // Returns number of affected rows

// Verify
_mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
```

### Testing Transactions

```csharp
[Fact]
public async Task Service_MultipleOperations_UsesTransaction()
{
    // Arrange
    await _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);
    await _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

    // Act
    await Service.YourMethod();

    // Assert
    _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
}
```

## Testing Configuration Classes

Configuration classes use `IOptions<T>` pattern. When testing services that depend on configuration:

### Mocking IOptions<T>

```csharp
public class JwtTokenGeneratorTests
{
    private readonly Mock<IOptions<JwtSettings>> _mockJwtSettings = new();

    public JwtTokenGeneratorTests()
    {
        var jwtSettings = new JwtSettings
        {
            Key = "test-key-that-is-long-enough",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpirationHours = 24
        };
        
        _mockJwtSettings.Setup(x => x.Value).Returns(jwtSettings);
    }

    protected override IJwtTokenGenerator CreateService()
    {
        return new JwtTokenGenerator(_mockJwtSettings.Object);
    }
}
```

### Testing Configuration Validation

Configuration classes are validated at startup. To test validation:

```csharp
[Fact]
public void Configuration_MissingRequiredProperty_ThrowsValidationException()
{
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Jwt:Issuer", "test" }
            // Missing Jwt:Key
        })
        .Build();

    // Act & Assert
    services.Invoking(s => s.AddConfigurationSettings(configuration))
        .Should().Throw<OptionsValidationException>();
}
```

## Testing Validators

FluentValidation validators can be tested independently:

### Testing Validator Rules

```csharp
public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("invalid username!")]
    public void Validate_InvalidUsername_ReturnsValidationError(string username)
    {
        // Arrange
        var request = new RegisterRequest { Username = username, Email = "test@example.com", Password = "Password123" };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public void Validate_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new RegisterRequest 
        { 
            Username = "validuser", 
            Email = "test@example.com", 
            Password = "Password123" 
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
```

### Testing Validator Integration

Validators are automatically applied via `ValidationFilter`. In integration tests, invalid requests should return 400 BadRequest:

```csharp
[Fact]
public async Task Register_InvalidRequest_ReturnsBadRequest()
{
    // Arrange
    var invalidRequest = new RegisterRequest 
    { 
        Username = "ab", // Too short
        Email = "invalid-email",
        Password = "123" // Too short
    };

    // Act
    var response = await Client.PostAsJsonAsync("/Auth/register", invalidRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var errors = await HttpClientTestHelpers.ReadJsonAsync<ValidationErrorResponse>(response);
    errors.Should().NotBeNull();
}
```

## Testing Health Checks

Health checks implement `IHealthCheck` and can be tested directly:

### Testing Health Check Implementation

```csharp
public class DatabaseHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_DatabaseAvailable_ReturnsHealthy()
    {
        // Arrange
        var dbContext = CreateTestDbContext();
        var healthCheck = new DatabaseHealthCheck(dbContext);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), 
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("Database is available");
    }

    [Fact]
    public async Task CheckHealthAsync_DatabaseUnavailable_ReturnsUnhealthy()
    {
        // Arrange
        var dbContext = CreateUnavailableDbContext();
        var healthCheck = new DatabaseHealthCheck(dbContext);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), 
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
```

### Testing Health Check Endpoint

```csharp
[Fact]
public async Task HealthCheck_AllServicesHealthy_ReturnsOk()
{
    // Act
    var response = await Client.GetAsync("/health");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var healthReport = await HttpClientTestHelpers.ReadJsonAsync<HealthReport>(response);
    healthReport.Status.Should().Be("Healthy");
}
```

## Testing Exception Middleware

The exception handling middleware catches unhandled exceptions. Test it via integration tests:

### Testing Exception Handling

```csharp
[Fact]
public async Task Endpoint_ThrowsException_ReturnsInternalServerError()
{
    // Arrange
    _mockService.Setup(x => x.YourMethod())
        .ThrowsAsync(new InvalidOperationException("Test error"));

    // Act
    var response = await Client.GetAsync("/api/endpoint");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    var error = await HttpClientTestHelpers.ReadJsonAsync<AuthErrorResponse>(response);
    error.Message.Should().Contain("error occurred");
}
```

### Testing Exception Mapping

```csharp
[Theory]
[InlineData(typeof(ArgumentException), HttpStatusCode.BadRequest)]
[InlineData(typeof(UnauthorizedAccessException), HttpStatusCode.Unauthorized)]
[InlineData(typeof(InvalidOperationException), HttpStatusCode.BadRequest)]
public async Task Middleware_MapsExceptionToStatusCode_Correctly(Type exceptionType, HttpStatusCode expectedStatus)
{
    // Arrange
    var exception = (Exception)Activator.CreateInstance(exceptionType, "Test")!;
    _mockService.Setup(x => x.YourMethod())
        .ThrowsAsync(exception);

    // Act
    var response = await Client.GetAsync("/api/endpoint");

    // Assert
    response.StatusCode.Should().Be(expectedStatus);
}
```

### When to Mock
- External dependencies (HTTP clients, file system, databases for unit tests)
- Service interfaces
- Configuration values (`IOptions<T>`)
- UnitOfWork for transaction management
- Time/clock for deterministic tests
- External APIs and third-party services

### When NOT to Mock
- The class being tested
- Simple value objects or DTOs
- Database operations in integration tests
- External API calls in unit tests (should be mocked, not called directly)

### External API Testing Guidelines

#### Best Practices for External APIs
- **Never call external APIs in unit tests** - they create flaky tests and depend on external services
- **Always mock HTTP responses** for external API calls using HttpClientTestUtilities
- **Use integration tests sparingly** for external APIs only when absolutely necessary
- **Design services to be testable** by injecting HttpClient or using interfaces

#### Mocking External APIs
```csharp
// Use HttpClientTestUtilities to mock external API responses
var mockHandler = new Mock<HttpMessageHandler>();
mockHandler.SetupRequest(HttpMethod.Get, "https://api.example.com")
    .ReturnsAsync(HttpClientTestUtilities.CreateJsonResponse(expectedData));

var httpClient = new HttpClient(mockHandler.Object);
var service = new ExternalApiService(httpClient);
```

#### Integration Testing with External APIs
If you must test external API integration:
1. Use a dedicated test environment or sandbox
2. Handle network failures and timeouts gracefully
3. Use retry policies and circuit breakers
4. Mark tests as `[Fact(Skip = "Requires external API")]` for CI/CD pipelines
5. Consider using tools like WireMock or similar for API simulation

### Common Mocking Patterns

```csharp
// Mocking a service
var mockService = new Mock<IService>();
mockService.Setup(x => x.Method(It.IsAny<ArgumentType>())).ReturnsAsync(expectedResult);

// Mocking IUnitOfWork
var mockUnitOfWork = new Mock<IUnitOfWork>();
mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(1);

// Mocking IOptions<T> for configuration
var mockOptions = new Mock<IOptions<JwtSettings>>();
mockOptions.Setup(x => x.Value).Returns(new JwtSettings { Key = "test-key" });

// Verifying mock calls
mockService.Verify(x => x.Method(It.IsAny<ArgumentType>()), Times.Once);

// Mocking HTTP responses
var mockHandler = new Mock<HttpMessageHandler>();
mockHandler.SetupRequest(HttpMethod.Get, "https://api.example.com")
    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
    });
```

## Assertion Guidelines

### FluentAssertions Best Practices

```csharp
// Always use descriptive assertion messages
result.Should().NotBeNull("Result should not be null");

// Be specific about expectations
user.Email.Should().Be("expected@example.com", "Email should match exactly");

// Use collection assertions effectively
users.Should().HaveCount(expectedCount, $"Should have {expectedCount} users");
users.Should().Contain(user, "Users list should contain the user");
```

### Entity Comparison

```csharp
// Use BeEquivalentTo for object comparison
actualUser.Should().BeEquivalentTo(expectedUser, "Users should be equivalent");

// For simple value comparison
actualUser.Email.Should().Be(expectedUser.Email, "Email should match");
```

## Database Testing

### Test Isolation
- Each test gets a fresh in-memory database
- Tests run in parallel with separate databases
- Database is cleaned up after each test

### Database Operations
```csharp
// Using the TestBase methods (inherited from RepositoryTestTemplate or TestBase)
await AddEntityAsync(GenericTestDataFactory.CreateUser());

// Using generic methods
await AddEntityAsync<Pokemon>(GenericTestDataFactory.CreateEntityWithProperties<Pokemon>(p => { p.Name = "test"; }));
await ClearEntitiesAsync<User>();

// Verify entity exists
var user = await GetEntityAsync<User>(1);
user.Should().NotBeNull();
```

## Integration Testing

### WebApplicationFactory Usage
```csharp
public class IntegrationTests : IntegrationTestTemplate
{
    public IntegrationTests(WebApplicationFactory<Program> factory) 
        : base(factory) { }

    [Fact]
    public async Task CompleteFlow_WorksCorrectly()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateYourRequest();

        // Act
        var response = await Client.PostAsJsonAsync("/api/endpoint", request);

        // Assert
        AssertSuccessResponse(response);
        var data = await HttpClientTestHelpers.ReadJsonAsync<ResponseType>(response);
        data.Should().NotBeNull();
    }
}
```

### HTTP Testing

```csharp
// Setting authentication using template helper
SetBearerToken(token);

// Making requests
var response = await Client.GetAsync("/api/endpoint");

// Asserting responses using template helpers
AssertSuccessResponse(response);
// or
AssertErrorResponse(response, HttpStatusCode.BadRequest, "expected error message");

// Reading JSON responses using helper
var data = await HttpClientTestHelpers.ReadJsonAsync<ResponseType>(response);
data.Should().NotBeNull();
```

## Error Handling Tests

### Test Exception Scenarios
```csharp
[Fact]
public async Task Method_WithInvalidInput_ThrowsException()
{
    // Act & Assert
    await service.Invoking(async x => await x.Method(invalidInput))
        .Should().ThrowAsync<ExpectedException>();
}
```

### HTTP Error Handling
```csharp
[Fact]
public async Task Endpoint_WithServerError_ReturnsInternalServerError()
{
    // Arrange
    mockService.Setup(x => x.Method(It.IsAny<ArgumentType>()))
        .ThrowsAsync(new InvalidOperationException());

    // Act
    var result = await controller.Get();

    // Assert
    result.Should().BeOfType<ObjectResult>();
    var objectResult = result as ObjectResult;
    objectResult.StatusCode.Should().Be(500);
}
```

## Coverage Requirements

### Target Coverage
- **Line Coverage**: ≥ 70%
- **Branch Coverage**: ≥ 60%
- **Method Coverage**: ≥ 80%

### Coverage Tools
- **Collection**: xUnit test runner with Coverlet collector
- **Reports**: Generated HTML reports in `TestResults/CoverageReport/`

### Running with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport"
```

## Best Practices

### Test Organization
1. **AAA Pattern**: Arrange, Act, Assert
2. **Descriptive Names**: Method names should describe the scenario and expected result
3. **Single Responsibility**: Each test should test one specific scenario
4. **Independence**: Tests should not depend on other tests

### Test Data
1. **Predictable**: Use fixed test data, not random values
2. **Minimal**: Create only necessary test data
3. **Cleanup**: Clean up after tests when needed

### Mocking
1. **Behavior Verification**: Verify interactions, not just state
2. **Realistic Mocks**: Mock behavior should match real implementation
3. **Avoid Over-Mocking**: Mock only what's necessary for the test

### Assertions
1. **Specific Assertions**: Test exact values, not just existence
2. **Meaningful Messages**: Include context in assertion messages
3. **Comprehensive Coverage**: Assert all important aspects of the result

## Running Tests

### All Tests
```bash
dotnet test
```

### Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~ClassName"
```

### With Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Parallel Execution
```bash
dotnet test --parallel
```

## Continuous Integration

### Azure DevOps
```yaml
- task: RunTests
  displayName: 'Run Tests'
  inputs:
    testProjects: '**/*Tests.csproj'
    arguments: '--collect:"XPlat Code Coverage"'
```

### GitHub Actions
```yaml
- name: Run Tests
  run: dotnet test --collect:"XPlat Code Coverage"
```

## Debugging Tests

### Common Issues
1. **Test Isolation**: Tests sharing state between runs
2. **Mock Setup**: Mocks not configured correctly
3. **Async/Await**: Not using await properly in async tests
4. **Database State**: Database state persisting between tests

### Debugging Techniques
1. Use debugger breakpoints in test methods
2. Add console logging for test diagnostics
3. Run tests individually to isolate issues
4. Use the Visual Studio Test Explorer

## Examples

### Repository Test Example
```csharp
[Fact]
public async Task GetByIdAsync_ExistingUser_ReturnsUser()
{
    // Arrange
    var expectedUser = GenericTestDataFactory.CreateUser(id: 1, username: "testuser");
    await AddEntityAsync(expectedUser);
    var repository = Repository;

    // Act
    var result = await repository.GetByIdAsync(1);

    // Assert
    result.Should().NotBeNull();
    result!.Username.Should().Be("testuser");
}
```

### Service Test Example
```csharp
public class AuthServiceTests : ServiceTestTemplate<IAuthService>
{
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly Mock<IPasswordHasher> _mockPasswordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _mockJwtTokenGenerator = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();

    protected override IAuthService CreateService()
    {
        return new AuthService(
            _mockUserRepository.Object,
            _mockPasswordHasher.Object,
            _mockJwtTokenGenerator.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest();
        var token = "test-token";
        
        _mockUserRepository.Setup(x => x.UsernameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockPasswordHasher.Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns(("hash", "salt"));
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mockJwtTokenGenerator.Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns(token);

        // Act
        var result = await Service.RegisterAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Token.Should().Be(token);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Controller Test Example
```csharp
public class PokemonControllerTests : ControllerTestTemplate<PokemonController>
{
    private readonly Mock<IPokemonService> _mockService = new();

    protected override PokemonController CreateController()
    {
        return new PokemonController(_mockService.Object);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsPokemon()
    {
        // Arrange
        var expectedPokemon = GenericTestDataFactory.CreateRandomPokemonDto();
        _mockService.Setup(x => x.GetRandomPokemonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPokemon);

        // Act
        var result = await Controller.Get(CancellationToken.None);

        // Assert
        var response = ControllerAssertionUtilities.AssertOkResult<RandomPokemonDto>(result);
        response.Should().BeEquivalentTo(expectedPokemon);
        _mockService.Verify(x => x.GetRandomPokemonAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Integration Test Example
```csharp
public class AuthenticationIntegrationTests : IntegrationTestTemplate
{
    public AuthenticationIntegrationTests(WebApplicationFactory<Program> factory) 
        : base(factory) { }

    [Fact]
    public async Task CompleteAuthenticationFlow_WorksCorrectly()
    {
        // Arrange
        var registerRequest = GenericTestDataFactory.CreateRegisterRequest();

        // Act
        var registerResponse = await Client.PostAsJsonAsync("/Auth/register", registerRequest);
        AssertSuccessResponse(registerResponse);
        
        var authResult = await HttpClientTestHelpers.ReadJsonAsync<AuthSuccessResponse>(registerResponse);
        SetBearerToken(authResult!.Token);

        var meResponse = await Client.GetAsync("/Auth/me");
        AssertSuccessResponse(meResponse);

        // Assert
        var currentUser = await HttpClientTestHelpers.ReadJsonAsync<CurrentUserResponse>(meResponse);
        currentUser.Should().NotBeNull();
    }
}
```

This comprehensive testing architecture provides a solid foundation for testing any backend features while maintaining consistency and quality across the entire test suite.

## Adding New Features

### Quick Setup Process
1. **Check the FEATURE_SETUP_GUIDE.md** - Step-by-step guide for adding tests
2. **Use the Templates** - Copy from `TestFixtures/TestTemplates.cs` for quick setup
3. **Follow Naming Patterns** - Maintain consistency with existing tests
4. **Use GenericTestDataFactory** - Add your entity/DTO factory methods

### Templates Available
- `ServiceTestTemplate<TService>` - Service layer tests
- `ControllerTestTemplate<TController>` - Controller tests  
- `RepositoryTestTemplate<TRepository, TEntity, TId>` - Repository tests
- `IntegrationTestTemplate` - Integration tests
- `ExternalApiServiceTestTemplate` - External API tests

### Example: Adding a New Feature
```csharp
// 1. Add to GenericTestDataFactory
public static YourEntity CreateYourEntity(int id = 1, string name = "test")
{
    return new YourEntity { Id = id, Name = name };
}

// 2. Create service test using template
public class YourFeatureServiceTests : ServiceTestTemplate<IYourFeatureService>
{
    private readonly Mock<IYourRepository> _mockRepository = new();
    
    protected override IYourFeatureService CreateService() => 
        new YourFeatureService(_mockRepository.Object);
    
    [Fact]
    public async Task YourMethod_ValidInput_ReturnsExpected() 
    { 
        // Arrange
        var input = GenericTestDataFactory.CreateYourRequest();
        var expected = GenericTestDataFactory.CreateYourEntity();
        _mockRepository.Setup(x => x.GetAsync(1)).ReturnsAsync(expected);
        
        // Act
        var result = await Service.YourMethod(input);
        
        // Assert
        result.Should().BeEquivalentTo(expected);
    }
}

// 3. Create controller test using template
public class YourFeatureControllerTests : ControllerTestTemplate<YourFeatureController>
{
    private readonly Mock<IYourFeatureService> _mockService = new();
    
    protected override YourFeatureController CreateController() => 
        new YourFeatureController(_mockService.Object);
    
    [Fact]
    public async Task Get_ValidId_ReturnsOk()
    {
        // Arrange
        var expected = GenericTestDataFactory.CreateYourEntity();
        _mockService.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(expected);
        
        // Act
        var result = await Controller.Get(1);
        
        // Assert
        var response = ControllerAssertionUtilities.AssertOkResult<YourEntity>(result);
        response.Should().BeEquivalentTo(expected);
        _mockService.Verify(x => x.GetByIdAsync(1), Times.Once);
    }
}
```

### Benefits of This Architecture
- **Consistent Patterns**: All features follow the same testing approach
- **Quick Setup**: Templates reduce boilerplate code significantly
- **Easy Maintenance**: Changes to patterns affect all features uniformly
- **Extensible**: New patterns can be added and reused across features
