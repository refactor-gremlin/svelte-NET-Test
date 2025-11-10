using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using System.Net;
using Xunit;
using MySvelteApp.Server.Features.Auth.RegisterUser;
using MySvelteApp.Server.Shared.Infrastructure.Persistence;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.TestFixtures;

/// <summary>
/// Standardized test templates for quickly creating tests for new features.
/// Copy and adapt these templates for your new feature.
/// </summary>

/// <summary>
/// Base template for testing services.
/// Replace TService with your service interface.
/// 
/// Usage example:
/// public class MyServiceTests : ServiceTestTemplate<IMyService>
/// {
///     private readonly Mock<IMyRepository> _mockRepository = new();
///     private readonly Mock<ILogger<MyService>> _mockLogger = new();
///     
///     protected override IMyService CreateService()
///     {
///         return new MyService(_mockRepository.Object, _mockLogger.Object);
///     }
///     
///     [Fact]
///     public async Task MyMethod_ValidInput_ReturnsExpected()
///     {
///         // Arrange
///         var input = GenericTestDataFactory.CreateMyRequest();
///         var expected = GenericTestDataFactory.CreateMyDto();
///         _mockRepository.Setup(x => x.GetAsync(It.IsAny<int>()))
///             .ReturnsAsync(expected);
///         
///         // Act
///         var result = await Service.MyMethod(input);
///         
///         // Assert
///         result.Should().BeEquivalentTo(expected);
///         _mockRepository.Verify(x => x.GetAsync(It.IsAny<int>()), Times.Once);
///     }
/// }
/// </summary>
/// <typeparam name="TService">The service interface to test</typeparam>
public abstract class ServiceTestTemplate<TService> where TService : class
{
    /// <summary>
    /// Creates the service instance to test. Override this method to provide your service with mocked dependencies.
    /// </summary>
    protected abstract TService CreateService();

    /// <summary>
    /// The service instance being tested. Created once per test class.
    /// </summary>
    protected TService Service { get; private set; } = null!;

    protected ServiceTestTemplate()
    {
        Service = CreateService();
    }
}

/// <summary>
/// Base template for testing controllers.
/// Replace TController with your controller class.
/// 
/// Usage example:
/// public class MyControllerTests : ControllerTestTemplate<MyController>
/// {
///     private readonly Mock<IMyService> _mockService = new();
///     
///     protected override MyController CreateController()
///     {
///         return new MyController(_mockService.Object);
///     }
///     
///     [Fact]
///     public async Task Get_ValidId_ReturnsOk()
///     {
///         // Arrange
///         var expected = GenericTestDataFactory.CreateMyDto();
///         _mockService.Setup(x => x.GetByIdAsync(1))
///             .ReturnsAsync(expected);
///         
///         // Act
///         var result = await Controller.Get(1);
///         
///         // Assert
///         ControllerAssertionUtilities.AssertOkResult(result, expected);
///         _mockService.Verify(x => x.GetByIdAsync(1), Times.Once);
///     }
/// }
/// </summary>
/// <typeparam name="TController">The controller class to test</typeparam>
public abstract class ControllerTestTemplate<TController> where TController : class
{
    /// <summary>
    /// Creates the controller instance to test. Override this method to provide your controller with mocked dependencies.
    /// </summary>
    protected abstract TController CreateController();

    /// <summary>
    /// The controller instance being tested. Created once per test class.
    /// </summary>
    protected TController Controller { get; private set; } = null!;

    protected ControllerTestTemplate()
    {
        Controller = CreateController();
    }

    /// <summary>
    /// Sets up the controller's HttpContext with a user claim for testing authenticated endpoints.
    /// </summary>
    protected void SetupAuthenticatedUser(int userId)
    {
        if (Controller is Microsoft.AspNetCore.Mvc.ControllerBase controllerBase)
        {
            var user = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.NameIdentifier, 
                        userId.ToString())
                }));
            controllerBase.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = user
                }
            };
        }
    }
}

/// <summary>
/// Base template for testing repositories.
/// Replace TRepository with your repository interface and TEntity with your entity type.
/// 
/// Usage example:
/// public class MyRepositoryTests : RepositoryTestTemplate<IMyRepository, MyEntity, int>
/// {
///     protected override IMyRepository CreateRepository()
///     {
///         return new MyRepository(DbContext);
///     }
///     
///     [Fact]
///     public async Task GetByIdAsync_ExistingId_ReturnsEntity()
///     {
///         // Arrange
///         var expected = GenericTestDataFactory.CreateMyEntity(id: 1);
///         await AddEntityAsync(expected);
///         
///         // Act
///         var result = await Repository.GetByIdAsync(1);
///         
///         // Assert
///         result.Should().NotBeNull();
///         result!.Id.Should().Be(1);
///     }
/// }
/// </summary>
/// <typeparam name="TRepository">The repository interface to test</typeparam>
/// <typeparam name="TEntity">The entity type managed by the repository</typeparam>
/// <typeparam name="TId">The ID type of the entity</typeparam>
public abstract class RepositoryTestTemplate<TRepository, TEntity, TId> : TestBase 
    where TRepository : class
    where TEntity : class
{
    /// <summary>
    /// Creates the repository instance to test. Override this method to provide your repository with the test DbContext.
    /// </summary>
    protected abstract TRepository CreateRepository();

    /// <summary>
    /// The repository instance being tested. Created once per test class.
    /// </summary>
    protected TRepository Repository => CreateRepository();

    // Common repository test patterns - override these if your repository doesn't support these operations
    // or if you need different behavior

    /// <summary>
    /// Example test for GetByIdAsync. Override or skip if not applicable.
    /// </summary>
    [Fact(Skip = "Override this test with your specific implementation")]
    public virtual Task GetByIdAsync_ExistingId_ReturnsEntity()
    {
        // Override this test in your repository test class
        return Task.CompletedTask;
    }

    /// <summary>
    /// Example test for GetByIdAsync with non-existent ID. Override or skip if not applicable.
    /// </summary>
    [Fact(Skip = "Override this test with your specific implementation")]
    public virtual Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        // Override this test in your repository test class
        return Task.CompletedTask;
    }

    /// <summary>
    /// Example test for AddAsync. Override or skip if not applicable.
    /// </summary>
    [Fact(Skip = "Override this test with your specific implementation")]
    public virtual Task AddAsync_ValidEntity_ReturnsEntity()
    {
        // Override this test in your repository test class
        return Task.CompletedTask;
    }
}

