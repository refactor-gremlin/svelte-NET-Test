using System.Text.RegularExpressions;

namespace MySvelteApp.Server.Shared.Domain.ValueObjects;

/// <summary>
/// Username value object that ensures valid username format and normalization.
/// </summary>
public sealed class Username : ValueObject
{
    private static readonly Regex UsernameRegex = new("^[a-zA-Z0-9_]+$", RegexOptions.Compiled);
    private const int MinLength = 3;
    private const int MaxLength = 50;

    public string Value { get; }

    private Username(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Username value object with validation and normalization.
    /// </summary>
    /// <param name="username">The username string</param>
    /// <returns>A validated and normalized Username value object</returns>
    /// <exception cref="ArgumentException">Thrown when username is invalid</exception>
    public static Username Create(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be empty.", nameof(username));
        }

        var trimmedUsername = username.Trim();

        if (trimmedUsername.Length < MinLength)
        {
            throw new ArgumentException($"Username must be at least {MinLength} characters long.", nameof(username));
        }

        if (trimmedUsername.Length > MaxLength)
        {
            throw new ArgumentException($"Username must not exceed {MaxLength} characters.", nameof(username));
        }

        if (!UsernameRegex.IsMatch(trimmedUsername))
        {
            throw new ArgumentException("Username can only contain letters, numbers, and underscores.", nameof(username));
        }

        return new Username(trimmedUsername);
    }

    /// <summary>
    /// Attempts to create a Username value object, returning null if invalid.
    /// </summary>
    public static Username? TryCreate(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        try
        {
            return Create(username);
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
    public static implicit operator string(Username username) => username.Value;
}

