using Xunit;

namespace Unclaimable.Tests;

public sealed class BooleanApiParity084Tests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("zqvxpioneer")]
    [InlineData("admin")]
    [InlineData("N1ke")]
    [InlineData("1111")]
    [InlineData("abababab")]
    [InlineData("_admin_")]
    [InlineData("\u0430dmin")]
    public void BooleanApisMatchCheckForRepresentativeDefaultInputs(string? value)
    {
        var checker = new Checker();
        var result = checker.Check(value);

        Assert.Equal(result.IsReserved, checker.IsReserved(value));
        Assert.Equal(result.IsClaimable, checker.IsClaimable(value));
    }

    [Fact]
    public void BooleanApisMatchCheckWithCustomAndAllowedIdentifiers()
    {
        var options = new Options();
        options.AdditionalReserved.Add("zzcustomguard");
        options.AllowedIdentifiers.Add("admin");

        var checker = new Checker(options);
        var values = new[]
        {
            "admin",
            "zzcustomguard",
            "prefixzzcustomguardsuffix",
            "zqvxpioneer"
        };

        foreach (var value in values)
        {
            var result = checker.Check(value);
            Assert.Equal(result.IsReserved, checker.IsReserved(value));
            Assert.Equal(result.IsClaimable, checker.IsClaimable(value));
        }
    }

    [Fact]
    public void BooleanApisMatchCheckAtHighObfuscationSensitivity()
    {
        var options = new Options { ObfuscationSensitivity = ObfuscationSensitivity.High };
        options.DisablePattern(Pattern.Repeated);
        var checker = new Checker(options);

        foreach (var value in new[] { "admmmin", "admin", "N1ke" })
        {
            var result = checker.Check(value);
            Assert.Equal(result.IsReserved, checker.IsReserved(value));
            Assert.Equal(result.IsClaimable, checker.IsClaimable(value));
        }
    }
}
