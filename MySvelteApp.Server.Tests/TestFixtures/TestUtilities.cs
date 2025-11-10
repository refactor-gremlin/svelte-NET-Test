using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using FluentAssertions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using MySvelteApp.Server.Shared.Infrastructure.Persistence;

namespace MySvelteApp.Server.Tests.TestFixtures;

/// <summary>
/// Generic utilities for testing HTTP client interactions
/// </summary>
public static class HttpClientTestUtilities
{
    public static Mock<HttpMessageHandler> CreateMockMessageHandler()
    {
        return new Mock<HttpMessageHandler>();
    }

    public static HttpClient CreateHttpClient(Mock<HttpMessageHandler> messageHandler)
    {
        return new HttpClient(messageHandler.Object);
    }

    public static HttpResponseMessage CreateJsonResponse(object content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(content), System.Text.Encoding.UTF8, "application/json")
        };
    }

    public static HttpResponseMessage CreateTextResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, "text/plain")
        };
    }

    public static HttpResponseMessage CreateEmptyResponse(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode);
    }

    public static void SetupSequence(this Mock<HttpMessageHandler> handler, IEnumerable<(string url, HttpResponseMessage response)> sequence)
    {
        var responses = sequence.ToList();
        if (responses.Any())
        {
            foreach (var response in responses)
            {
                handler.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(response.response);
            }
        }
    }

    public static void SetupRequest(this Mock<HttpMessageHandler> handler, HttpMethod method, string url, HttpResponseMessage response)
    {
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == method && (req.RequestUri != null && req.RequestUri.ToString() == url)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
}

