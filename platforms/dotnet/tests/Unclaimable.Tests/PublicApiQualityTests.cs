using Xunit;

namespace Unclaimable.Tests;

public sealed class PublicApiQualityTests
{
    [Fact]
    public void BlockCharactersDoesNotPartiallyApplyWhenValidationFails()
    {
        var policy = new UnclaimablePolicy();

        Assert.Throws<ArgumentException>(() => policy.BlockCharacters("^", string.Empty));

        Assert.False(policy.IsCharacterBlocked("^"));
    }

    [Fact]
    public void AllowCharactersDoesNotPartiallyApplyWhenValidationFails()
    {
        var policy = new UnclaimablePolicy();
        policy.BlockCharacters("^", "$");

        Assert.Throws<ArgumentException>(() => policy.AllowCharacters("^", string.Empty));

        Assert.True(policy.IsCharacterBlocked("^"));
        Assert.True(policy.IsCharacterBlocked("$"));
    }

    [Fact]
    public void AdditionalBlockedCharactersDoesNotPartiallyApplyWhenValidationFails()
    {
        var options = new UnclaimableOptions();

        Assert.Throws<ArgumentException>(() => options.AdditionalBlockedCharacters("^", string.Empty));

        Assert.DoesNotContain("^", options.ConfiguredBlockedCharacters);
    }

    [Fact]
    public void DetailedResultSnapshotsDiagnostics()
    {
        var diagnostics = new List<UnclaimableDiagnostic>
        {
            new UnclaimableDiagnostic(UnclaimableMatchKind.TooShort)
        };

        var result = new UnclaimableDetailedResult("ab", diagnostics);
        diagnostics.Clear();

        Assert.Single(result.Diagnostics);
        Assert.True(result.IsReserved);
    }
}
