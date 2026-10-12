using Unclaimable.Extended;
using Xunit;

namespace Unclaimable.Tests;

public sealed class ReleaseGate084Tests
{
    [Fact]
    public void NewExtendedGroupsAreEnabledUntilExplicitlyDisabled()
    {
        var options = new ExtendedOptions();
        Assert.Empty(options.DisabledCategories);
        options.DisableCategory(ExtendedCategory.CryptoWallets);
        Assert.Contains(ExtendedCategory.CryptoWallets, options.DisabledCategories);
        options.EnableCategory(ExtendedCategory.CryptoWallets);
        Assert.DoesNotContain(ExtendedCategory.CryptoWallets, options.DisabledCategories);
    }

    [Fact]
    public void CategorizedReservationRejectsMissingIdentifier()
    {
        Assert.Throws<ArgumentException>(() => new Options().Reserve(" ", "custom"));
    }

    [Fact]
    public void SupplementaryDecimalDigitsRemainNumericOnly()
    {
        // Osmanya decimal digits are represented by surrogate pairs in UTF-16.
        Assert.Equal(MatchKind.NumericOnly, new Checker().Check("\U000104A0\U000104A1\U000104A2").MatchKind);
    }

    [Fact]
    public void SupplementaryUppercaseLettersRemainUppercaseOnly()
    {
        // Deseret capital letters are represented by surrogate pairs in UTF-16.
        var options = new Options().EnablePattern(Pattern.UppercaseOnly);
        Assert.Equal(MatchKind.UppercaseOnly, new Checker(options).Check("\U00010400\U00010401").MatchKind);
    }

    [Fact]
    public void CheckerRejectsMissingRuntimePolicy()
    {
        Assert.Throws<ArgumentNullException>(() => new Checker(new Options(), null!));
    }
}
