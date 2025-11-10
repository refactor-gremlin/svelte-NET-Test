using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySvelteApp.Server.Features.Pokemon.GetRandomPokemon;
using MySvelteApp.Server.Shared.Common.DTOs.Responses;
using MySvelteApp.Server.Shared.Presentation.Common;

namespace MySvelteApp.Server.Features.Pokemon.GetRandomPokemon;

[ApiController]
[Route("pokemon/random")]
public class GetRandomPokemonEndpoint : ApiControllerBase
{
    private readonly GetRandomPokemonQuery _handler;

    public GetRandomPokemonEndpoint(GetRandomPokemonQuery handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<GetRandomPokemonResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        var result = await _handler.HandleAsync(cancellationToken);
        return ToActionResult(result);
    }
}

