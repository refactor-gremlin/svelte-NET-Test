# Testing Guidelines

Unit tests follow vertical slice architecture - mirror production structure in `Features/` and `Shared/`.

## Test Structure

```
Features/{Group}/{FeatureName}/
├── {FeatureName}HandlerTests.cs    # Business logic tests
└── {FeatureName}EndpointTests.cs    # API endpoint tests

Shared/Infrastructure/
├── Authentication/                  # Infrastructure tests
├── Persistence/
└── Security/
```

## Test Base Classes

| Class | Purpose | Usage |
|-------|---------|-------|
| `TestBase` | Database access | `public class MyTests : TestBase` |
| `ControllerTestTemplate<T>` | Endpoint tests | `public class MyEndpointTests : ControllerTestTemplate<MyEndpoint>` |

## Naming

- **Test Classes**: `{FeatureName}HandlerTests`, `{FeatureName}EndpointTests`
- **Test Methods**: `{MethodName}_Scenario_ExpectedResult`
  - `HandleAsync_ValidRequest_ReturnsSuccess`
  - `HandleAsync_InvalidInput_ReturnsBadRequest`

## Test Data

```csharp
var user = GenericTestDataFactory.CreateUser();
var request = GenericTestDataFactory.CreateRegisterRequest();
var pokemon = GenericTestDataFactory.CreateRandomPokemonDto();
```

## Handler Testing

```csharp
public class RegisterUserHandlerTests
{
    private readonly Mock<IUserRepository> _mockRepo = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly Mock<IPasswordHasher> _mockHasher = new();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _handler = new RegisterUserHandler(_mockRepo.Object, _mockUow.Object, _mockHasher.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest();
        _mockRepo.Setup(x => x.UsernameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }
}
```

## Endpoint Testing

```csharp
public class RegisterUserEndpointTests : ControllerTestTemplate<RegisterUserEndpoint>
{
    private readonly Mock<RegisterUserHandler> _mockHandler = new();

    protected override RegisterUserEndpoint CreateController()
    {
        return new RegisterUserEndpoint(_mockHandler.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest();
        var response = new RegisterUserResponse { Token = "test", UserId = 1 };
        _mockHandler.Setup(x => x.HandleAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<RegisterUserResponse>.Success(response));

        // Act
        var result = await Controller.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockHandler.Verify(x => x.HandleAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

## Mocking Patterns

### UnitOfWork

```csharp
_mockUow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(1);
_mockUow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
```

### IOptions<T>

```csharp
var settings = new JwtSettings { Key = "test-key", Issuer = "test" };
_mockOptions.Setup(x => x.Value).Returns(settings);
```

### HttpClient (External APIs)

```csharp
var mockHandler = new Mock<HttpMessageHandler>();
mockHandler.Protected()
    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
    .ReturnsAsync(HttpClientTestUtilities.CreateJsonResponse(expectedData));

var httpClient = new HttpClient(mockHandler.Object);
```

## Validator Testing

```csharp
public class RegisterUserValidatorTests
{
    private readonly RegisterUserValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void Validate_InvalidUsername_ReturnsError(string username)
    {
        var request = new RegisterUserRequest { Username = username, Email = "test@example.com" };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }
}
```

## Infrastructure Testing

### Repository Tests

```csharp
public class UserRepositoryTests : TestBase
{
    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsUser()
    {
        var user = GenericTestDataFactory.CreateUser();
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var repo = new UserRepository(DbContext);
        var result = await repo.GetByIdAsync(user.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }
}
```

### Health Check Tests

```csharp
[Fact]
public async Task CheckHealthAsync_DatabaseAvailable_ReturnsHealthy()
{
    var healthCheck = new DatabaseHealthCheck(DbContext);
    var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
    result.Status.Should().Be(HealthStatus.Healthy);
}
```

## Assertions

Use FluentAssertions:

```csharp
result.IsSuccess.Should().BeTrue();
result.Value.Should().NotBeNull();
result.Value!.Property.Should().Be(expected);
result.Should().BeOfType<OkObjectResult>();
_mock.Verify(x => x.Method(), Times.Once);
```

## When to Mock

| Mock | Don't Mock |
|------|-----------|
| External dependencies (HTTP, DB for unit tests) | Class being tested |
| Service interfaces | Simple DTOs/value objects |
| `IOptions<T>` | Database in integration tests |
| `IUnitOfWork` | |

## Test Organization

- **One test class per handler/endpoint**
- **Arrange-Act-Assert** pattern
- **Descriptive test names** (`Method_Scenario_ExpectedResult`)
- **Isolated tests** - each test is independent
- **Use factories** for test data

## Running Tests

```bash
# All tests
dotnet test

# Specific test class
dotnet test --filter FullyQualifiedName~RegisterUserHandlerTests

# With coverage
dotnet test /p:CollectCoverage=true
```

## Adding Tests for New Features

1. Create `Features/{Group}/{FeatureName}/` folder
2. Add `{FeatureName}HandlerTests.cs` - test business logic
3. Add `{FeatureName}EndpointTests.cs` - test API endpoint
4. Use `GenericTestDataFactory` for test data
5. Mock dependencies, verify behavior

**Example**: See `Features/Auth/RegisterUser/RegisterUserHandlerTests.cs` and `RegisterUserEndpointTests.cs`
