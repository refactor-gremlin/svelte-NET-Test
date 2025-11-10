using FluentValidation;
using MySvelteApp.Server.Features.Auth.LoginUser;

namespace MySvelteApp.Server.Features.Auth.LoginUser;

public class LoginUserValidator : AbstractValidator<LoginUserRequest>
{
    public LoginUserValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

