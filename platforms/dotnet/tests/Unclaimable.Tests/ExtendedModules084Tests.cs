using Unclaimable.Extended;
using Xunit;

namespace Unclaimable.Tests;

public sealed class ExtendedModules084Tests
{
    [Theory]
    [InlineData(ExtendedCategory.RegionalRolesNetherlands, "commissarisvandekoning", "regionalrolesnetherlands")]
    [InlineData(ExtendedCategory.RegionalRolesUnitedStates, "countyexecutive", "regionalrolesunitedstates")]
    [InlineData(ExtendedCategory.CryptoWallets, "rabbywallet", "cryptowallets")]
    [InlineData(ExtendedCategory.CryptoExchanges, "coinbaseexchange", "cryptoexchanges")]
    [InlineData(ExtendedCategory.CryptoProtocols, "curvefinance", "cryptoprotocols")]
    [InlineData(ExtendedCategory.CryptoTokens, "eurc", "cryptotokens")]
    public void NewModulesRequireExplicitSelection(
        ExtendedCategory category, string identifier, string categoryName)
    {
        var existingGroups = new Options();
        existingGroups.UseExtendedData();
        Assert.True(new Checker(existingGroups).IsClaimable(identifier));

        var selected = new Options();
        selected.UseExtendedData(extended => extended.EnableCategory(category));
        var result = new Checker(selected).Check(identifier);
        Assert.True(result.IsReserved);
        Assert.Equal(categoryName, result.Category);
        Assert.True(ExtendedData.GetCount(category) >= 5);
    }

    [Fact]
    public void ModulesAreIndependentAndCanUseExactExceptions()
    {
        var options = new Options();
        options.UseExtendedData(extended =>
        {
            extended.EnableCategory(ExtendedCategory.CryptoWallets);
            extended.EnableCategory(ExtendedCategory.RegionalRolesNetherlands);
            extended.AllowedIdentifiers.Add("Rabby Wallet");
        });
        var checker = new Checker(options);

        Assert.True(checker.IsClaimable("rabbywallet"));
        Assert.Equal("cryptowallets", checker.Check("braavos").Category);
        Assert.Equal("regionalrolesnetherlands", checker.Check("commissarisvandekoning").Category);
        Assert.True(checker.IsClaimable("countyexecutive"));
    }
}
