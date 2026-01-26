using System.Text.RegularExpressions;
using Mastery.Domain.Common;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.ValueObjects;

public sealed partial class Locale : ValueObject
{
    public string Code { get; }

    private Locale(string code)
    {
        Code = code;
    }

    public static Locale Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Locale cannot be empty.");

        // Validate format: "en" or "en-US"
        if (!LocalePattern().IsMatch(code))
            throw new DomainException($"Invalid locale format: {code}. Expected format: 'en' or 'en-US'.");

        return new Locale(code);
    }

    public static Locale Default => new("en-US");

    public string Language => Code.Split('-')[0];

    public string? Region => Code.Contains('-') ? Code.Split('-')[1] : null;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }

    public override string ToString() => Code;

    public static implicit operator string(Locale locale) => locale.Code;

    [GeneratedRegex(@"^[a-z]{2}(-[A-Z]{2})?$")]
    private static partial Regex LocalePattern();
}
