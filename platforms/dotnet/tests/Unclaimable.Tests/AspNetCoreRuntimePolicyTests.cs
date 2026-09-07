using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Unclaimable.Tests;

public sealed class AspNetCoreRuntimePolicyTests
{
    [Fact]
    public void DependencyInjectionExposesLiveRuntimePolicy()
    {
        using var provider = new ServiceCollection()
            .AddUnclaimable()
            .BuildServiceProvider();

        var checker = provider.GetRequiredService<IUnclaimableChecker>();
        var policy = provider.GetRequiredService<IUnclaimablePolicy>();

        Assert.True(checker.IsClaimable("normal^name"));

        policy.BlockCharacter("^");

        Assert.Equal(UnclaimableMatchKind.BlockedCharacter, checker.Check("normal^name").MatchKind);
    }

    [Fact]
    public void StartupBlockedCharactersSeedRuntimePolicy()
    {
        using var provider = new ServiceCollection()
            .AddUnclaimable(options => options.AdditionalBlockedCharacters("^", "$"))
            .BuildServiceProvider();

        var checker = provider.GetRequiredService<IUnclaimableChecker>();
        var policy = provider.GetRequiredService<IUnclaimablePolicy>();

        Assert.True(policy.IsCharacterBlocked("^"));
        Assert.Equal(UnclaimableMatchKind.BlockedCharacter, checker.Check("normal^name").MatchKind);
    }
}
