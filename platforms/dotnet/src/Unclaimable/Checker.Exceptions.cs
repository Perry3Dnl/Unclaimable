using System.Text;

namespace Unclaimable;

public sealed partial class Checker
{
    private readonly Dictionary<string, Rule> _ruleExceptions =
        new Dictionary<string, Rule>(StringComparer.Ordinal);

    private readonly Dictionary<string, Pattern> _patternExceptions =
        new Dictionary<string, Pattern>(StringComparer.Ordinal);

    private readonly HashSet<string> _allowedRepeatedCharacters =
        new HashSet<string>(StringComparer.Ordinal);

    private void CaptureExceptions(Options options)
    {
        foreach (var exception in options.RuleExceptions)
        {
            var value = NormalizeExceptionIdentifier(exception.Value);
            if (_ruleExceptions.TryGetValue(value, out var existing))
            {
                _ruleExceptions[value] = existing | exception.Rules;
            }
            else
            {
                _ruleExceptions.Add(value, exception.Rules);
            }
        }

        foreach (var exception in options.PatternExceptions)
        {
            var value = NormalizeExceptionIdentifier(exception.Value);
            if (_patternExceptions.TryGetValue(value, out var existing))
            {
                _patternExceptions[value] = existing | exception.Patterns;
            }
            else
            {
                _patternExceptions.Add(value, exception.Patterns);
            }
        }

        foreach (var character in options.AllowedRepeatedCharacters)
        {
            _allowedRepeatedCharacters.Add(character);
        }
    }

    private bool IsRuleException(Rule rule, string? value)
    {
        if (value is null)
        {
            return false;
        }

        var normalized = NormalizeExceptionIdentifier(value);
        return _ruleExceptions.TryGetValue(normalized, out var rules)
               && (rules & rule) == rule;
    }

    private bool IsPatternException(Pattern pattern, string? value)
    {
        if (value is null)
        {
            return false;
        }

        var normalized = NormalizeExceptionIdentifier(value);
        return _patternExceptions.TryGetValue(normalized, out var patterns)
               && (patterns & pattern) == pattern;
    }

    private static string NormalizeExceptionIdentifier(string value) =>
        value.Normalize(NormalizationForm.FormKC).ToLowerInvariant();

    private static Rule? GetRuleForReservedEntry(ReservedEntry entry)
    {
        if (string.Equals(entry.Category, "profanity", StringComparison.Ordinal))
        {
            return Rule.Profanity;
        }

        return null;
    }

    private static Rule? GetRuleForIdentityCategory(string category)
    {
        switch (category)
        {
            case "celebrity":
                return Rule.CelebrityNames;
            case "nationality":
                return Rule.Nationalities;
            case "currency":
                return Rule.Currencies;
            case "religion":
                return Rule.Religions;
            case "landmark":
                return Rule.Landmarks;
            case "event":
                return Rule.Events;
            case "award":
                return Rule.Awards;
            case "fictionalcharacter":
                return Rule.FictionalCharacters;
            case "franchise":
                return Rule.Franchises;
            case "profession":
                return Rule.Professions;
            case "military":
                return Rule.Military;
            default:
                return null;
        }
    }
}
