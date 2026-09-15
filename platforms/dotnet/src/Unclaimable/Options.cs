namespace Unclaimable;

/// <summary>
/// Configures a <see cref="Checker"/>. Option values are captured when the checker is constructed;
/// later mutations to this object do not change that checker's matching configuration.
/// Runtime character-policy updates remain live through the checker's <see cref="IPolicy"/> instance.
/// </summary>
public sealed partial class Options
{
    /// <summary>Creates options using the default configuration.</summary>
    public Options()
    {
    }

    private bool _partialMatching;
    private readonly HashSet<string> _additionalBlockedCharacters = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<Language> _languages = new HashSet<Language>
    {
        Language.English
    };

    /// <summary>
    /// Controls how aggressively reserved-name rules are applied.
    /// Strict is the default and enables the partial-matching capability even when
    /// <see cref="PartialMatching"/> is set to <see langword="false"/>. Built-in dataset entries
    /// still require explicit partial eligibility; strict mode does not turn every built-in value
    /// into a generic substring rule.
    /// </summary>
    public Strictness Strictness { get; set; } = Strictness.Strict;

    /// <summary>
    /// Built-in rules to disable. <see cref="Rule.Numbers"/> is disabled by default in 0.7.2 and later,
    /// so identifiers may contain Unicode decimal digits unless the rule is explicitly enabled.
    /// Named identity-list rules remain separately opt-in through <see cref="EnableRule(Rule)"/>.
    /// </summary>
    public Rule DisabledRules { get; set; } = Rule.Numbers;

    /// <summary>
    /// Localized built-in datasets currently enabled for this checker. English is enabled by default.
    /// Use <see cref="AddLanguage"/> and <see cref="RemoveLanguage"/> to change the enabled set.
    /// Removing language-pack folders from a source checkout is also supported; missing packs simply
    /// contribute no embedded entries. Global datasets such as brands and technology are always included.
    /// </summary>
    public IReadOnlyCollection<Language> Languages => _languages;

    /// <summary>Adds a localized built-in language dataset while keeping currently enabled languages.</summary>
    /// <param name="language">The supported language dataset to enable.</param>
    /// <returns>This options instance.</returns>
    public Options AddLanguage(Language language)
    {
        ValidateLanguage(language, nameof(language));
        _languages.Add(language);
        return this;
    }

    /// <summary>Removes a localized built-in language dataset.</summary>
    /// <param name="language">The supported language dataset to disable.</param>
    /// <returns>This options instance.</returns>
    public Options RemoveLanguage(Language language)
    {
        ValidateLanguage(language, nameof(language));
        _languages.Remove(language);
        return this;
    }

    /// <summary>Minimum accepted identifier length in UTF-16 code units. Defaults to 3.</summary>
    public int MinimumLength { get; set; } = 3;

    /// <summary>Maximum accepted identifier length in UTF-16 code units. Defaults to 32.</summary>
    public int MaximumLength { get; set; } = 32;

    /// <summary>
    /// Also compare a compact form with separators and punctuation removed.
    /// Kept for compatibility; prefer disabling <see cref="Rule.CompactMatching"/>.
    /// </summary>
    public bool CompactMatching { get; set; } = true;

    /// <summary>
    /// Applies <see cref="Rule.CompactMatching"/> consistently to partial and obfuscation matching
    /// as well as direct compact matching. Enabled by default as part of the strict 0.4.0 policy.
    /// Set this to <see langword="false"/> only when legacy compact-rule interaction is required.
    /// </summary>
    public bool ConsistentCompactMatching { get; set; } = true;

    /// <summary>
    /// Enables matching eligible reserved values inside larger identifiers.
    /// Strict mode enables this capability automatically even when this property is set to
    /// <see langword="false"/>. Since 0.6.0, built-in entries participate only when their dataset
    /// explicitly marks them as partial-safe; application-defined default reservations retain their
    /// configured partial-matching behavior.
    /// </summary>
    public bool PartialMatching
    {
        get => _partialMatching;
        set => _partialMatching = value;
    }

