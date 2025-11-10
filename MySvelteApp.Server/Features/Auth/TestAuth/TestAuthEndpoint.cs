using Microsoft.AspNetCore.Mvc;
using MySvelteApp.Server.Shared.Presentation.Common;

namespace MySvelteApp.Server.Features.Auth.TestAuth;

[ApiController]
[Route("auth/test")]
public class TestAuthEndpoint : ApiControllerBase
{
    [HttpGet]
    public IActionResult Handle()
    {
        return Ok(new { Message = "If you can see this, you are authenticated!" });
    }
}

