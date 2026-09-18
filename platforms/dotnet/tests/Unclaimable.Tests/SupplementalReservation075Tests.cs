using Xunit;

namespace Unclaimable.Tests;

public sealed class SupplementalReservation075Tests
{
    [Fact]
    public void CategorizedReservationPreservesDiagnosticCategory()
    {
        var options = CreateRelaxedOptions();
        options.Reserve("example identity", "externaltest", ReservedMatchMode.WholeIdentifier);

        var result = new Checker(options).Check("example identity");

        Assert.True(result.IsReserved);
        Assert.Equal("externaltest", result.Category);
        Assert.Equal(MatchKind.Exact, result.MatchKind);
    }

    [Fact]
    public void WholeIdentifierUsesNormalIdentityTransformsWithoutPartialMatching()
    {
        var options = CreateRelaxedOptions();
        options.Reserve("example identity", "externaltest", ReservedMatchMode.WholeIdentifier);
        var checker = new Checker(options);

        Assert.True(checker.Check("example-identity").IsReserved);

        var obfuscated = checker.Check("3xampleidentity");
        Assert.True(obfuscated.IsReserved);
        Assert.Equal(MatchKind.Obfuscated, obfuscated.MatchKind);

        Assert.True(checker.IsClaimable("myexampleidentityfan"));
    }

    [Fact]
    public void ExistingReservationModesKeepTheirEstablishedSemantics()
    {
        Assert.Equal(0, (int)ReservedMatchMode.Default);
        Assert.Equal(1, (int)ReservedMatchMode.Exact);
        Assert.Equal(2, (int)ReservedMatchMode.WholeIdentifier);

        var exactOptions = CreateRelaxedOptions();
        exactOptions.Reserve("acme", ReservedMatchMode.Exact);
        var exact = new Checker(exactOptions);
        Assert.True(exact.IsReserved("ACME"));
        Assert.True(exact.IsClaimable("acme-fan"));

        var defaultOptions = CreateRelaxedOptions();
        defaultOptions.Reserve("acme", ReservedMatchMode.Default);
        var defaultChecker = new Checker(defaultOptions);
        Assert.True(defaultChecker.IsReserved("myacmefan"));
    }

    [Fact]
    public void CategorizedReservationValidatesInputs()
    {
        var options = new Options();

        Assert.Throws<ArgumentException>(
            () => options.Reserve("", "category", ReservedMatchMode.WholeIdentifier));
        Assert.Throws<ArgumentException>(
            () => options.Reserve("value", "", ReservedMatchMode.WholeIdentifier));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => options.Reserve("value", "category", (ReservedMatchMode)999));
    }

    private static Options CreateRelaxedOptions()
    {
        var options = new Options
        {
            MinimumLength = 0,
            MaximumLength = 128
        };

        options.DisableRule(
            Rule.Whitespace
            | Rule.BlockedCharacters
            | Rule.LeadingSeparator
            | Rule.TrailingSeparator);
        options.DisablePattern(
            Pattern.NumericOnly
            | Pattern.Repeated
            | Pattern.SymbolOnly
            | Pattern.AsciiArt
            | Pattern.UppercaseOnly);

        return options;
    }
}