/// <summary>
/// Generic utilities for database testing
/// </summary>
public static class DatabaseTestUtilities
{
    public static async Task<T> AddEntityAsync<T>(AppDbContext context, T entity) where T : class
    {
        context.Set<T>().Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public static async Task<List<T>> AddEntitiesAsync<T>(AppDbContext context, params T[] entities) where T : class
    {
        context.Set<T>().AddRange(entities);
        await context.SaveChangesAsync();
        return entities.ToList();
    }

    public static async Task<T?> GetEntityAsync<T>(AppDbContext context, object id) where T : class
    {
        return await context.Set<T>().FindAsync(id);
    }

    public static async Task<List<T>> GetAllEntitiesAsync<T>(AppDbContext context) where T : class
    {
        return await context.Set<T>().ToListAsync();
    }

    public static async Task<int> CountEntitiesAsync<T>(AppDbContext context) where T : class
    {
        return await context.Set<T>().CountAsync();
    }

    public static async Task ClearEntitiesAsync<T>(AppDbContext context) where T : class
    {
        context.Set<T>().RemoveRange(context.Set<T>());
        await context.SaveChangesAsync();
    }

    public static async Task<T> UpdateEntityAsync<T>(AppDbContext context, T entity) where T : class
    {
        context.Set<T>().Update(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public static async Task<int> DeleteEntitiesAsync<T>(AppDbContext context, System.Linq.Expressions.Expression<Func<T, bool>> predicate) where T : class
    {
        var entities = context.Set<T>().Where(predicate).ToList();
        context.Set<T>().RemoveRange(entities);
        return await context.SaveChangesAsync();
    }
}

/// <summary>
/// Generic assertion helpers for testing
/// </summary>
public static class AssertionUtilities
{
    public static void ShouldBeEquivalentTo<T>(T actual, T expected, string because = "")
    {
        actual.Should().BeEquivalentTo(expected, because);
    }

    public static void ShouldHaveProperty<T>(T obj, string propertyName, object expectedValue, string because = "")
    {
        var property = typeof(T).GetProperty(propertyName);
        var value = property?.GetValue(obj);
        value.Should().Be(expectedValue, because);
    }

    public static void ShouldNotBeNull<T>(T? obj, string because = "")
    {
        obj.Should().NotBeNull(because);
    }

    public static void ShouldBeNull<T>(T? obj, string because = "")
    {
        obj.Should().BeNull(because);
    }

    public static void ShouldBeEmpty<T>(IEnumerable<T> collection, string because = "")
    {
        collection.Should().BeEmpty(because);
    }

    public static void ShouldContain<T>(IEnumerable<T> collection, T item, string because = "")
    {
        collection.Should().Contain(item, because);
    }

    public static void ShouldHaveCount<T>(IEnumerable<T> collection, int expectedCount, string because = "")
    {
        collection.Should().HaveCount(expectedCount, because);
    }

    public static void ShouldBeOfType<T>(object obj, string because = "")
    {
        obj.Should().BeOfType<T>(because);
    }

    public static void ShouldBeSuccessful(this System.Net.Http.HttpResponseMessage response, string because = "")
    {
        response.IsSuccessStatusCode.Should().BeTrue(because);
    }

    public static void ShouldBeUnsuccessful(this System.Net.Http.HttpResponseMessage response, string because = "")
    {
        response.IsSuccessStatusCode.Should().BeFalse(because);
    }

    public static void ShouldHaveStatusCode(this System.Net.Http.HttpResponseMessage response, HttpStatusCode expectedStatusCode, string because = "")
    {
        response.StatusCode.Should().Be(expectedStatusCode, because);
    }
}

/// <summary>
/// Controller assertion helpers for testing MVC controllers
/// </summary>
public static class ControllerAssertionUtilities
{
    /// <summary>
    /// Asserts that an ActionResult has the expected status code and returns the expected value type.
    /// </summary>
    public static T AssertActionResult<T>(ActionResult<T> result, int expectedStatusCode, string because = "")
    {
        result.Should().NotBeNull(because);
        result.Result.Should().BeOfType<ObjectResult>(because);
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(expectedStatusCode, because);
        objectResult.Value.Should().BeOfType<T>(because);
        return (T)objectResult.Value!;
    }

    /// <summary>
    /// Asserts that an IActionResult is an OkObjectResult with the expected value type.
    /// </summary>
    public static T AssertOkResult<T>(IActionResult result, T? expectedValue = default, string because = "")
    {
        result.Should().NotBeNull(because);
        result.Should().BeOfType<OkObjectResult>(because);
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeOfType<T>(because);
        
        if (expectedValue != null)
        {
            okResult.Value.Should().BeEquivalentTo(expectedValue, because);
        }
        
        return (T)okResult.Value!;
    }

    /// <summary>
    /// Asserts that an ActionResult<T> is an OkObjectResult with the expected value.
    /// </summary>
    public static T AssertOkResult<T>(ActionResult<T> result, T? expectedValue = default, string because = "")
    {
        result.Should().NotBeNull(because);
        result.Result.Should().BeOfType<OkObjectResult>(because);
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeOfType<T>(because);
        
        if (expectedValue != null)
        {
            okResult.Value.Should().BeEquivalentTo(expectedValue, because);
        }
        
        return (T)okResult.Value!;
    }

    /// <summary>
    /// Asserts that an IActionResult is a BadRequestObjectResult with an error message.
    /// </summary>
    public static void AssertBadRequestResult(IActionResult result, string? expectedMessage = null, string because = "")
    {
        result.Should().NotBeNull(because);
        result.Should().BeOfType<BadRequestObjectResult>(because);
        var badRequestResult = result as BadRequestObjectResult;
        
        if (expectedMessage != null)
        {
            badRequestResult!.Value.Should().NotBeNull(because);
            var errorValue = badRequestResult.Value?.ToString();
            errorValue.Should().Contain(expectedMessage, because);
        }
    }

    /// <summary>
    /// Asserts that an IActionResult is an UnauthorizedObjectResult with an error message.
    /// </summary>
    public static void AssertUnauthorizedResult(IActionResult result, string? expectedMessage = null, string because = "")
    {
        result.Should().NotBeNull(because);
        result.Should().BeOfType<UnauthorizedObjectResult>(because);
        var unauthorizedResult = result as UnauthorizedObjectResult;
        
        if (expectedMessage != null)
        {
            unauthorizedResult!.Value.Should().NotBeNull(because);
            var errorValue = unauthorizedResult.Value?.ToString();
            errorValue.Should().Contain(expectedMessage, because);
        }
    }

    /// <summary>
    /// Asserts that an ActionResult is a NotFoundResult.
    /// </summary>
    public static void AssertNotFoundResult(ActionResult? result, string because = "")
    {
        result.Should().NotBeNull(because);
        result.Should().BeOfType<NotFoundResult>(because);
    }

    /// <summary>
    /// Asserts that an ActionResult has the expected status code.
    /// </summary>
    public static void AssertErrorResult(ActionResult? result, int expectedStatusCode, string? expectedMessage = null, string because = "")
    {
        result.Should().NotBeNull(because);
        result.Should().BeOfType<ObjectResult>(because);
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(expectedStatusCode, because);
        
        if (expectedMessage != null)
        {
            objectResult.Value.Should().NotBeNull(because);
            var errorValue = objectResult.Value?.ToString();
            errorValue.Should().Contain(expectedMessage, because);
        }
    }

    /// <summary>
    /// Asserts that an ActionResult<T> is a BadRequestObjectResult with an error message.
    /// </summary>
    public static void AssertBadRequestResult<T>(ActionResult<T> result, string? expectedMessage = null, string because = "")
    {
        if (result.Result != null)
        {
            AssertBadRequestResult(result.Result, expectedMessage, because);
        }
        else
        {
            result.Should().NotBeNull(because);
            result.Result.Should().NotBeNull(because);
        }
    }

    /// <summary>
    /// Asserts that an ActionResult<T> is an UnauthorizedObjectResult with an error message.
    /// </summary>
    public static void AssertUnauthorizedResult<T>(ActionResult<T> result, string? expectedMessage = null, string because = "")
    {
        if (result.Result != null)
        {
            AssertUnauthorizedResult(result.Result, expectedMessage, because);
        }
        else
        {
            result.Should().NotBeNull(because);
            result.Result.Should().NotBeNull(because);
        }
    }

    /// <summary>
    /// Asserts that an ActionResult<T> is a NotFoundResult.
    /// </summary>
    public static void AssertNotFoundResult<T>(ActionResult<T> result, string because = "")
    {
        AssertNotFoundResult(result.Result, because);
    }
}

/// <summary>
/// HTTP client helpers for integration testing
/// </summary>
public static class HttpClientTestHelpers
{
    /// <summary>
    /// Sets a Bearer token on the HttpClient's default request headers.
    /// </summary>
    public static void SetBearerToken(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Asserts that an HTTP response has a success status code.
    /// </summary>
    public static void AssertSuccessResponse(HttpResponseMessage response, string because = "")
    {
        response.Should().NotBeNull(because);
        response.IsSuccessStatusCode.Should().BeTrue($"Expected success status code but got {response.StatusCode}. {because}");
    }

    /// <summary>
    /// Asserts that an HTTP response has the expected error status code.
    /// </summary>
    public static void AssertErrorResponse(HttpResponseMessage response, HttpStatusCode expectedStatus, string? expectedMessage = null, string because = "")
    {
        response.Should().NotBeNull(because);
        response.StatusCode.Should().Be(expectedStatus, because);
        
        if (expectedMessage != null)
        {
            var content = response.Content.ReadAsStringAsync().Result;
            content.Should().Contain(expectedMessage, because);
        }
    }

    /// <summary>
    /// Reads JSON content from an HTTP response and deserializes it.
    /// </summary>
    public static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<T>();
    }
}

/// <summary>
/// Database assertion helpers
/// </summary>
public static class DatabaseAssertionUtilities
{
    /// <summary>
    /// Asserts that an entity exists in the database with the given ID.
    /// </summary>
    public static async Task<T> AssertEntityExists<T>(AppDbContext context, object id, string because = "") where T : class
    {
        var entity = await context.Set<T>().FindAsync(id);
        entity.Should().NotBeNull($"Entity of type {typeof(T).Name} with ID {id} should exist. {because}");
        return entity!;
    }

    /// <summary>
    /// Asserts that an entity does not exist in the database with the given ID.
    /// </summary>
    public static async Task AssertEntityDoesNotExist<T>(AppDbContext context, object id, string because = "") where T : class
    {
        var entity = await context.Set<T>().FindAsync(id);
        entity.Should().BeNull($"Entity of type {typeof(T).Name} with ID {id} should not exist. {because}");
    }

    /// <summary>
    /// Seeds the database with multiple entities of the same type.
    /// </summary>
    public static async Task<List<T>> SeedDatabase<T>(AppDbContext context, params T[] entities) where T : class
    {
        context.Set<T>().AddRange(entities);
        await context.SaveChangesAsync();
        return entities.ToList();
    }
}

/// <summary>
/// Configuration testing utilities
/// </summary>
public static class ConfigurationTestUtilities
{
    // This class needs to be implemented based on the actual configuration interface used
    // For now, we'll leave these methods commented out as they depend on specific configuration types
    
    /*
    public static Mock<T> CreateConfigurationMock<T>(string settingName, string value) where T : class, IConfiguration
    {
        var mock = new Mock<T>();
        mock.Setup(x => x[settingName]).Returns(value);
        return mock;
    }

    public static Mock<T> CreateConfigurationMock<T>() where T : class
    {
        return new Mock<T>();
    }

    public static void VerifyConfigurationCalled<T>(this Mock<T> mock, string settingName, string because = "") where T : class, IConfiguration
    {
        mock.Verify(x => x[settingName], Times.Once, because);
    }

    public static void VerifyConfigurationCalled<T>(this Mock<T> mock, string settingName, Times times, string because = "") where T : class, IConfiguration
    {
        mock.Verify(x => x[settingName], times, because);
    }

    public static void VerifyConfigurationNotCalled<T>(this Mock<T> mock, string settingName, string because = "") where T : class, IConfiguration
    {
        mock.Verify(x => x[settingName], Times.Never, because);
    }
    */
}