    /// <summary>
    /// Minimum compact length required before a reservation may participate in partial matching.
    /// Defaults to 3.
    /// </summary>
    public int PartialMatchMinimumLength { get; set; } = 3;

    /// <summary>Enables common leetspeak and obfuscation matching. Defaults to true.</summary>
    public bool ObfuscationMatching { get; set; } = true;

    /// <summary>Enables Unicode-confusable matching. Defaults to true.</summary>
    public bool UnicodeConfusableMatching { get; set; } = true;

    /// <summary>
    /// Allows Unicode decimal digits even when <see cref="Rule.Numbers"/> is enabled.
    /// Kept for compatibility; prefer disabling <see cref="Rule.Numbers"/>.
    /// </summary>
    public bool AllowNumbers { get; set; }

    /// <summary>When true, rejects non-ASCII input. Defaults to false.</summary>
    public bool AsciiOnly { get; set; }

    /// <summary>Rejects identifiers containing only invisible Unicode categories. Defaults to false.</summary>
    public bool RejectInvisibleOnlyIdentifiers { get; set; }

    /// <summary>Rejects Unicode control characters. Defaults to false.</summary>
    public bool RejectControlCharacters { get; set; }

    /// <summary>Rejects Unicode format characters. Defaults to false.</summary>
    public bool RejectFormatCharacters { get; set; }

    /// <summary>Enables the built-in profanity dataset. Defaults to true.</summary>
    public bool ProfanityMatching { get; set; } = true;

    /// <summary>Allows profanity entries to participate in partial matching. Defaults to false.</summary>
    public bool ProfanityPartialMatching { get; set; }

    /// <summary>Application-defined reserved identifiers using the default matching pipeline.</summary>
    public ICollection<string> AdditionalReserved { get; } = new List<string>();

    /// <summary>Additional blocked characters captured when a checker is constructed.</summary>
    public IReadOnlyCollection<string> ConfiguredBlockedCharacters => _additionalBlockedCharacters;

    /// <summary>Optional fallback validation message for ASP.NET Core integration.</summary>
    public string? ValidationMessage { get; set; }

    /// <summary>Optional reason-specific validation messages for ASP.NET Core integration.</summary>
    public ValidationMessages Messages { get; } = new ValidationMessages();

    /// <summary>Adds one or more blocked characters without replacing the current set.</summary>
    public Options AdditionalBlockedCharacters(params string[] characters)
    {
        if (characters is null)
        {
            throw new ArgumentNullException(nameof(characters));
        }

        foreach (var character in characters)
        {
            ValidateCharacterValue(character, nameof(characters));
            _additionalBlockedCharacters.Add(character);
        }

        return this;
    }

    /// <summary>Removes one or more configured blocked characters.</summary>
    public Options RemoveBlockedCharacters(params string[] characters)
    {
        if (characters is null)
        {
            throw new ArgumentNullException(nameof(characters));
        }

        foreach (var character in characters)
        {
            ValidateCharacterValue(character, nameof(characters));
            _additionalBlockedCharacters.Remove(character);
        }

        return this;
    }

    private static void ValidateCharacterValue(string character, string parameterName)
    {
        if (string.IsNullOrEmpty(character))
        {
            throw new ArgumentException("Character values cannot be null or empty.", parameterName);
        }

        if (character.Length == 1)
        {
            if (char.IsSurrogate(character[0]))
            {
                throw new ArgumentException("Character values must contain exactly one valid Unicode scalar.", parameterName);
            }

            return;
        }

        if (character.Length == 2 && char.IsSurrogatePair(character, 0))
        {
            return;
        }

        throw new ArgumentException("Character values must contain exactly one Unicode scalar.", parameterName);
    }

    private static void ValidateLanguage(Language language, string parameterName)
    {
        if (!Enum.IsDefined(typeof(Language), language))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Language must be a supported Language value.");
        }
    }
}
