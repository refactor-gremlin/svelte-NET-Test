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
public class PokemonServiceTests : ServiceTestTemplate<IPokemonService>
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new();

    protected override IPokemonService CreateService()
    {
        var httpClient = new HttpClient();
        return new PokemonService(httpClient);
    }

    [Fact]
    public async Task GetRandomPokemonAsync_ValidResponse_ReturnsPokemon()
    {
        // Arrange
        var expectedPokemon = GenericTestDataFactory.CreateRandomPokemonDto();
        // Setup mocks...

        // Act
        var result = await Service.GetRandomPokemonAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedPokemon);
    }
}
```

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

## Mocking Guidelines

### When to Mock
- External dependencies (HTTP clients, file system, databases for unit tests)
- Service interfaces
- Configuration values
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

    protected override IAuthService CreateService()
    {
        return new AuthService(_mockUserRepository.Object, /* other dependencies */);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest();
        var expectedUser = GenericTestDataFactory.CreateUser();
        _mockUserRepository.Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await Service.RegisterAsync(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
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
