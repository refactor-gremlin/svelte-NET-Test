using MySvelteApp.Server.Shared.Domain.Entities;

namespace MySvelteApp.Server.Shared.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}

