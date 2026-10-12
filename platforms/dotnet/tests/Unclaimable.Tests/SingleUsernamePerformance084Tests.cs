using System.Globalization;
using Xunit;

namespace Unclaimable.Tests;

public sealed class SingleUsernamePerformance084Tests
{
    [Fact]
    public void FullScanBenchmarkMissIsClaimableWithTenThousandCustomReservations()
    {
        var options = new Options();
        options.DisableRule(Rule.ObfuscationMatching);
        options.DisablePattern(Pattern.Repeated);
        for (var i = 0; i < 10000; i++)
            options.AdditionalReserved.Add("zzpartial" + i.ToString("D5", CultureInfo.InvariantCulture));

        var checker = new Checker(options);
        Assert.True(checker.Check("zznomatchingreservationhere").IsClaimable);

        var lateMatch = checker.Check("prefixzzpartial09999suffix");
        Assert.True(lateMatch.IsReserved);
        Assert.Equal("custom", lateMatch.Category);
        Assert.Equal("zzpartial09999", lateMatch.MatchedValue);
        Assert.Equal(MatchKind.Partial, lateMatch.MatchKind);
    }

    [Fact]
    public void RepresentativeUsernameBenchmarkCasesPreserveExpectedOutcomes()
    {
        var checker = new Checker();
        Assert.True(checker.Check("zqvxpioneer").IsClaimable);
        Assert.Equal("roles", checker.Check("admin").Category);
        Assert.Equal("profanity", checker.Check("saddick").Category);

        var options = new Options { ObfuscationSensitivity = ObfuscationSensitivity.High };
        options.DisablePattern(Pattern.Repeated);
        Assert.NotNull(new Checker(options).Check("admmmin"));
    }
}
