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
        Assert.Throws<ArgumentException>(() => options.AllowCharacters("ab"));
        Assert.Throws<ArgumentException>(() => options.AllowRepeatedCharacters("ab"));
    }
}
