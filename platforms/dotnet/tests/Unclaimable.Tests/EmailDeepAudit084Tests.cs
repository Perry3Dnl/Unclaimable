using System;
using Unclaimable.Email;
using Xunit;

namespace Unclaimable.Tests;

public sealed class EmailDeepAudit084Tests
{
    [Theory]
    [InlineData(EmailUsage.Auto, EmailAddressPurpose.NewAddress, EmailAddressPurpose.NewAddress)]
    [InlineData(EmailUsage.Auto, EmailAddressPurpose.ExistingAddress, EmailAddressPurpose.ExistingAddress)]
    [InlineData(EmailUsage.ExistingAddress, EmailAddressPurpose.NewAddress, EmailAddressPurpose.ExistingAddress)]
    [InlineData(EmailUsage.IssuedAddress, EmailAddressPurpose.ExistingAddress, EmailAddressPurpose.NewAddress)]
    public void ReportsRequestedAndEffectiveEmailPurpose(
        EmailUsage usage, EmailAddressPurpose requested, EmailAddressPurpose effective)
    {
        var options = new EmailOptions { EmailUsage = usage };
        var result = new EmailChecker(options).Check("bluegarden@example.com", requested);
        Assert.Equal(requested, result.Purpose);
        Assert.Equal(effective, result.EffectivePurpose);
    }

    [Fact]
    public void EarlyInvalidInputRetainsRequestedPurposeInDiagnostics()
    {
        var checker = new EmailChecker(new EmailOptions
        {
            EmailUsage = EmailUsage.IssuedAddress,
            MaximumInputLength = 2
        });
        var result = checker.CheckExistingAddress("bluegarden@example.com");
        Assert.False(result.IsAllowed);
        Assert.Equal(EmailAddressPurpose.ExistingAddress, result.Purpose);
        Assert.Equal(EmailAddressPurpose.NewAddress, result.EffectivePurpose);
    }

    [Theory]
    [InlineData("a@example.com")]
    [InlineData("a@EXAMPLE.COM")]
    [InlineData("a@sub.example.com")]
    public void ValidConfiguredDomainDoesNotMisclassifyNormalMailboxes(string address)
    {
        var options = new EmailOptions();
        options.ProtectedDomains.Add("example.com");
        var result = new EmailChecker(options).CheckExistingAddress(address);
        Assert.True(result.IsAllowed);
        Assert.False(result.IsSuspiciousDomain);
    }

    [Fact]
    public void TrustedDomainCannotHideAnotherProtectedDomainEmbeddedInHostname()
    {
        var options = new EmailOptions();
        options.ProtectedDomains.Add("example.com");
        options.ProtectedDomains.Add("google.com");
        var checker = new EmailChecker(options);
        var result = checker.CheckExistingAddress("bluegarden@google.com.example.com");
        Assert.Equal(EmailFailureKind.SuspiciousDomain, result.FailureKind);
        Assert.Equal(DomainLookalikeKind.EmbeddedProtectedDomain, result.DomainLookalikeKind);
        Assert.Equal("google.com", result.MatchedProtectedDomain);
    }

    [Fact]
    public void ActualProtectedSubdomainRemainsAllowed()
    {
        var options = new EmailOptions();
        options.ProtectedDomains.Add("example.com");
        options.ProtectedDomains.Add("google.com");
        var result = new EmailChecker(options).CheckExistingAddress("bluegarden@mail.example.com");
        Assert.True(result.IsAllowed);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void RejectsUnsupportedEditDistances(int editDistance)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EmailChecker(new EmailOptions { MaximumDomainEditDistance = editDistance }));
    }

    [Fact]
    public void RejectsUnknownEmailPurposeWithoutCrashing()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EmailChecker().Check("bluegarden@example.com", (EmailAddressPurpose)98));
    }

    [Fact]
    public void RejectsNullLocalPartCheckerOrOptionsExplicitly()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailChecker((EmailOptions)null!));
        Assert.Throws<ArgumentNullException>(() =>
            new EmailChecker((Unclaimable.IChecker)null!, new EmailOptions()));
        Assert.Throws<ArgumentNullException>(() =>
            new EmailChecker(new Unclaimable.Checker(), null!));
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("a..example.com")]
    [InlineData(".example.com")]
    [InlineData("example.com.")]
    [InlineData("exa_mple.com")]
    public void InvalidConfiguredDomainFailsWithArgumentError(string configured)
    {
        var options = new EmailOptions();
        options.IssuingDomains.Add(configured);
        Assert.Throws<ArgumentException>(() => new EmailChecker(options));
    }

    [Fact]
    public void InvalidProtectedDomainAlsoFailsWithArgumentError()
    {
        var options = new EmailOptions();
        options.ProtectedDomains.Add("invalid..example.com");
        Assert.Throws<ArgumentException>(() => new EmailChecker(options));
    }

    [Fact]
    public void RelaxedModeStillRejectsUnapprovedIssuingDomain()
    {
        var options = EmailOptions.ForUserRegistration();
        options.IssuingDomains.Add("example.com");
        options.EnforceIssuingDomainsForExistingAddresses = true;
        options.AllowIssuingDomainSubdomains = false;
        var result = new EmailChecker(options).CheckExistingAddress("admin@other.example.net");
        Assert.False(result.IsAllowed);
        Assert.False(result.IsIssuingDomainAllowed);
        Assert.True(result.IsLocalPartReserved);
    }
}