/// <summary>
/// Template for testing external API services (like Pokemon service).
/// 
/// Usage example:
/// public class MyApiServiceTests : ExternalApiServiceTestTemplate
/// {
///     private Mock<HttpMessageHandler> _mockHandler = null!;
///     private HttpClient _httpClient = null!;
///     private MyApiService _service = null!;
///     
///     protected override async Task InitializeAsync()
///     {
///         await base.InitializeAsync();
///         _mockHandler = HttpClientTestUtilities.CreateMockMessageHandler();
///         _httpClient = HttpClientTestUtilities.CreateHttpClient(_mockHandler);
///         _service = new MyApiService(_httpClient);
///     }
///     
///     [Fact]
///     public async Task GetData_ValidResponse_ReturnsData()
///     {
///         // Arrange
///         var expectedData = new { name = "test" };
///         var response = HttpClientTestUtilities.CreateJsonResponse(expectedData);
///         _mockHandler.SetupRequest(HttpMethod.Get, "https://api.example.com/data", response);
///         
///         // Act
///         var result = await _service.GetDataAsync();
///         
///         // Assert
///         result.Should().NotBeNull();
///     }
/// }
/// </summary>
public abstract class ExternalApiServiceTestTemplate : TestBase
{
    // This template provides the TestBase infrastructure for database access if needed.
    // Override InitializeAsync if you need to set up HTTP client mocks.
}

/// <summary>
/// Template for integration tests with WebApplicationFactory.
/// Provides isolated in-memory database per test class instance.
/// 
/// Usage example:
/// public class MyFeatureIntegrationTests : IntegrationTestTemplate
/// {
///     public MyFeatureIntegrationTests(WebApplicationFactory<Program> factory) 
///         : base(factory) { }
///     
///     [Fact]
///     public async Task CompleteFlow_WorksCorrectly()
///     {
///         // Arrange
///         var request = GenericTestDataFactory.CreateMyRequest();
///         
///         // Act
///         var response = await Client.PostAsJsonAsync("/api/myfeature", request);
///         
///         // Assert
///         HttpClientTestHelpers.AssertSuccessResponse(response);
///         var result = await HttpClientTestHelpers.ReadJsonAsync<MyResponse>(response);
///         result.Should().NotBeNull();
///     }
/// }
/// </summary>
public abstract class IntegrationTestTemplate : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    /// <summary>
    /// The WebApplicationFactory instance.
    /// </summary>
    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;

    /// <summary>
    /// HTTP client for making requests to the test server.
    /// </summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <summary>
    /// Service scope for accessing scoped services.
    /// </summary>
    protected IServiceScope Scope { get; private set; } = null!;

    /// <summary>
    /// Database context for the test. Uses isolated in-memory database.
    /// </summary>
    protected AppDbContext DbContext { get; private set; } = null!;

    /// <summary>
    /// Initializes the integration test template with an isolated database.
    /// </summary>
    protected IntegrationTestTemplate(WebApplicationFactory<Program> factory)
    {
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add isolated in-memory database
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                });
            });
        });

        Scope = Factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DbContext.Database.EnsureCreated();

        Client = Factory.CreateClient();
    }

    /// <summary>
    /// Cleans up resources after tests.
    /// </summary>
    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
        Scope.Dispose();
    }

    /// <summary>
    /// Sets a Bearer token on the HTTP client for authenticated requests.
    /// </summary>
    protected void SetBearerToken(string token)
    {
        HttpClientTestHelpers.SetBearerToken(Client, token);
    }

    /// <summary>
    /// Asserts that an HTTP response is successful.
    /// </summary>
    protected void AssertSuccessResponse(HttpResponseMessage response, string because = "")
    {
        HttpClientTestHelpers.AssertSuccessResponse(response, because);
    }

    /// <summary>
    /// Asserts that an HTTP response has the expected error status.
    /// </summary>
    protected void AssertErrorResponse(HttpResponseMessage response, HttpStatusCode expectedStatus, string? expectedMessage = null, string because = "")
    {
        HttpClientTestHelpers.AssertErrorResponse(response, expectedStatus, expectedMessage, because);
    }
}
