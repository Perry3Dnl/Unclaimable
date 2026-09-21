using Xunit;

namespace Unclaimable.Tests;

public sealed class ScopedExceptions080Tests
{
    [Fact]
    public void RuleExceptionSkipsOnlyTheNamedRule()
    {
        var options = new Options();
        options.AllowIdentifierForRule("charlotte", Rule.PopularCityNames);

        var result = new Checker(options).Check("charlotte");

        Assert.True(result.IsClaimable);
    }

    [Fact]
    public void RuleExceptionDoesNotBypassOtherDenyChecks()
    {
        var options = new Options();
        options.EnablePattern(Pattern.UppercaseOnly);
        options.AllowIdentifierForPattern("ADMIN", Pattern.UppercaseOnly);

        var result = new Checker(options).Check("ADMIN");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Exact, result.MatchKind);
        Assert.Equal("admin", result.MatchedValue);
        Assert.Equal("roles", result.Category);
    }

    [Fact]
    public void PatternExceptionSkipsOnlyTheNamedPattern()
    {
        var options = new Options();
        options.AllowIdentifierForPattern("ababab", Pattern.Repeated);

        Assert.True(new Checker(options).Check("ababab").IsClaimable);
    }

    [Fact]
    public void RuleAndPatternExceptionsCanBeCombinedForOneIdentifier()
    {
        var options = new Options();
        options.AllowIdentifierForRule("TTT_orchid7", Rule.BlockedCharacters);
        options.AllowIdentifierForPattern("TTT_orchid7", Pattern.Repeated);

        Assert.True(new Checker(options).Check("TTT_orchid7").IsClaimable);
    }

    [Fact]
    public void StartupCharacterAllowanceKeepsBlockedCharacterRuleEnabled()
    {
        var options = new Options();
        options.AllowCharacters("_");

        var checker = new Checker(options);

        Assert.True(checker.Check("orchid_user7").IsClaimable);
        Assert.Equal(MatchKind.BlockedCharacter, checker.Check("orchid-user7").MatchKind);
    }

    [Theory]
    [InlineData("TTT")]
    [InlineData("tttttt")]
    [InlineData("orchidTTT7")]
    public void AllowedRepeatedCharacterCanRepeatDirectly(string value)
    {
        var options = new Options();
        options.AllowRepeatedCharacters("T");

        Assert.True(new Checker(options).Check(value).IsClaimable);
    }

    [Fact]
    public void AllowedRepeatedCharacterDoesNotDisableOtherRepeatedPatterns()
    {
        var options = new Options();
        options.AllowRepeatedCharacters("T");

        var result = new Checker(options).Check("ababab");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.RepeatedPattern, result.MatchKind);
    }

    [Fact]
    public void TeamPrefixCanBeSupportedWithoutDisablingEitherRule()
    {
        var options = new Options();
        options.AllowCharacters("_");
        options.AllowRepeatedCharacters("T");

        var checker = new Checker(options);

        Assert.True(checker.Check("TTT_orchid7").IsClaimable);
        Assert.Equal(MatchKind.RepeatedPattern, checker.Check("AAA_orchid7").MatchKind);
    }

    [Fact]
    public void ExceptionConfigurationIsCapturedByChecker()
    {
        var options = new Options();
        options.AllowIdentifierForPattern("ababab", Pattern.Repeated);
        var captured = new Checker(options);

        options.AllowIdentifierForPattern("hahaha", Pattern.Repeated);

        Assert.True(captured.Check("ababab").IsClaimable);
        Assert.Equal(MatchKind.RepeatedPattern, captured.Check("hahaha").MatchKind);
        Assert.True(new Checker(options).Check("hahaha").IsClaimable);
    }

    public static IEnumerable<object[]> ProtectedIdentityRuleExceptionCases()
    {
        yield return new object[] { Rule.CountryNames, "france" };
        yield return new object[] { Rule.PopularCityNames, "charlotte" };
        yield return new object[] { Rule.CelebrityNames, "taylorswift" };
        yield return new object[] { Rule.Nationalities, "dutch" };
        yield return new object[] { Rule.Currencies, "eur" };
        yield return new object[] { Rule.Religions, "buddhism" };
        yield return new object[] { Rule.Landmarks, "eiffeltower" };
        yield return new object[] { Rule.Events, "eurovision" };
        yield return new object[] { Rule.Awards, "nobelprize" };
        yield return new object[] { Rule.FictionalCharacters, "darthvader" };
        yield return new object[] { Rule.Franchises, "starwars" };
        yield return new object[] { Rule.Professions, "physician" };
        yield return new object[] { Rule.Military, "airforce" };
    }

    [Theory]
    [MemberData(nameof(ProtectedIdentityRuleExceptionCases))]
    public void RuleExceptionCanSkipEachProtectedIdentityRule(Rule rule, string value)
    {
        const Rule allProtectedIdentityRules =
            Rule.CountryNames
            | Rule.PopularCityNames
            | Rule.CelebrityNames
            | Rule.Nationalities
            | Rule.Currencies
            | Rule.Religions
            | Rule.Landmarks
            | Rule.Events
            | Rule.Awards
            | Rule.FictionalCharacters
            | Rule.Franchises
            | Rule.Professions
            | Rule.Military;

        var options = new Options();
        options.DisableRule(allProtectedIdentityRules & ~rule);

        foreach (var category in Enum.GetValues<Category>())
        {
            options.DisableCategory(category);
        }

        options.AllowIdentifierForRule(value, rule);

        Assert.True(new Checker(options).Check(value).IsClaimable);
    }

    [Theory]
    [InlineData("ab", Rule.MinimumLength)]
    [InlineData("orchid garden", Rule.Whitespace)]
    [InlineData("orchid_user", Rule.BlockedCharacters)]
    [InlineData(".orchid", Rule.LeadingSeparator)]
    [InlineData("orchid.", Rule.TrailingSeparator)]
    [InlineData("supportive", Rule.PartialMatching)]
    [InlineData("N1k3", Rule.ObfuscationMatching)]
    [InlineData("\u0430pple", Rule.UnicodeConfusableMatching)]
    public void RuleExceptionCanSkipCoreRulesWithoutDisablingThemGlobally(string value, Rule rule)
    {
        var options = new Options();
        options.AllowIdentifierForRule(value, rule);

        Assert.True(new Checker(options).Check(value).IsClaimable);
    }

    [Fact]
    public void RuleExceptionCanSkipNumbersWhenNumbersRuleIsEnabled()
    {
        var options = new Options();
        options.EnableRule(Rule.Numbers);
        options.AllowIdentifierForRule("orchid7", Rule.Numbers);

        Assert.True(new Checker(options).Check("orchid7").IsClaimable);
        Assert.Equal(MatchKind.NumbersNotAllowed, new Checker(options).Check("garden7").MatchKind);
    }

    [Fact]
    public void RuleExceptionCanSkipMaximumLength()
    {
        const string value = "abcdefghijklmnopqrstuvwxyzabcdefg";
        var options = new Options();
        options.AllowIdentifierForRule(value, Rule.MaximumLength);

        Assert.True(new Checker(options).Check(value).IsClaimable);
    }

    [Fact]
    public void RuleExceptionCanSkipCompactMatchingForOneIdentifier()
    {
        var options = new Options();
        options.AllowCharacters("-");
        options.AllowIdentifierForRule("Taylor-Swift", Rule.CompactMatching);

        Assert.True(new Checker(options).Check("Taylor-Swift").IsClaimable);
        Assert.Equal("celebrity", new Checker(new Options().AllowCharacters("-")).Check("Taylor-Swift").Category);
    }

    [Fact]
    public void RuleExceptionCanSkipProfanityMatchingForOneIdentifier()
    {
        var options = new Options();
        options.AllowIdentifierForRule("fuckboyenthusiast", Rule.Profanity);

        Assert.True(new Checker(options).Check("fuckboyenthusiast").IsClaimable);
    }

    [Fact]
    public void PatternExceptionsCanCoverMultiplePatternsForOneIdentifier()
    {
        var options = new Options();
        options.AllowIdentifierForPattern("8===3", Pattern.AsciiArt);
        options.AllowIdentifierForPattern("8===3", Pattern.Repeated);

        Assert.True(new Checker(options).Check("8===3").IsClaimable);
    }

    [Theory]
    [InlineData("123456", Pattern.NumericOnly, false)]
    [InlineData("!@#$", Pattern.SymbolOnly, false)]
    [InlineData("QZXVORN", Pattern.UppercaseOnly, true)]
    public void PatternExceptionSkipsOnlySelectedPatternKinds(
        string value,
        Pattern pattern,
        bool enableFirst)
    {
        var options = new Options();
        if (enableFirst)
        {
            options.EnablePattern(pattern);
        }

        options.AllowIdentifierForPattern(value, pattern);

        Assert.True(new Checker(options).Check(value).IsClaimable);
    }

    [Fact]
    public void RepeatedCharacterAllowancesNormalizeCaseAndMerge()
    {
        var options = new Options();
        options.AllowRepeatedCharacters("T");
        options.AllowRepeatedCharacters("t");

        Assert.Single(options.AllowedRepeatedCharacters);
        Assert.True(new Checker(options).Check("tttOrchid").IsClaimable);
    }

    [Fact]
    public void SeparateRuleAndPatternExceptionCallsMergeForSameIdentifier()
    {
        var options = new Options();
        options.AllowIdentifierForRule("x_orchid7", Rule.BlockedCharacters);
        options.AllowIdentifierForRule("x_orchid7", Rule.Numbers);
        options.EnableRule(Rule.Numbers);

        options.AllowIdentifierForPattern("ABABAB", Pattern.Repeated);
        options.EnablePattern(Pattern.UppercaseOnly);
        options.AllowIdentifierForPattern("ABABAB", Pattern.UppercaseOnly);

        Assert.True(new Checker(options).Check("x_orchid7").IsClaimable);
        Assert.True(new Checker(options).Check("ABABAB").IsClaimable);
    }

    [Fact]
    public void StartupPolicyConstructorValidatesAndAppliesAllowedCharacters()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Policy(null!, Array.Empty<string>()));
        Assert.Throws<ArgumentNullException>(() =>
            new Policy(Array.Empty<string>(), null!));

        var policy = new Policy(new[] { "_" }, new[] { "_" });

        Assert.False(policy.IsCharacterBlocked("_"));
        Assert.True(policy.IsCharacterExplicitlyAllowed("_"));
    }

    [Fact]
    public void ExceptionApisRejectInvalidConfiguration()
    {
        var options = new Options();

        Assert.Throws<ArgumentException>(() => options.AllowIdentifierForRule(" ", Rule.Numbers));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.AllowIdentifierForRule("value", Rule.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.AllowIdentifierForRule("value", (Rule)(1 << 30)));
        Assert.Throws<ArgumentException>(() => options.AllowIdentifierForPattern(" ", Pattern.Repeated));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.AllowIdentifierForPattern("value", Pattern.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.AllowIdentifierForPattern("value", (Pattern)(1 << 20)));
        Assert.Throws<ArgumentNullException>(() => options.AllowCharacters(null!));
        Assert.Throws<ArgumentNullException>(() => options.AllowRepeatedCharacters(null!));
        Assert.Throws<ArgumentException>(() => options.AllowCharacters("ab"));
        Assert.Throws<ArgumentException>(() => options.AllowRepeatedCharacters("ab"));
    }
}
