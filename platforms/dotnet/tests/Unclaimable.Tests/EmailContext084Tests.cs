using Unclaimable.Email;
using Xunit;

namespace Unclaimable.Tests;

public sealed class EmailContext084Tests
{
    [Fact]
    public void DefaultsKeepStrictExistingAddressBehavior()
    {
        var result = new EmailChecker().CheckExistingAddress("admin@example.com");
        Assert.False(result.IsAllowed);
        Assert.Equal(EmailFailureKind.ReservedLocalPart, result.FailureKind);
    }

    [Fact]
    public void RelaxedExistingAddressesKeepReservedLocalPartDiagnostics()
    {
        var options = new EmailOptions
        {
            EmailUsage = EmailUsage.ExistingAddress,
            EmailProtectionLevel = EmailProtectionLevel.Relaxed
        };
        var result = new EmailChecker(options).CheckExistingAddress("admin@example.com");
        Assert.True(result.IsAllowed);
        Assert.True(result.LocalPartResult!.IsReserved);
        Assert.Equal(EmailFailureKind.None, result.FailureKind);
    }

    [Fact]
    public void RelaxedExistingAddressesKeepProtectedDomainDiagnostics()
    {
        var options = new EmailOptions { EmailProtectionLevel = EmailProtectionLevel.Relaxed };
        options.ProtectedDomains.Add("lidl.nl");
        var result = new EmailChecker(options).CheckExistingAddress("bluegarden@lidi.nl");
        Assert.True(result.IsAllowed);
        Assert.True(result.IsSuspiciousDomain);
        Assert.Equal("lidl.nl", result.MatchedProtectedDomain);
    }

    [Fact]
    public void RelaxedNeverDisablesSyntaxValidation()
    {
        var checker = new EmailChecker(new EmailOptions { EmailProtectionLevel = EmailProtectionLevel.Relaxed });
        Assert.False(checker.CheckExistingAddress("not-an-email").IsAllowed);
    }

    [Fact]
    public void RelaxedNeverDisablesExplicitIssuingDomainAllowlist()
    {
        var options = new EmailOptions
        {
            EmailProtectionLevel = EmailProtectionLevel.Relaxed,
            EnforceIssuingDomainsForExistingAddresses = true
        };
        options.IssuingDomains.Add("example.com");
        Assert.Equal(EmailFailureKind.UnapprovedIssuingDomain,
            new EmailChecker(options).CheckExistingAddress("bluegarden@elsewhere.net").FailureKind);
    }

    [Fact]
    public void IssuedAddressModeRetainsStrictProtectionEvenWhenRelaxed()
    {
        var options = new EmailOptions
        {
            EmailUsage = EmailUsage.IssuedAddress,
            EmailProtectionLevel = EmailProtectionLevel.Relaxed
        };
        var result = new EmailChecker(options).CheckExistingAddress("admin@example.com");
        Assert.Equal(EmailFailureKind.ReservedLocalPart, result.FailureKind);
    }

    [Fact]
    public void ExistingAddressModeOverridesPerCallPurpose()
    {
        var options = new EmailOptions
        {
            EmailUsage = EmailUsage.ExistingAddress,
            EmailProtectionLevel = EmailProtectionLevel.Relaxed
        };
        var result = new EmailChecker(options).CheckNewAddress("admin@example.com");
        Assert.True(result.IsAllowed);
    }

    [Theory]
    [InlineData("example.co.za", "examp1e.co.za")]
    [InlineData("example.co.uk", "examp1e.co.uk")]
    [InlineData("example.com.au", "examp1e.com.au")]
    [InlineData("example.co.nz", "examp1e.co.nz")]
    [InlineData("example.com.br", "examp1e.com.br")]
    public void MultilabelSuffixUsesCorrectRegistrantLabel(string protectedDomain, string lookalikeDomain)
    {
        var options = new EmailOptions();
        options.ProtectedDomains.Add(protectedDomain);
        var result = new EmailChecker(options).CheckExistingAddress("bluegarden@" + lookalikeDomain);
        Assert.False(result.IsAllowed);
        Assert.Equal(DomainLookalikeKind.Confusable, result.DomainLookalikeKind);
    }

    [Fact]
    public void UnsupportedEnumValuesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EmailChecker(new EmailOptions { EmailUsage = (EmailUsage)99 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EmailChecker(new EmailOptions { EmailProtectionLevel = (EmailProtectionLevel)99 }));
    }
}
