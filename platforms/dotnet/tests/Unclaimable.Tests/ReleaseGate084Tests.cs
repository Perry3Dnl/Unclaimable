using Unclaimable.Extended;
using Xunit;

namespace Unclaimable.Tests;

public sealed class ReleaseGate084Tests
{
    [Fact]
    public void NewExtendedGroupsAreVisibleAsDisabledUntilEnabled()
    {
        var options = new ExtendedOptions();
        Assert.Contains(ExtendedCategory.CryptoWallets, options.DisabledCategories);
        Assert.Contains(ExtendedCategory.RegionalRolesNetherlands, options.DisabledCategories);

        options.EnableCategory(ExtendedCategory.CryptoWallets);
        Assert.DoesNotContain(ExtendedCategory.CryptoWallets, options.DisabledCategories);
    }

    [Fact]
    public void CategorizedReservationRejectsMissingIdentifier()
    {
        Assert.Throws<ArgumentException>(() => new Options().Reserve(" ", "custom"));
    }

    [Fact]
    public void CheckerRejectsMissingRuntimePolicy()
    {
        Assert.Throws<ArgumentNullException>(() => new Checker(new Options(), null!));
    }
}
