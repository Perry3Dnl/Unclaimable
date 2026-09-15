using Xunit;

namespace Unclaimable.Tests;

public sealed class CoverageCoreBehaviorTests
{
    [Fact]
    public void DetailedCheckCollectsMultiplePolicyDiagnosticsWithMessages()
    {
        var options = new Options
        {
            MinimumLength = 20,
            MaximumLength = 40,
            AsciiOnly = true,
            RejectControlCharacters = true,
            RejectFormatCharacters = true,
            RejectInvisibleOnlyIdentifiers = true
        };
        options.EnableRule(Rule.Numbers);

        var policy = new Policy(new[] { "#" });
        var checker = new Checker(options, policy);
        var result = checker.CheckDetailed("_1 \u0001\u200Bé#-", includeMessages: true);
        var kinds = result.Diagnostics.Select(diagnostic => diagnostic.Kind).ToArray();

        Assert.Contains(MatchKind.TooShort, kinds);
        Assert.Contains(MatchKind.LeadingSeparator, kinds);
        Assert.Contains(MatchKind.TrailingSeparator, kinds);
        Assert.Contains(MatchKind.NumbersNotAllowed, kinds);
        Assert.Contains(MatchKind.InvalidCharacters, kinds);
        Assert.Contains(MatchKind.BlockedCharacter, kinds);
        Assert.All(result.Diagnostics, diagnostic => Assert.False(string.IsNullOrWhiteSpace(diagnostic.Message)));
    }

    [Fact]
    public void DetailedCheckCollectsMaximumLengthWithoutMessages()
    {
        var options = new Options
        {
            MinimumLength = 0,
            MaximumLength = 4
        };

        var result = new Checker(options).CheckDetailed("abcdefgh", includeMessages: false);
        var diagnostic = Assert.Single(result.Diagnostics.Where(item => item.Kind == MatchKind.TooLong));

        Assert.Null(diagnostic.Message);
    }

    [Fact]
    public void DetailedCheckReportsMalformedUtf16AndStopsFurtherCollection()
    {
        var input = "abc\uD800";
        var checker = new Checker();

        var result = checker.CheckDetailed(input, includeMessages: true);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(MatchKind.InvalidCharacters, diagnostic.Kind);
        Assert.Equal(3, diagnostic.OffendingCharacterIndex);
        Assert.Contains("UTF-16", diagnostic.Message);
        Assert.Equal(MatchKind.InvalidCharacters, checker.Check(input).MatchKind);
    }

