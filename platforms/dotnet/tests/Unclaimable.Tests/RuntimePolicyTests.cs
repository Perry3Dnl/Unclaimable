using Xunit;

namespace Unclaimable.Tests;

public sealed class RuntimePolicyTests
{
    [Fact]
    public void RuntimePolicyCanRelaxBuiltInBlockedCharacters()
    {
        var options = new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Standard
        };
        var policy = new UnclaimablePolicy(options.ConfiguredBlockedCharacters);
        var checker = new UnclaimableChecker(options, policy);

        Assert.Equal(UnclaimableMatchKind.BlockedCharacter, checker.Check("john-doe").MatchKind);

        policy.AllowCharacter("-");

        Assert.True(checker.IsClaimable("john-doe"));
    }

    [Fact]
    public void RuntimePolicyCanBlockAdditionalCharactersWithoutRebuildingChecker()
    {
        var options = new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Standard
        };
        var policy = new UnclaimablePolicy(options.ConfiguredBlockedCharacters);
        var checker = new UnclaimableChecker(options, policy);

        Assert.True(checker.IsClaimable("john^doe"));

        policy.BlockCharacter("^");

        var result = checker.Check("john^doe");
        Assert.Equal(UnclaimableMatchKind.BlockedCharacter, result.MatchKind);
        Assert.Equal("^", result.OffendingCharacter);
    }

    [Fact]
    public void RuntimePolicyOperationsAreImmediatelyVisible()
    {
        var policy = new UnclaimablePolicy();

        policy.BlockCharacters("^", "$", "@");
        Assert.Contains("^", policy.BlockedCharacters);
        Assert.True(policy.IsCharacterBlocked("$"));

        policy.AllowCharacters("^", "$", "@");
        Assert.False(policy.IsCharacterBlocked("^"));
        Assert.DoesNotContain("$", policy.BlockedCharacters);
    }
}
