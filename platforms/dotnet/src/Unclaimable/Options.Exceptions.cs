using System.Globalization;
using System.Text;

namespace Unclaimable;

public sealed partial class Options
{
    private readonly List<RuleExceptionRegistration> _ruleExceptions = new List<RuleExceptionRegistration>();
    private readonly List<PatternExceptionRegistration> _patternExceptions = new List<PatternExceptionRegistration>();
    private readonly HashSet<string> _allowedRepeatedCharacters = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> _allowedCharacters = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets characters explicitly allowed at checker construction time.
    /// These seed the runtime <see cref="IPolicy"/> without disabling the blocked-character or whitespace rules.
    /// </summary>
    public IReadOnlyCollection<string> ConfiguredAllowedCharacters => _allowedCharacters;

    /// <summary>
    /// Gets Unicode scalar values whose direct repetition is permitted by <see cref="Pattern.Repeated"/>.
    /// </summary>
    public IReadOnlyCollection<string> AllowedRepeatedCharacters => _allowedRepeatedCharacters;

    /// <summary>
    /// Allows a complete identifier to skip only the supplied rule or rules.
    /// Every other rule, pattern, reserved-name check, and explicit application reservation still runs.
    /// </summary>
    public Options AllowIdentifierForRule(string value, Rule rules)
    {
        ValidateExceptionValue(value, nameof(value));
        ValidateRule(rules, nameof(rules));
        if (rules == Rule.None)
        {
            throw new ArgumentOutOfRangeException(nameof(rules), "At least one rule must be supplied.");
        }

        _ruleExceptions.Add(new RuleExceptionRegistration(value, rules));
        return this;
    }

    /// <summary>
    /// Allows a complete identifier to skip only the supplied pattern check or checks.
    /// Every other pattern, rule, and reserved-name check still runs.
    /// </summary>
    public Options AllowIdentifierForPattern(string value, Pattern patterns)
    {
        ValidateExceptionValue(value, nameof(value));
        ValidatePattern(patterns, nameof(patterns));
        if (patterns == Pattern.None)
        {
            throw new ArgumentOutOfRangeException(nameof(patterns), "At least one pattern must be supplied.");
        }

        _patternExceptions.Add(new PatternExceptionRegistration(value, patterns));
        return this;
    }

    /// <summary>
    /// Explicitly allows one or more Unicode scalar values in the startup character policy.
    /// Later runtime calls to <see cref="IPolicy.BlockCharacter(string)"/> can block them again.
    /// </summary>
    public Options AllowCharacters(params string[] characters)
    {
        if (characters is null)
        {
            throw new ArgumentNullException(nameof(characters));
        }

        foreach (var character in characters)
        {
            ValidateCharacter(character, nameof(characters));
            _allowedCharacters.Add(character);
        }

        return this;
    }

    /// <summary>
    /// Allows direct runs of one or more Unicode scalar values through <see cref="Pattern.Repeated"/>.
    /// This does not disable cyclic-repeat detection for other content.
    /// </summary>
    public Options AllowRepeatedCharacters(params string[] characters)
    {
        if (characters is null)
        {
            throw new ArgumentNullException(nameof(characters));
        }

        foreach (var character in characters)
        {
            ValidateCharacter(character, nameof(characters));
            _allowedRepeatedCharacters.Add(NormalizeExceptionCharacter(character));
        }

        return this;
    }

    internal IReadOnlyList<RuleExceptionRegistration> RuleExceptions => _ruleExceptions;
    internal IReadOnlyList<PatternExceptionRegistration> PatternExceptions => _patternExceptions;

    private static void ValidateExceptionValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("An exception identifier cannot be null, empty, or whitespace.", parameterName);
        }
    }

    private static string NormalizeExceptionCharacter(string value) =>
        value.Normalize(NormalizationForm.FormKC).ToLowerInvariant();

    internal sealed class RuleExceptionRegistration
    {
        internal RuleExceptionRegistration(string value, Rule rules)
        {
            Value = value;
            Rules = rules;
        }

        internal string Value { get; }
        internal Rule Rules { get; }
    }

    internal sealed class PatternExceptionRegistration
    {
        internal PatternExceptionRegistration(string value, Pattern patterns)
        {
            Value = value;
            Patterns = patterns;
        }

        internal string Value { get; }
        internal Pattern Patterns { get; }
    }
}
