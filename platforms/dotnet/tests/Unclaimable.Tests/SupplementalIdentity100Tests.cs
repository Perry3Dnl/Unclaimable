using System;
using System.IO;
using System.Linq;
using Unclaimable.Extended;
using Xunit;

namespace Unclaimable.Tests;

public sealed class SupplementalIdentity100Tests
{
    [Fact]
    public void DataIsNotEnabledByDefault()
    {
        var options = new Options();
        Assert.Empty(options.AdditionalReserved);
        Assert.True(new Checker(options).IsClaimable("bluegarden"));
        Assert.True(new Checker(ValidationPresets.Username()).IsClaimable("bluegarden"));
    }

    [Fact]
    public void RegistrationUsesCoreAndKeepsOrdinaryChecksUnchanged()
    {
        var options = new Options();
        options.UseSupplementalIdentityData();
        var checker = new Checker(options);

        var result = checker.Check("Bitfinex");
        Assert.True(result.IsReserved);
        Assert.Equal("identity-crypto-exchanges", result.Category);
        Assert.Equal(MatchKind.Exact, result.MatchKind);
        Assert.Equal(new Checker().IsClaimable("bluegarden"), checker.IsClaimable("bluegarden"));
    }

    [Fact]
    public void HighCollisionGenericTermsAreNotInSupplementalSnapshot()
    {
        var assembly = typeof(SupplementalIdentityOptionsExtensions).Assembly;
        using var stream = assembly.GetManifestResourceStream("Unclaimable.Extended.Vocabulary.identity.json");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        var snapshot = reader.ReadToEnd();
        Assert.DoesNotContain("\"Safe\"", snapshot);
        Assert.DoesNotContain("\"Flow\"", snapshot);
        Assert.DoesNotContain("\"Gala\"", snapshot);
        Assert.DoesNotContain("\"Rain\"", snapshot);
    }

    [Fact]
    public void OptInRegistrationHandlesNullAndSeparateOptions()
    {
        Assert.Throws<ArgumentNullException>(() => SupplementalIdentityOptionsExtensions.UseSupplementalIdentityData(null!));
        var defaultOptions = new Options();
        var optedIn = new Options().UseSupplementalIdentityData();
        Assert.True(new Checker(defaultOptions).IsClaimable("bluegarden"));
        Assert.True(new Checker(optedIn).IsClaimable("bluegarden"));
        Assert.Equal(3, defaultOptions.MinimumLength);
        Assert.Equal(3, optedIn.MinimumLength);
    }

    [Fact]
    public void ExplicitRegistrationDoesNotChangeExistingExtendedMethod()
    {
        var original = new Options().UseExtendedData();
        var extra = new Options().UseExtendedData().UseSupplementalIdentityData();
        var originalChecker = new Checker(original);
        var extraChecker = new Checker(extra);
        Assert.Equal(originalChecker.IsClaimable("bluegarden"), extraChecker.IsClaimable("bluegarden"));
    }
}
