using Xunit;

namespace Unclaimable.Tests;

public sealed class CheckerTests
{
    private static readonly Checker EnglishChecker = new Checker(new Options());

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
        Assert.Equal(MatchKind.Partial, result.MatchKind);
        Assert.Equal(expectedMatch, result.MatchedValue);
    }

    [Theory]
    [InlineData("john-doe", "-")]
    [InlineData("john_doe", "_")]
    [InlineData("john doe", " ")]
    public void StructuralCharactersAreBlockedByDefault(string value, string expectedCharacter)
    {
        var result = Checker.Default.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.BlockedCharacter, result.MatchKind);
        Assert.Equal(expectedCharacter, result.OffendingCharacter);
    }

    [Fact]
    public void LeadingAndTrailingSeparatorsAreRejectedByDefault()
    {
        Assert.Equal(MatchKind.LeadingSeparator, Checker.Default.Check(".john").MatchKind);
        Assert.Equal(MatchKind.TrailingSeparator, Checker.Default.Check("john.").MatchKind);
    }

    [Fact]
    public void NumbersAreRejectedByDefault()
    {
        var result = Checker.Default.Check("ordinary2");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.NumbersNotAllowed, result.MatchKind);
        Assert.Equal("2", result.OffendingCharacter);
    }

    [Fact]
    public void UnicodeDecimalDigitsAreRejectedByDefault()
    {
        var result = Checker.Default.Check("user\u0661");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.NumbersNotAllowed, result.MatchKind);
        Assert.Equal("\u0661", result.OffendingCharacter);
    }

    [Fact]
    public void LengthRulesAreEnabledByDefault()
    {
        Assert.Equal(MatchKind.TooShort, Checker.Default.Check("ab").MatchKind);
        Assert.Equal(MatchKind.TooLong, Checker.Default.Check(new string('a', 33)).MatchKind);
    }

    [Fact]
    public void LengthThresholdsCanBeConfigured()
    {
        var checker = new Checker(new Options
        {
            MinimumLength = 5,
            MaximumLength = 8
        });

        Assert.Equal(MatchKind.TooShort, checker.Check("four").MatchKind);
        Assert.True(checker.IsClaimable("normal"));
        Assert.Equal(MatchKind.TooLong, checker.Check("toolonggg").MatchKind);
    }

    [Fact]
    public void RulesCanBeRelaxedThroughDisabledRules()
    {
        var checker = new Checker(new Options
        {
            Strictness = Strictness.Standard,
            DisabledRules = Rule.Numbers
                            | Rule.BlockedCharacters
                            | Rule.Whitespace
                            | Rule.LeadingSeparator
                            | Rule.TrailingSeparator
        });

        Assert.True(checker.IsClaimable("ordinary-user2"));
        Assert.True(checker.IsClaimable("ordinary user"));
    }

    [Fact]
    public void AdditionalBlockedCharactersAreAppliedAtStartup()
    {
        var options = new Options();
        options.AdditionalBlockedCharacters("^", "$", "@");

        var checker = new Checker(options);

        var result = checker.Check("john^doe");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.BlockedCharacter, result.MatchKind);
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
        var checker = new Checker(new Options
        {
            DisabledRules = Rule.Numbers
        });

        var result = checker.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(matchedValue, result.MatchedValue);
        Assert.Equal(category, result.Category);
        Assert.Equal(MatchKind.Obfuscated, result.MatchKind);
    }

    [Fact]
    public void CompactMatchingStillWorksWhenStructuralCharacterRulesAreRelaxed()
    {
        var checker = new Checker(new Options
        {
            DisabledRules = Rule.BlockedCharacters
                            | Rule.Whitespace
        });

        var result = checker.Check("customer-service");

        Assert.True(result.IsReserved);
        Assert.Equal("customer service", result.MatchedValue);
        Assert.Equal("support", result.Category);
        Assert.Equal(MatchKind.Compact, result.MatchKind);
    }

    [Fact]
    public void ApplicationSpecificNamesCanBeAddedWithoutChangingGlobalData()
    {
        var options = new Options();
        options.AdditionalReserved.Add("examplebrand");

        var checker = new Checker(options);

        Assert.True(checker.IsReserved("ExampleBrand"));
        Assert.Equal("custom", checker.Check("examplebrand").Category);
    }

    [Fact]
    public void DetailedCheckCollectsPolicyAndReservedNameDiagnostics()
    {
        var checker = new Checker(new Options());
        var result = checker.CheckDetailed("admin2", includeMessages: true);

        Assert.True(result.IsReserved);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Kind == MatchKind.NumbersNotAllowed);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Kind == MatchKind.Partial
            && diagnostic.MatchedValue == "admin");
    }
}
