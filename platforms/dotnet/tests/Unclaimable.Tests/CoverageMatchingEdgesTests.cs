using Xunit;

namespace Unclaimable.Tests;

public sealed class CoverageMatchingEdgesTests
{
    public static TheoryData<char, char> UnicodeConfusables => new()
    {
        { '\u0430', 'a' }, { '\u0432', 'b' }, { '\u0435', 'e' }, { '\u043A', 'k' },
        { '\u043C', 'm' }, { '\u043D', 'h' }, { '\u043E', 'o' }, { '\u0440', 'p' },
        { '\u0441', 'c' }, { '\u0442', 't' }, { '\u0443', 'y' }, { '\u0445', 'x' },
        { '\u0455', 's' }, { '\u0456', 'i' }, { '\u0458', 'j' }, { '\u04CF', 'l' },
        { '\u03B1', 'a' }, { '\u03B2', 'b' }, { '\u03B5', 'e' }, { '\u03B9', 'i' },
        { '\u03BA', 'k' }, { '\u03BC', 'm' }, { '\u03BD', 'v' }, { '\u03BF', 'o' },
        { '\u03C1', 'p' }, { '\u03C4', 't' }, { '\u03C5', 'y' }, { '\u03C7', 'x' },
        { '\u03F2', 'c' }, { '\u0131', 'i' }
    };

    [Theory]
    [MemberData(nameof(UnicodeConfusables))]
    public void EverySupportedUnicodeConfusableMapsThroughPublicMatching(char confusable, char ascii)
    {
        var reserved = $"{ascii}xy";
        var input = $"{confusable}xy";
        var options = new Options().Reserve(reserved, ReservedMatchMode.Default);

        var result = new Checker(options).Check(input);

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.UnicodeConfusable, result.MatchKind);
        Assert.Equal(reserved, result.MatchedValue);
    }

    [Fact]
    public void ObfuscationCandidateExpansionIsBoundedForHighlyAmbiguousInput()
    {
        var options = new Options().Reserve("nevermatches", ReservedMatchMode.Default);
        options.DisablePattern(Pattern.NumericOnly | Pattern.Repeated);

        var checker = new Checker(options);

        Assert.True(checker.IsClaimable("111111a"));
    }

    [Fact]
    public void CompatibilityNormalizationExpansionStillMatchesWithoutOriginalSpan()
    {
        var options = new Options().Reserve("ffoo", ReservedMatchMode.Default);

        var result = new Checker(options).Check("ﬀoo");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Exact, result.MatchKind);
        Assert.Null(result.OriginalMatchStartIndex);
        Assert.Null(result.OriginalMatchLength);
    }

    [Fact]
    public void SupplementaryScalarReservationBuildsFallbackInputMapping()
    {
        var options = new Options().Reserve("a😀b", ReservedMatchMode.Default);

        var result = new Checker(options).Check("a😀b");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Exact, result.MatchKind);
        Assert.Equal(0, result.OriginalMatchStartIndex);
        Assert.Equal(4, result.OriginalMatchLength);
    }

    [Fact]
    public void AllowedIdentifierStillCannotBypassCustomExactReservation()
    {
        var options = new Options().Reserve("coverageexact", ReservedMatchMode.Default);
        options.AllowedIdentifiers.Add("coverageexact");

        var result = new Checker(options).Check("coverageexact");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Exact, result.MatchKind);
        Assert.Equal("custom", result.Category);
    }

    [Fact]
    public void AllowedIdentifierStillCannotBypassCustomCompactReservation()
    {
        var options = new Options().Reserve("coveragecompact", ReservedMatchMode.Default);
        options.AllowedIdentifiers.Add("coverage-compact");
        options.DisableRule(Rule.BlockedCharacters);

        var result = new Checker(options).Check("coverage-compact");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Compact, result.MatchKind);
        Assert.Equal("custom", result.Category);
    }

    [Fact]
    public void AllowedIdentifierStillCannotBypassCustomPartialReservation()
    {
        var options = new Options().Reserve("coveragepartial", ReservedMatchMode.Default);
        options.AllowedIdentifiers.Add("mycoveragepartialname");

        var result = new Checker(options).Check("mycoveragepartialname");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Partial, result.MatchKind);
        Assert.Equal("custom", result.Category);
    }

    [Fact]
    public void AllowedIdentifierStillCannotBypassCustomConfusableReservation()
    {
        var options = new Options().Reserve("applecoverage", ReservedMatchMode.Default);
        options.AllowedIdentifiers.Add("аpplecoverage");

        var result = new Checker(options).Check("аpplecoverage");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.UnicodeConfusable, result.MatchKind);
        Assert.Equal("custom", result.Category);
    }

    [Fact]
    public void AllowedIdentifierStillCannotBypassCustomObfuscatedReservation()
    {
        var options = new Options().Reserve("leetcoverage", ReservedMatchMode.Default);
        options.AllowedIdentifiers.Add("l33tc0v3r4g3");

        var result = new Checker(options).Check("l33tc0v3r4g3");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Obfuscated, result.MatchKind);
        Assert.Equal("custom", result.Category);
    }

    [Fact]
    public void NullLikeAllowedIdentifierEntriesAreIgnoredDuringCapture()
    {
        var options = new Options();
        options.AllowedIdentifiers.Add("   ");

        Assert.True(new Checker(options).IsClaimable("ordinarycoverageuser"));
    }
}
