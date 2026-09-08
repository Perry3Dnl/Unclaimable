using Xunit;

namespace Unclaimable.Tests;

public sealed class RuntimePolicyTests
{
    [Fact]
    public void RuntimePolicyCanRelaxBuiltInBlockedCharacters()
    {
        var options = new Options
        {
            Strictness = Strictness.Standard
        };
        var policy = new Policy(options.ConfiguredBlockedCharacters);
        var checker = new Checker(options, policy);

        Assert.Equal(MatchKind.BlockedCharacter, checker.Check("john-doe").MatchKind);

        policy.AllowCharacter("-");

        Assert.True(checker.IsClaimable("john-doe"));
    }

    [Fact]
    public void RuntimePolicyCanBlockAdditionalCharactersWithoutRebuildingChecker()
    {
        var options = new Options
        {
            Strictness = Strictness.Standard
        };
        var policy = new Policy(options.ConfiguredBlockedCharacters);
        var checker = new Checker(options, policy);

        Assert.True(checker.IsClaimable("john^doe"));

        policy.BlockCharacter("^");

        var result = checker.Check("john^doe");
        Assert.Equal(MatchKind.BlockedCharacter, result.MatchKind);
        Assert.Equal("^", result.OffendingCharacter);
    }

    [Fact]
    public void RuntimePolicyOperationsAreImmediatelyVisible()
    {
        var policy = new Policy();

        policy.BlockCharacters("^", "$", "@");
        Assert.Contains("^", policy.BlockedCharacters);
        Assert.True(policy.IsCharacterBlocked("$"));

        policy.AllowCharacters("^", "$", "@");
        Assert.False(policy.IsCharacterBlocked("^"));
        Assert.DoesNotContain("$", policy.BlockedCharacters);
    }
}
