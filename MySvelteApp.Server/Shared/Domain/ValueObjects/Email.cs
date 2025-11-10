using System.Text.RegularExpressions;

namespace MySvelteApp.Server.Shared.Domain.ValueObjects;

/// <summary>
/// Email value object that ensures valid email format and normalizes email addresses.
/// </summary>
public sealed class Email : ValueObject
{
    private static readonly Regex EmailRegex = new("^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$", RegexOptions.Compiled);

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Email value object with validation and normalization.
    /// </summary>
    /// <param name="email">The email address string</param>
    /// <returns>A validated and normalized Email value object</returns>
    /// <exception cref="ArgumentException">Thrown when email is invalid</exception>
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        }

        var trimmedEmail = email.Trim();
        var normalizedEmail = trimmedEmail.ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalizedEmail))
        {
            throw new ArgumentException("Invalid email format.", nameof(email));
        }

        return new Email(normalizedEmail);
    }

    /// <summary>
    /// Attempts to create an Email value object, returning null if invalid.
    /// </summary>
    public static Email? TryCreate(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        try
        {
            return Create(email);
        }
        catch
        {
            return null;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    /// <summary>
    /// Implicit conversion to string for convenience.
    /// </summary>
    public static implicit operator string(Email email) => email.Value;
}

