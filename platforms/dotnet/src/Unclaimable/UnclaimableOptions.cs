namespace Unclaimable;

public sealed class UnclaimableOptions
{
    private bool _partialMatching;
    private readonly HashSet<string> _additionalBlockedCharacters = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Controls how aggressively reserved-name rules are applied.
    /// Strict is the default and enables embedded/partial reserved-name matching.
    /// </summary>
    public UnclaimableStrictness Strictness { get; set; } = UnclaimableStrictness.Strict;

    /// <summary>
    /// Built-in rules to disable. No rules are disabled by default.
    /// </summary>
    public UnclaimableRule DisabledRules { get; set; } = UnclaimableRule.None;

    /// <summary>
    /// Localized built-in dataset to use. Dutch is the default.
    /// Global datasets such as brands and technology are always included.
    /// </summary>
    public UnclaimableLanguage Language { get; set; } = UnclaimableLanguage.Dutch;

    /// <summary>
    /// Include every supported localized dataset instead of only <see cref="Language"/>.
    /// Disabled by default because it increases checker construction cost, memory usage,
    /// and the amount of work required by partial and obfuscation matching.
    /// </summary>
    public bool AllowMultiLanguage { get; set; }

    /// <summary>Minimum accepted identifier length. Defaults to 3.</summary>
    public int MinimumLength { get; set; } = 3;

    /// <summary>Maximum accepted identifier length. Defaults to 32.</summary>
    public int MaximumLength { get; set; } = 32;

    /// <summary>
    /// Also compare a compact form with separators and punctuation removed.
    /// Kept for compatibility; prefer disabling UnclaimableRule.CompactMatching.
    /// </summary>
    public bool CompactMatching { get; set; } = true;

    /// <summary>
    /// Also reject usernames that contain a reserved value as part of a larger value.
    /// Strict mode enables this automatically. Kept for compatibility; prefer
    /// disabling UnclaimableRule.PartialMatching when relaxation is required.
    /// </summary>
    public bool PartialMatching
    {
        get => _partialMatching || Strictness == UnclaimableStrictness.Strict;
        set => _partialMatching = value;
    }

    /// <summary>Minimum compact reserved-name length eligible for partial matching.</summary>
    public int PartialMatchMinimumLength { get; set; } = 4;

    /// <summary>
    /// Include profanity from the enabled localized dataset or datasets. Enabled by default.
    /// Kept for compatibility; prefer disabling UnclaimableRule.Profanity.
    /// </summary>
    public bool ProfanityMatching { get; set; } = true;

    /// <summary>
    /// Allow profanity entries to participate in substring matching when partial matching is enabled.
    /// This remains opt-in to avoid avoidable false positives for ordinary words.
    /// </summary>
    public bool ProfanityPartialMatching { get; set; }

    /// <summary>
    /// Detect common username obfuscation and leetspeak substitutions.
    /// Kept for compatibility; prefer disabling UnclaimableRule.ObfuscationMatching.
    /// </summary>
    public bool ObfuscationMatching { get; set; } = true;

    /// <summary>
    /// Detect common Unicode lookalikes and diacritic-based impersonation.
    /// Kept for compatibility; prefer disabling UnclaimableRule.UnicodeConfusableMatching.
    /// </summary>
    public bool UnicodeConfusableMatching { get; set; } = true;

    /// <summary>
    /// Allow Unicode decimal digits. Numbers are rejected by default.
    /// Kept for compatibility; prefer disabling UnclaimableRule.Numbers.
    /// </summary>
    public bool AllowNumbers { get; set; }

    /// <summary>
    /// Reject input containing characters outside printable ASCII (U+0020 through U+007E).
    /// This additional restriction remains opt-in because Unclaimable supports Unicode-aware checks.
    /// </summary>
    public bool AsciiOnly { get; set; }

    /// <summary>
    /// Optional application-wide fallback validation message used by the ASP.NET Core attribute.
    /// </summary>
    public string? ValidationMessage { get; set; }

    /// <summary>Optional reason-specific validation messages.</summary>
    public UnclaimableValidationMessages Messages { get; } = new UnclaimableValidationMessages();

    /// <summary>Application-specific names to reserve in addition to the shared dataset.</summary>
    public ICollection<string> AdditionalReserved { get; } = new List<string>();

    /// <summary>
    /// Characters configured at startup in addition to Unclaimable's built-in blocked characters.
    /// </summary>
    public IReadOnlyCollection<string> ConfiguredBlockedCharacters => _additionalBlockedCharacters;

    /// <summary>
    /// Adds application-specific blocked characters to the strict built-in character policy.
    /// </summary>
    public UnclaimableOptions AdditionalBlockedCharacters(params string[] characters)
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

    internal bool IsRuleEnabled(UnclaimableRule rule) => (DisabledRules & rule) == 0;

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
