using Xunit;

namespace Unclaimable.Tests;

public sealed class OriginalSpan040Tests
{
    [Fact]
    public void ExactMatchReportsOriginalInputSpan()
    {
        var result = Checker.Default.Check("ADMIN");

        Assert.Equal(MatchKind.Exact, result.MatchKind);
        Assert.Equal(0, result.OriginalMatchStartIndex);
        Assert.Equal(5, result.OriginalMatchLength);
    }

    [Fact]
    public void TrimmedExactMatchReportsSpanInsideOriginalInput()
    {
        var options = new Options { DisabledRules = Rule.Whitespace };
        var result = new Checker(options).Check(" admin ");

        Assert.Equal(MatchKind.Exact, result.MatchKind);
        Assert.Equal(1, result.OriginalMatchStartIndex);
        Assert.Equal(5, result.OriginalMatchLength);
    }

    [Fact]
    public void CompactOriginalSpanIncludesInterveningPunctuation()
    {
        var options = RelaxStructuralRules(new Options());
        options.AdditionalReserved.Add("foobar");
        var result = new Checker(options).Check("foo-bar");

        Assert.Equal(MatchKind.Compact, result.MatchKind);
        Assert.Equal(0, result.MatchStartIndex);
        Assert.Equal(6, result.MatchLength);
        Assert.Equal(0, result.OriginalMatchStartIndex);
        Assert.Equal(7, result.OriginalMatchLength);
    }

    [Fact]
    public void CompactPartialOriginalSpanIncludesInterveningPunctuation()
    {
        var options = RelaxStructuralRules(new Options());
        options.AdditionalReserved.Add("foobar");
        var result = new Checker(options).Check("xxfoo-barzz");

        Assert.Equal(MatchKind.Partial, result.MatchKind);
        Assert.Equal(2, result.MatchStartIndex);
        Assert.Equal(6, result.MatchLength);
        Assert.Equal(2, result.OriginalMatchStartIndex);
        Assert.Equal(7, result.OriginalMatchLength);
    }

    [Fact]
    public void UncertainNormalizationLeavesOriginalSpanUnavailable()
    {
        var options = new Options { DisabledRules = Rule.MinimumLength };
        options.AdditionalReserved.Add("ffi");
        var result = new Checker(options).Check("\uFB03");

        Assert.Equal(MatchKind.Exact, result.MatchKind);
        Assert.Null(result.OriginalMatchStartIndex);
        Assert.Null(result.OriginalMatchLength);
    }

    [Fact]
    public void DetailedReservedDiagnosticCarriesOriginalSpan()
    {
        var options = RelaxStructuralRules(new Options());
        options.AdditionalReserved.Add("foobar");
        var result = new Checker(options).CheckDetailed("foo-bar");

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(0, diagnostic.OriginalMatchStartIndex);
        Assert.Equal(7, diagnostic.OriginalMatchLength);
    }

    [Fact]
    public void LengthFailuresCarryCapturedThresholds()
    {
        var checker = new Checker(new Options { MinimumLength = 5, MaximumLength = 8 });

        Assert.Equal(5, checker.Check("four").LengthLimit);
        Assert.Equal(8, checker.Check("toolonggg").LengthLimit);
    }

    [Fact]
    public void LengthLimitUsesCheckerSnapshotAfterOptionsMutation()
    {
        var options = new Options { MinimumLength = 5 };
        var checker = new Checker(options);
        options.MinimumLength = 8;

        var result = checker.Check("four");

        Assert.Equal(MatchKind.TooShort, result.MatchKind);
        Assert.Equal(5, result.LengthLimit);
    }

    private static Options RelaxStructuralRules(Options options)
    {
        options.DisabledRules |= Rule.BlockedCharacters | Rule.Whitespace | Rule.LeadingSeparator | Rule.TrailingSeparator;
        return options;
    }
}