    [Fact]
    public void InvisibleCombiningMarksAreRejectedWhenNoOtherStrictUnicodeRuleApplies()
    {
        var options = new Options
        {
            MinimumLength = 0,
            RejectControlCharacters = false,
            RejectFormatCharacters = false,
            RejectInvisibleOnlyIdentifiers = true
        };

        var checker = new Checker(options);
        var input = "\u0301\u0300\u0301";

        Assert.Equal(MatchKind.InvalidCharacters, checker.Check(input).MatchKind);
        Assert.Contains(
            checker.CheckDetailed(input, includeMessages: true).Diagnostics,
            diagnostic => diagnostic.Kind == MatchKind.InvalidCharacters
                          && diagnostic.Message?.Contains("visible", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void SupplementaryUnicodeScalarIsReportedAsOneOffendingCharacter()
    {
        var options = new Options
        {
            MinimumLength = 0,
            AsciiOnly = true,
            RejectInvisibleOnlyIdentifiers = false,
            RejectControlCharacters = false,
            RejectFormatCharacters = false
        };

        var result = new Checker(options).Check("abc😀");

        Assert.Equal(MatchKind.InvalidCharacters, result.MatchKind);
        Assert.Equal(3, result.OffendingCharacterIndex);
        Assert.Equal("😀", result.OffendingCharacter);
    }

    [Fact]
    public void SupplementaryUnicodeScalarCanPassThePolicyLoopWhenUnicodeIsAllowed()
    {
        var options = new Options
        {
            MinimumLength = 0,
            RejectInvisibleOnlyIdentifiers = false,
            RejectControlCharacters = false,
            RejectFormatCharacters = false
        };

        Assert.True(new Checker(options).IsClaimable("hello😀world"));
    }

    [Fact]
    public void DetailedReservedDiagnosticsBuildMessagesForEveryReservedMatchShape()
    {
        AssertReservedDiagnostic(
            new Options().Reserve("coveragebrand", ReservedMatchMode.Exact),
            "coveragebrand",
            MatchKind.Exact);

        var compact = new Options().Reserve("coveragebrand", ReservedMatchMode.Default);
        compact.DisableRule(Rule.BlockedCharacters);
        AssertReservedDiagnostic(compact, "coverage-brand", MatchKind.Compact);

        AssertReservedDiagnostic(
            new Options().Reserve("coveragebrand", ReservedMatchMode.Default),
            "mycoveragebrandname",
            MatchKind.Partial);

        AssertReservedDiagnostic(
            new Options().Reserve("leetcode", ReservedMatchMode.Default),
            "l33tc0d3",
            MatchKind.Obfuscated);

        AssertReservedDiagnostic(
            new Options().Reserve("apple", ReservedMatchMode.Default),
            "аpple",
            MatchKind.UnicodeConfusable);

        var country = new Options().EnableRule(Rule.CountryNames);
        var countryResult = new Checker(country).CheckDetailed("france", includeMessages: true);
        var countryDiagnostic = Assert.Single(countryResult.Diagnostics.Where(item => item.Kind == MatchKind.CountryName));
        Assert.Equal("This value is not allowed.", countryDiagnostic.Message);
    }

    [Fact]
    public void PatternDiagnosticsProduceMessagesForEveryPatternKind()
    {
        AssertPatternDiagnostic(new Options(), "12345", MatchKind.NumericOnly);
        AssertPatternDiagnostic(new Options(), "8===D", MatchKind.AsciiArt);
        AssertPatternDiagnostic(new Options(), "!!!", MatchKind.SymbolOnly);
        AssertPatternDiagnostic(new Options(), "abababab", MatchKind.RepeatedPattern);

        var uppercase = new Options().EnablePattern(Pattern.UppercaseOnly);
        AssertPatternDiagnostic(uppercase, "COVERAGE", MatchKind.UppercaseOnly);

        var nullResult = new Checker().CheckDetailed(null, includeMessages: true);
        Assert.Empty(nullResult.Diagnostics);
    }

    [Fact]
    public void UnicodeConfusableCanFlowIntoObfuscationAndPartialMatching()
    {
        var combined = new Options().Reserve("google", ReservedMatchMode.Default);
        var combinedResult = new Checker(combined).CheckDetailed("gооgl3", includeMessages: true);
        Assert.Contains(
            combinedResult.Diagnostics,
            diagnostic => diagnostic.Kind == MatchKind.Obfuscated && diagnostic.MatchedValue == "google");

        var partial = new Options().Reserve("admin", ReservedMatchMode.Default);
        var partialResult = new Checker(partial).CheckDetailed("xxаdminyy", includeMessages: true);
        Assert.Contains(
            partialResult.Diagnostics,
            diagnostic => diagnostic.Kind == MatchKind.Partial && diagnostic.MatchedValue == "admin");
    }

    [Fact]
    public void UnicodeConfusableCanUseCompactMatching()
    {
        var options = new Options().Reserve("foo-bar", ReservedMatchMode.Default);
        options.DisableRule(Rule.BlockedCharacters);

        var result = new Checker(options).CheckDetailed("fоо_bar", includeMessages: true);

        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Kind == MatchKind.UnicodeConfusable
                          && diagnostic.MatchedValue == "foo-bar");
    }

    [Fact]
    public void ObfuscationSupportsPartialAndNonCompactModes()
    {
        var partial = new Options().Reserve("admin", ReservedMatchMode.Default);
        var partialResult = new Checker(partial).CheckDetailed("xx4dm1nyy", includeMessages: true);
        Assert.Contains(partialResult.Diagnostics, diagnostic => diagnostic.Kind == MatchKind.Partial);

        var nonCompact = new Options().Reserve("a-b", ReservedMatchMode.Default);
        nonCompact.DisableRule(Rule.CompactMatching | Rule.BlockedCharacters);
        var nonCompactResult = new Checker(nonCompact).Check("4-b");
        Assert.Equal(MatchKind.Obfuscated, nonCompactResult.MatchKind);

        var surrogate = new Options().Reserve("nevermatches", ReservedMatchMode.Default);
        Assert.True(new Checker(surrogate).IsClaimable("😀4xyz"));
    }

    [Fact]
    public void CheckerConstructorRejectsInvalidConfiguration()
    {
        Assert.Throws<ArgumentNullException>(() => new Checker((Options)null!));
        Assert.Throws<ArgumentNullException>(() => new Checker(new Options(), null!));

        Assert.Throws<ArgumentOutOfRangeException>(() => new Checker(new Options { MinimumLength = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Checker(new Options { MaximumLength = 0 }));
        Assert.Throws<ArgumentException>(() => new Checker(new Options { MinimumLength = 5, MaximumLength = 4 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Checker(new Options { PartialMatchMinimumLength = 0 }));
    }

    [Fact]
    public void PolicyValidationCoversNullArraysSurrogatesAndInvalidScalars()
    {
        Assert.Throws<ArgumentNullException>(() => new Policy(null!));

        var policy = new Policy();
        Assert.Throws<ArgumentNullException>(() => policy.BlockCharacters(null!));
        Assert.Throws<ArgumentNullException>(() => policy.AllowCharacters(null!));
        Assert.Throws<ArgumentException>(() => policy.BlockCharacter(""));
        Assert.Throws<ArgumentException>(() => policy.BlockCharacter("\uD800"));
        Assert.Throws<ArgumentException>(() => policy.AllowCharacter("ab"));

        policy.BlockCharacter("😀");
        Assert.True(policy.IsCharacterBlocked("😀"));
        Assert.True(policy.AllowCharacter("😀"));
        Assert.False(policy.IsCharacterBlocked("😀"));
        Assert.True(policy.IsCharacterExplicitlyAllowed("😀"));
    }

    private static void AssertReservedDiagnostic(Options options, string input, MatchKind kind)
    {
        var result = new Checker(options).CheckDetailed(input, includeMessages: true);
        var diagnostic = Assert.Single(result.Diagnostics.Where(item => item.Kind == kind));
        Assert.False(string.IsNullOrWhiteSpace(diagnostic.Message));
    }

    private static void AssertPatternDiagnostic(Options options, string input, MatchKind kind)
    {
        var result = new Checker(options).CheckDetailed(input, includeMessages: true);
        var diagnostic = Assert.Single(result.Diagnostics.Where(item => item.Kind == kind));
        Assert.False(string.IsNullOrWhiteSpace(diagnostic.Message));
    }
}
