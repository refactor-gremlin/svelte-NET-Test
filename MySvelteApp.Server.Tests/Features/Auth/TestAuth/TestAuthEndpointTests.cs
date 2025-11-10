using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using MySvelteApp.Server.Features.Auth.TestAuth;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.Features.Auth.TestAuth;

public class TestAuthEndpointTests : ControllerTestTemplate<TestAuthEndpoint>
{
    protected override TestAuthEndpoint CreateController()
    {
        return new TestAuthEndpoint();
    }

    [Fact]
    public void Handle_ReturnsOkWithMessage()
    {
        // Arrange - No setup needed, endpoint doesn't require authentication

        // Act
        var result = Controller.Handle();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        
        // Check the anonymous object properties using reflection
        var responseType = okResult.Value!.GetType();
        var messageProperty = responseType.GetProperty("Message");
        messageProperty.Should().NotBeNull();
        var messageValue = messageProperty!.GetValue(okResult.Value)?.ToString();
        messageValue.Should().Be("If you can see this, you are authenticated!");
    }
}

