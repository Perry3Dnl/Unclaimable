using System;
using System.Reflection;
using Unclaimable;
using Unclaimable.Email;
using Xunit;

namespace Unclaimable.Tests;

public sealed class AdversarialIntegration084Tests
{
    [Fact]
    public void CoreOptionalNullIsNotARequiredRegistrationValue()
    {
        var checker = new Checker();
        Assert.True(checker.IsClaimable(null));
        string? absent = null;
        Assert.False(!string.IsNullOrWhiteSpace(absent) && checker.IsClaimable(absent));
    }

    [Theory]
    [InlineData("www.ck", "www")]
    [InlineData("alpha.www.ck", "www")]
    [InlineData("alpha.beta.ck", "alpha")]
    [InlineData("city.kawasaki.jp", "city")]
    [InlineData("alpha.city.kawasaki.jp", "city")]
    [InlineData("example.co.za", "example")]
    [InlineData("example.blogspot.com", "example")]
    [InlineData("example.unknownsuffixzz", "example")]
    public void SuffixResolverUsesPinnedWildcardExceptionAndPrivateRules(string domain, string expected)
    {
        var type = typeof(EmailChecker).Assembly.GetType("Unclaimable.Email.PublicSuffixResolver", throwOnError: true)!;
        var method = type.GetMethod("GetRegistrantLabel", BindingFlags.Static | BindingFlags.NonPublic)!;
        var label = (string)method.Invoke(null, new object[] { domain })!;
        Assert.Equal(expected, label);
    }

    [Fact]
    public void SuffixResolverHandlesSingleLabelWithoutAnIndexError()
    {
        var type = typeof(EmailChecker).Assembly.GetType("Unclaimable.Email.PublicSuffixResolver", throwOnError: true)!;
        var method = type.GetMethod("GetRegistrantLabel", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.Equal("localhost", (string)method.Invoke(null, new object[] { "localhost" })!);
    }

    [Fact]
    public void LongInvalidMailboxIsRejectedBeforeProtectedMatching()
    {
        var options = new EmailOptions();
        options.ProtectedDomains.Add("example.co.za");
        var checker = new EmailChecker(options);
        var input = new string('x', 100_000) + "@example.co.za";
        for (var i = 0; i < 20; i++)
        {
            var result = checker.CheckExistingAddress(input);
            Assert.False(result.IsAllowed);
            Assert.Equal(EmailFailureKind.InvalidLocalPart, result.FailureKind);
        }
    }

    [Fact]
    public void LargeCustomReservationsDoNotChangeExplicitExactDecisions()
    {
        var options = new Options();
        for (var i = 0; i < 1000; i++)
            options.AdditionalReserved.Add("zzcustom" + i.ToString("D5"));
        var checker = new Checker(options);
        Assert.True(checker.IsReserved("zzcustom00999"));
        Assert.True(checker.IsClaimable("ordinarybluegarden"));
    }

    [Theory]
    [InlineData("málaga")]
    [InlineData("Łukasz")]
    [InlineData("Αλέξανδρος")]
    [InlineData("山田太郎")]
    public void InternationalNamesProduceStableDiagnostics(string value)
    {
        var checker = new Checker();
        var first = checker.Check(value);
        var second = checker.Check(value);
        Assert.Equal(first.IsClaimable, second.IsClaimable);
        Assert.Equal(first.MatchKind, second.MatchKind);
    }
}
