using System;
using Unclaimable.Email;
using Xunit;

namespace Unclaimable.Tests;

public sealed class EmailRelease084FeatureTests
{
    [Fact]
    public void RegistrationPresetKeepsReservedFindingsWithoutBlocking()
    {
        var checker = new EmailChecker(EmailOptions.ForUserRegistration());
        var result = checker.CheckExistingAddress("support@example.com");
        Assert.True(result.IsAllowed);
        Assert.True(result.IsSyntaxValid);
        Assert.True(result.IsLocalPartReserved);
        Assert.True(result.IsIssuingDomainAllowed);
    }

    [Fact]
    public void OrganizationPresetRequiresAnExactDomain()
    {
        var checker = new EmailChecker(EmailOptions.ForOrganizationEmail("EXAMPLE.COM"));
        Assert.True(checker.CheckExistingAddress("bluegarden@example.com").IsAllowed);
        var rejected = checker.CheckExistingAddress("bluegarden@sub.example.com");
        Assert.False(rejected.IsAllowed);
        Assert.False(rejected.IsIssuingDomainAllowed);
        Assert.True(rejected.IsSyntaxValid);
    }

    [Fact]
    public void IssuedAddressPresetIsStrict()
    {
        Assert.Equal(EmailFailureKind.ReservedLocalPart,
            new EmailChecker(EmailOptions.ForIssuedAddresses())
                .CheckNewAddress("admin@example.com").FailureKind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void OrganizationPresetRejectsMissingDomain(string? domain)
    {
        Assert.Throws<ArgumentException>(() => EmailOptions.ForOrganizationEmail(domain!));
    }

    [Fact]
    public void InputLimitIsOptInAndEnforcedBeforeParsing()
    {
        var options = new EmailOptions { MaximumInputLength = 20 };
        var checker = new EmailChecker(options);
        Assert.Equal(EmailFailureKind.InvalidFormat,
            checker.CheckExistingAddress("bluegarden@example.com").FailureKind);
        Assert.False(checker.CheckExistingAddress("bluegarden@example.com").IsSyntaxValid);
        Assert.True(new EmailChecker().CheckExistingAddress("bluegarden@example.com").IsAllowed);
    }

    [Fact]
    public void InputLimitMustBePositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EmailChecker(new EmailOptions { MaximumInputLength = 0 }));
    }

    [Theory]
    [InlineData("BLUEGARDEN@EXAMPLE.COM")]
    [InlineData("bluegarden@ExAmPlE.cOm")]
    public void MixedCaseDomainNormalizesToAscii(string address)
    {
        var result = new EmailChecker().CheckExistingAddress(address);
        Assert.Equal("example.com", result.Domain);
        Assert.True(result.IsSyntaxValid);
    }

    [Fact]
    public void PunycodeAndUnicodeProtectedDomainsHaveEquivalentIdentity()
    {
        var options = new EmailOptions();
        options.ProtectedDomains.Add("bücher.de");
        var checker = new EmailChecker(options);
        Assert.True(checker.CheckExistingAddress("bluegarden@xn--bcher-kva.de").IsAllowed);
        Assert.True(checker.CheckExistingAddress("bluegarden@bücher.de").IsAllowed);
    }

    [Fact]
    public void TrailingDotIsNotSilentlyAcceptedAsAnAlternateMailboxDomain()
    {
        var result = new EmailChecker().CheckExistingAddress("bluegarden@example.com.");
        Assert.False(result.IsAllowed);
    }
}
