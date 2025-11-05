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

### Test Data Factories

#### GenericTestDataFactory
- **Purpose**: Creates test data for any entity type
- **Usage**: `var user = GenericTestDataFactory.CreateUser();`

#### UserTestDataFactory (Legacy)
- **Purpose**: User-specific test data creation
- **Usage**: `var request = UserTestDataFactory.CreateLoginRequest();`

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
    protected override IPokemonService CreateService()
    {
        // Setup service with mocked dependencies
    }

    [Fact]
    public async Task GetRandomPokemonAsync_ValidResponse_ReturnsPokemon()
    {
        // Arrange
        // Act
        // Assert pattern
    }
}
```

### Controller Testing

```csharp
public class PokemonControllerTests : ControllerTestTemplate<PokemonController>
{
    protected override PokemonController CreateController()
    {
        // Setup controller with mocked dependencies
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsPokemon()
    {
        // Arrange
        var pokemon = new RandomPokemonDto { /* ... */ };
        _mockService.Setup(x => x.GetRandomPokemonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pokemon);

        // Act
        var result = await _controller.Get(CancellationToken.None);

        // Assert
        AssertActionResult<RandomPokemonDto>(result, 200);
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
// Using the TestBase methods
await AddEntityAsync(new User { /* properties */ });

// Using generic methods
await AddEntityAsync<Pokemon>(new Pokemon { /* properties */ });
await ClearEntitiesAsync<User>();
```

## Integration Testing

### WebApplicationFactory Usage
```csharp
public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CompleteFlow_WorksCorrectly()
    {
        // Test complete user flows
    }
}
```

### HTTP Testing

```csharp
// Setting authentication
_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

// Making requests
var response = await _client.GetAsync("/api/endpoint");
response.EnsureSuccessStatusCode();

// Reading JSON responses
var data = await response.Content.ReadFromJsonAsync<ResponseType>();
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
[Fact]
public async Task GetRandomPokemonAsync_ValidResponse_ReturnsPokemon()
{
    // Arrange
    var expectedPokemon = GenericTestDataFactory.CreateRandomPokemonDto();
    _mockService.Setup(x => x.GetRandomPokemonAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(expectedPokemon);

    // Act
    var result = await _service.GetRandomPokemonAsync();

    // Assert
    result.Should().Be(expectedPokemon);
}
```

### Controller Test Example
```csharp
[Fact]
public async Task Get_ValidRequest_ReturnsPokemon()
{
    // Arrange
    var expectedPokemon = new RandomPokemonDto { /* ... */ };
    _mockService.Setup(x => x.GetRandomPokemonAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(expectedPokemon);

    // Act
    var result = await _controller.Get(CancellationToken.None);

    // Assert
    AssertActionResult<RandomPokemonDto>(result, 200);
    var okResult = result as OkObjectResult;
    var pokemon = okResult.Value as RandomPokemonDto;
    pokemon.Should().BeEquivalentTo(expectedPokemon);
}
```

### Integration Test Example
```csharp
[Fact]
public async Task CompleteAuthenticationFlow_WorksCorrectly()
{
    // Arrange
    var client = _factory.CreateClient();
    var loginRequest = new { /* ... */ };

    // Act
    var loginResponse = await client.PostAsJsonAsync("/Auth/login", loginRequest);
    var token = loginResponse.Data.token;

    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    var meResponse = await client.GetAsync("/Auth/me");

    // Assert
    loginResponse.EnsureSuccessStatusCode();
    meResponse.EnsureSuccessStatusCode();
}
```

This comprehensive testing architecture provides a solid foundation for testing any backend features while maintaining consistency and quality across the entire test suite.
