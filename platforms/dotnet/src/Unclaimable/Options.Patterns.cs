namespace Unclaimable;

public sealed partial class Options
{
    private const Pattern DefaultPatterns =
        Pattern.NumericOnly
        | Pattern.Repeated
        | Pattern.SymbolOnly
        | Pattern.AsciiArt;

    private const Pattern AllPatterns =
        Pattern.NumericOnly
        | Pattern.Repeated
        | Pattern.SymbolOnly
        | Pattern.AsciiArt
        | Pattern.UppercaseOnly;

    private Pattern _enabledPatterns = DefaultPatterns;

    /// <summary>
    /// Gets the pattern checks enabled for newly constructed checkers.
    /// Numeric-only, repeated, symbol-only, and ASCII-art checks are enabled by default in 0.8.0 and later.
    /// <see cref="Pattern.UppercaseOnly"/> remains opt-in so ordinary uppercase identifiers stay claimable.
    /// </summary>
    public Pattern EnabledPatterns => _enabledPatterns;

    /// <summary>
    /// Minimum number of Unicode text elements that a repeated span must contain before
    /// <see cref="Pattern.Repeated"/> rejects it. The repeated span may occur anywhere
    /// inside the identifier. Defaults to 4.
    /// </summary>
    public int RepeatedPatternMinimumLength { get; set; } = 4;

    /// <summary>Enables one or more pattern checks without changing the remaining pattern configuration.</summary>
    /// <param name="pattern">One pattern or a bitwise combination of supported patterns.</param>
    /// <returns>This options instance.</returns>
    public Options EnablePattern(Pattern pattern)
    {
        ValidatePattern(pattern, nameof(pattern));
        _enabledPatterns |= pattern;
        return this;
    }

    /// <summary>Disables one or more pattern checks without changing the remaining pattern configuration.</summary>
    /// <param name="pattern">One pattern or a bitwise combination of supported patterns.</param>
    /// <returns>This options instance.</returns>
    public Options DisablePattern(Pattern pattern)
    {
        ValidatePattern(pattern, nameof(pattern));
        _enabledPatterns &= ~pattern;
        return this;
    }

    internal bool IsPatternEnabled(Pattern pattern) => (_enabledPatterns & pattern) == pattern;

    private static void ValidatePattern(Pattern pattern, string parameterName)
    {
        if ((pattern & ~AllPatterns) != 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Pattern must contain only supported Pattern values.");
        }
    }
}
