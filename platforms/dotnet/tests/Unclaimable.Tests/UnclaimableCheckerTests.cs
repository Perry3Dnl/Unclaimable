using Xunit;

namespace Unclaimable.Tests;

public sealed class UnclaimableCheckerTests
{
    private static readonly UnclaimableChecker EnglishChecker = new UnclaimableChecker(new UnclaimableOptions());

    [Theory]
    [InlineData("admin")]
    [InlineData("moderator")]
    [InlineData("support")]
    [InlineData("system")]
    [InlineData("apple")]
    [InlineData("nike")]
    public void BuiltInReservedNamesAreRejected(string value)
    {
        Assert.True(EnglishChecker.IsReserved(value));
        Assert.False(EnglishChecker.IsClaimable(value));
    }

    [Theory]
    [InlineData("adminold", "admin")]
    [InlineData("supportive", "support")]
    [InlineData("apples", "apple")]
    [InlineData("nikee", "nike")]
    public void StrictPartialMatchingIsEnabledByDefault(string value, string expectedMatch)
    {
        var result = EnglishChecker.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(UnclaimableMatchKind.Partial, result.MatchKind);
        Assert.Equal(expectedMatch, result.MatchedValue);
    }

    [Theory]
    [InlineData("john-doe", "-")]
    [InlineData("john_doe", "_")]
    [InlineData("john doe", " ")]
    public void StructuralCharactersAreBlockedByDefault(string value, string expectedCharacter)
    {
        var result = UnclaimableChecker.Default.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(UnclaimableMatchKind.BlockedCharacter, result.MatchKind);
        Assert.Equal(expectedCharacter, result.OffendingCharacter);
    }

    [Fact]
    public void LeadingAndTrailingSeparatorsAreRejectedByDefault()
    {
        Assert.Equal(UnclaimableMatchKind.LeadingSeparator, UnclaimableChecker.Default.Check(".john").MatchKind);
        Assert.Equal(UnclaimableMatchKind.TrailingSeparator, UnclaimableChecker.Default.Check("john.").MatchKind);
    }

    [Fact]
    public void NumbersAreRejectedByDefault()
    {
        var result = UnclaimableChecker.Default.Check("ordinary2");

        Assert.True(result.IsReserved);
        Assert.Equal(UnclaimableMatchKind.NumbersNotAllowed, result.MatchKind);
        Assert.Equal("2", result.OffendingCharacter);
    }

    [Fact]
    public void UnicodeDecimalDigitsAreRejectedByDefault()
    {
        var result = UnclaimableChecker.Default.Check("user\u0661");

        Assert.True(result.IsReserved);
        Assert.Equal(UnclaimableMatchKind.NumbersNotAllowed, result.MatchKind);
        Assert.Equal("\u0661", result.OffendingCharacter);
    }

    [Fact]
    public void LengthRulesAreEnabledByDefault()
    {
        Assert.Equal(UnclaimableMatchKind.TooShort, UnclaimableChecker.Default.Check("ab").MatchKind);
        Assert.Equal(UnclaimableMatchKind.TooLong, UnclaimableChecker.Default.Check(new string('a', 33)).MatchKind);
    }

    [Fact]
    public void LengthThresholdsCanBeConfigured()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            MinimumLength = 5,
            MaximumLength = 8
        });

        Assert.Equal(UnclaimableMatchKind.TooShort, checker.Check("four").MatchKind);
        Assert.True(checker.IsClaimable("normal"));
        Assert.Equal(UnclaimableMatchKind.TooLong, checker.Check("toolonggg").MatchKind);
    }

    [Fact]
    public void RulesCanBeRelaxedThroughDisabledRules()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Standard,
            DisabledRules = UnclaimableRule.Numbers
                            | UnclaimableRule.BlockedCharacters
                            | UnclaimableRule.Whitespace
                            | UnclaimableRule.LeadingSeparator
                            | UnclaimableRule.TrailingSeparator
        });

        Assert.True(checker.IsClaimable("ordinary-user2"));
        Assert.True(checker.IsClaimable("ordinary user"));
    }

    [Fact]
    public void AdditionalBlockedCharactersAreAppliedAtStartup()
    {
        var options = new UnclaimableOptions();
        options.AdditionalBlockedCharacters("^", "$", "@");

        var checker = new UnclaimableChecker(options);

        var result = checker.Check("john^doe");

        Assert.True(result.IsReserved);
        Assert.Equal(UnclaimableMatchKind.BlockedCharacter, result.MatchKind);
        Assert.Equal("^", result.OffendingCharacter);
    }

    [Theory]
    [InlineData("N1ke", "nike", "brands")]
    [InlineData("N1k3", "nike", "brands")]
    [InlineData("G00gle", "google", "technology")]
    [InlineData("r00t", "root", "roles")]
    public void ObfuscationMatchingStillWorksWhenNumberPolicyIsRelaxed(
        string value,
        string matchedValue,
        string category)
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            DisabledRules = UnclaimableRule.Numbers
        });

        var result = checker.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(matchedValue, result.MatchedValue);
        Assert.Equal(category, result.Category);
        Assert.Equal(UnclaimableMatchKind.Obfuscated, result.MatchKind);
    }

    [Fact]
    public void CompactMatchingStillWorksWhenStructuralCharacterRulesAreRelaxed()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            DisabledRules = UnclaimableRule.BlockedCharacters
                            | UnclaimableRule.Whitespace
        });

        var result = checker.Check("customer-service");

        Assert.True(result.IsReserved);
        Assert.Equal("customer service", result.MatchedValue);
        Assert.Equal("support", result.Category);
        Assert.Equal(UnclaimableMatchKind.Compact, result.MatchKind);
    }

    [Fact]
    public void ApplicationSpecificNamesCanBeAddedWithoutChangingGlobalData()
    {
        var options = new UnclaimableOptions();
        options.AdditionalReserved.Add("examplebrand");

        var checker = new UnclaimableChecker(options);

        Assert.True(checker.IsReserved("ExampleBrand"));
        Assert.Equal("custom", checker.Check("examplebrand").Category);
    }

    [Fact]
    public void DetailedCheckCollectsPolicyAndReservedNameDiagnostics()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions());
        var result = checker.CheckDetailed("admin2", includeMessages: true);

        Assert.True(result.IsReserved);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Kind == UnclaimableMatchKind.NumbersNotAllowed);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Kind == UnclaimableMatchKind.Partial
            && diagnostic.MatchedValue == "admin");
    }
}
