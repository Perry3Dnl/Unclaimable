using Xunit;

namespace Unclaimable.Tests;

public sealed class CoveragePublicApiTests
{
    [Fact]
    public void OptionsRejectInvalidLanguagesCharactersCategoriesModesAndPatterns()
    {
        var options = new Options();

        Assert.Throws<ArgumentNullException>(() => options.AdditionalBlockedCharacters(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.AddLanguage((Language)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.RemoveLanguage((Language)999));
        Assert.Throws<ArgumentException>(() => options.AdditionalBlockedCharacters(""));
        Assert.Throws<ArgumentException>(() => options.AdditionalBlockedCharacters("\uD800"));
        Assert.Throws<ArgumentException>(() => options.AdditionalBlockedCharacters("ab"));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.DisableCategory((Category)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.EnableCategory((Category)999));
        Assert.Throws<ArgumentException>(() => options.Reserve("   "));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Reserve("validname", (ReservedMatchMode)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.EnablePattern((Pattern)(1 << 20)));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.DisablePattern((Pattern)(1 << 20)));

        options.AdditionalBlockedCharacters("😀", "#");
        Assert.Contains("😀", options.ConfiguredBlockedCharacters);
        Assert.Contains("#", options.ConfiguredBlockedCharacters);
    }

    [Fact]
    public void OptionsExposeAndMutateCategoryAndPatternCollections()
    {
        var options = new Options();

        Assert.Empty(options.DisabledCategories);
        options.DisableCategory(Category.Brands);
        Assert.Contains(Category.Brands, options.DisabledCategories);
        options.EnableCategory(Category.Brands);
        Assert.DoesNotContain(Category.Brands, options.DisabledCategories);

        var initial = options.EnabledPatterns;
        options.EnablePattern(Pattern.UppercaseOnly);
        Assert.True((options.EnabledPatterns & Pattern.UppercaseOnly) != 0);
        options.DisablePattern(Pattern.UppercaseOnly);
        Assert.Equal(initial, options.EnabledPatterns);
    }

    [Fact]
    public void ResultFactoriesAndPropertiesRemainInternallyConsistent()
    {
        var allowed = Result.Allowed("abc");
        Assert.True(allowed.IsClaimable);
        Assert.False(allowed.IsReserved);
        Assert.Equal(3, allowed.InputLength);

        var invalid = Result.InvalidCharacters("a#", 1, "#");
        Assert.Equal(MatchKind.InvalidCharacters, invalid.MatchKind);
        Assert.Equal(1, invalid.OffendingCharacterIndex);
        Assert.Equal("#", invalid.OffendingCharacter);

        Assert.Equal(MatchKind.NumbersNotAllowed, Result.NumbersNotAllowed("a1", 1, "1").MatchKind);
        Assert.Equal(MatchKind.TooShort, Result.TooShort("a").MatchKind);
        Assert.Equal(MatchKind.TooLong, Result.TooLong("abcdef").MatchKind);
        Assert.Equal(MatchKind.BlockedCharacter, Result.BlockedCharacter("a#", 1, "#").MatchKind);
        Assert.Equal(MatchKind.LeadingSeparator, Result.LeadingSeparator("_abc", 0, "_").MatchKind);
        Assert.Equal(MatchKind.TrailingSeparator, Result.TrailingSeparator("abc_", 3, "_").MatchKind);
    }

    [Fact]
    public void DiagnosticAndDetailedResultExposeEveryPublicProperty()
    {
        var diagnostic = new Diagnostic(
            MatchKind.Partial,
            matchedValue: "admin",
            category: "roles",
            offendingCharacterIndex: 2,
            offendingCharacter: "x",
            matchStartIndex: 3,
            matchLength: 5,
            message: "message");

        Assert.Equal(MatchKind.Partial, diagnostic.Kind);
        Assert.Equal("admin", diagnostic.MatchedValue);
        Assert.Equal("roles", diagnostic.Category);
        Assert.Equal(2, diagnostic.OffendingCharacterIndex);
        Assert.Equal("x", diagnostic.OffendingCharacter);
        Assert.Equal(3, diagnostic.MatchStartIndex);
        Assert.Equal(5, diagnostic.MatchLength);
        Assert.Null(diagnostic.OriginalMatchStartIndex);
        Assert.Null(diagnostic.OriginalMatchLength);
        Assert.Equal("message", diagnostic.Message);

        var detailed = new DetailedResult("abcdef", new[] { diagnostic });
        Assert.Equal("abcdef", detailed.Input);
        Assert.Equal(6, detailed.InputLength);
        Assert.True(detailed.IsReserved);
        Assert.False(detailed.IsClaimable);
        Assert.Same(diagnostic, Assert.Single(detailed.Diagnostics));

        var empty = new DetailedResult(null, Array.Empty<Diagnostic>());
        Assert.Equal(0, empty.InputLength);
        Assert.False(empty.IsReserved);
        Assert.True(empty.IsClaimable);

        Assert.Throws<ArgumentNullException>(() => new DetailedResult("input", null!));
    }

    [Fact]
    public void PolicyBlockedCharacterSnapshotHonorsAllowancesAndOrdering()
    {
        var policy = new Policy(new[] { "#", "@" });
        Assert.Equal(new[] { "#", "-", "@", "_" }, policy.BlockedCharacters);

        Assert.True(policy.AllowCharacter("-"));
        Assert.False(policy.AllowCharacter("-"));
        Assert.DoesNotContain("-", policy.BlockedCharacters);

        policy.BlockCharacters("$", "%");
        Assert.Contains("$", policy.BlockedCharacters);
        Assert.Contains("%", policy.BlockedCharacters);

        policy.AllowCharacters("$", "%");
        Assert.DoesNotContain("$", policy.BlockedCharacters);
        Assert.DoesNotContain("%", policy.BlockedCharacters);
    }
}
