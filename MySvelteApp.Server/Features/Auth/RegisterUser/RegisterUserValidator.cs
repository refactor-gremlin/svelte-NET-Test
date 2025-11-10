using FluentValidation;
using MySvelteApp.Server.Features.Auth.RegisterUser;
using System.Text.RegularExpressions;

namespace MySvelteApp.Server.Features.Auth.RegisterUser;

public class RegisterUserValidator : AbstractValidator<RegisterUserRequest>
{
    private static readonly Regex UsernameRegex = new("^[a-zA-Z0-9_]+$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new("^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$", RegexOptions.Compiled);
    private static readonly Regex PasswordRegex = new("(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)", RegexOptions.Compiled);

    public RegisterUserValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
            .Matches(UsernameRegex).WithMessage("Username can only contain letters, numbers, and underscores.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .Matches(EmailRegex).WithMessage("Please enter a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches(PasswordRegex).WithMessage("Password must contain at least one uppercase letter, one lowercase letter, and one number.");
    }
}

