namespace Unclaimable;

/// <summary>
/// Configures a <see cref="Checker"/>. Option values are captured when the checker is constructed;
/// later mutations to this object do not change that checker's matching configuration.
/// Runtime character-policy updates remain live through the checker's <see cref="IPolicy"/> instance.
/// </summary>
public sealed partial class Options
{
    /// <summary>Creates options using the strict default configuration.</summary>
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

    /// <summary>Built-in rules to disable. No rules are disabled by default.</summary>
    public Rule DisabledRules { get; set; } = Rule.None;

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

    /// <summary>
    /// Minimum accepted identifier length in UTF-16 code units, using <see cref="string.Length"/> semantics.
    /// Unicode scalar values and grapheme clusters can occupy more than one code unit. Defaults to 3.
    /// </summary>
    public int MinimumLength { get; set; } = 3;

    /// <summary>
    /// Maximum accepted identifier length in UTF-16 code units, using <see cref="string.Length"/> semantics.
    /// Unicode scalar values and grapheme clusters can occupy more than one code unit. Defaults to 32.
    /// </summary>
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
    /// Kept for compatibility; disable <see cref="Rule.PartialMatching"/> to turn the rule off entirely.
    /// </summary>
    public bool PartialMatching
    {
        get => _partialMatching || Strictness == Strictness.Strict;
        set => _partialMatching = value;
    }

    /// <summary>
    /// Minimum compact reserved-name length eligible for partial matching after dataset eligibility is established.
    /// Defaults to 4.
    /// </summary>
    public int PartialMatchMinimumLength { get; set; } = 4;

    /// <summary>
    /// Include profanity from the enabled localized dataset or datasets. Enabled by default.
    /// Kept for compatibility; prefer disabling <see cref="Rule.Profanity"/>.
    /// </summary>
    public bool ProfanityMatching { get; set; } = true;

    /// <summary>
    /// Allow ordinary profanity entries to participate in substring matching when partial matching is enabled.
    /// Curated profanity entries explicitly marked partial-safe can still participate without this option.
    /// This remains opt-in to avoid avoidable false positives for ordinary words.
    /// </summary>
    public bool ProfanityPartialMatching { get; set; }

    /// <summary>
    /// Detect common username obfuscation and leetspeak substitutions.
    /// Candidate expansion is deterministic and deliberately bounded to 32 generated candidates per identifier;
    /// inputs with more possible substitution combinations are not exhaustively enumerated.
    /// Kept for compatibility; prefer disabling <see cref="Rule.ObfuscationMatching"/>.
    /// </summary>
    public bool ObfuscationMatching { get; set; } = true;

    /// <summary>
    /// Detect selected common Unicode lookalikes and diacritic-based impersonation forms.
    /// This selected mapping is not a complete Unicode Technical Standard #39 confusable implementation.
    /// Kept for compatibility; prefer disabling <see cref="Rule.UnicodeConfusableMatching"/>.
    /// </summary>
    public bool UnicodeConfusableMatching { get; set; } = true;

    /// <summary>
    /// Allow Unicode decimal digits. Numbers are rejected by default.
    /// Kept for compatibility; prefer disabling <see cref="Rule.Numbers"/>.
    /// </summary>
    public bool AllowNumbers { get; set; }

    /// <summary>
    /// Reject input containing characters outside printable ASCII (U+0020 through U+007E).
    /// This additional restriction remains opt-in because Unclaimable supports Unicode-aware checks.
    /// </summary>
    public bool AsciiOnly { get; set; }

    /// <summary>
    /// Rejects identifiers containing only whitespace, formatting characters,
    /// control characters, or combining marks. Enabled by default.
    /// This is an approximation of visible content based on Unicode categories and does not determine actual rendering.
    /// </summary>
    public bool RejectInvisibleOnlyIdentifiers { get; set; } = true;

    /// <summary>Rejects Unicode control characters. Enabled by default.</summary>
    public bool RejectControlCharacters { get; set; } = true;

    /// <summary>
    /// Rejects Unicode formatting characters, including zero-width and
    /// bidirectional formatting characters. Enabled by default as part of the strict policy.
    /// This remains separately configurable because formatting characters can be legitimate in some languages.
    /// </summary>
    public bool RejectFormatCharacters { get; set; } = true;

    /// <summary>Optional application-wide fallback validation message used by the ASP.NET Core attribute.</summary>
    public string? ValidationMessage { get; set; }

    /// <summary>Optional reason-specific validation messages.</summary>
    public ValidationMessages Messages { get; } = new ValidationMessages();

    /// <summary>Application-specific names to reserve in addition to the shared dataset.</summary>
    public ICollection<string> AdditionalReserved { get; } = new List<string>();

    /// <summary>Characters configured at startup in addition to Unclaimable's built-in blocked characters.</summary>
    public IReadOnlyCollection<string> ConfiguredBlockedCharacters => _additionalBlockedCharacters;

    /// <summary>Adds application-specific blocked Unicode scalar values to the strict built-in character policy.</summary>
    /// <param name="characters">Unicode scalar values to block.</param>
    /// <returns>This options instance.</returns>
    public Options AdditionalBlockedCharacters(params string[] characters)
    {
        if (characters is null)
        {
            throw new ArgumentNullException(nameof(characters));
        }

        foreach (var character in characters)
        {
            ValidateCharacter(character, nameof(characters));
            _additionalBlockedCharacters.Add(character);
        }

        return this;
    }

    internal bool IsRuleEnabled(Rule rule) => (DisabledRules & rule) == 0;

    private static void ValidateLanguage(Language language, string parameterName)
    {
        if (!Enum.IsDefined(typeof(Language), language))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Language must be a supported Language value.");
        }
    }

    private static void ValidateCharacter(string value, string parameterName)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("A blocked character cannot be null or empty.", parameterName);
        }

        if (value.Length == 1 && !char.IsSurrogate(value[0]))
        {
            return;
        }

        if (value.Length == 2 && char.IsSurrogatePair(value[0], value[1]))
        {
            return;
        }

        throw new ArgumentException("Values must contain exactly one Unicode character.", parameterName);
    }
}
