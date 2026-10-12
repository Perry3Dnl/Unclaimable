using Unclaimable.Email;
using Xunit;

namespace Unclaimable.Tests;

public sealed class EmailIssuingDomainPolicy084Tests
{
    [Theory]
    [InlineData("bluegarden@google.com", true)]
    [InlineData("bluegarden@gmail.com", false)]
    [InlineData("bluegarden@microsoft.com", false)]
    [InlineData("bluegarden@sub.google.com", false)]
    [InlineData("bluegarden@google.com.attacker.com", false)]
    public void ExactIssuingDomainForExistingAddress(string address, bool expectedAllowed)
    {
        var options = new EmailOptions
        {
            AllowIssuingDomainSubdomains = false,
            EnforceIssuingDomainsForExistingAddresses = true
        };
        options.IssuingDomains.Add("google.com");

        var result = new EmailChecker(options).CheckExistingAddress(address);

        Assert.Equal(expectedAllowed, result.IsAllowed);
        if (!expectedAllowed && result.DomainLookalikeKind == DomainLookalikeKind.None)
        {
            Assert.Equal(EmailFailureKind.UnapprovedIssuingDomain, result.FailureKind);
        }
    }

    [Theory]
    [InlineData("bluegarden@google.com", true)]
    [InlineData("bluegarden@sub.google.com", true)]
    [InlineData("bluegarden@deep.sub.google.com", true)]
    [InlineData("bluegarden@fakegoogle.com", false)]
    public void LegacyDefaultsPermitRealSubdomainsForIssuedAddresses(string address, bool expectedAllowed)
    {
        var options = new EmailOptions();
        options.IssuingDomains.Add("google.com");

        Assert.Equal(expectedAllowed, new EmailChecker(options).CheckNewAddress(address).IsAllowed);
    }

    [Fact]
    public void ExistingAddressDoesNotEnforceIssuingDomainByDefault()
    {
        var options = new EmailOptions();
        options.IssuingDomains.Add("google.com");

        Assert.True(new EmailChecker(options).CheckExistingAddress("bluegarden@example.net").IsAllowed);
    }

    [Fact]
    public void ExistingAddressCanEnforceIssuingDomainAndPermitSubdomains()
    {
        var options = new EmailOptions
        {
            EnforceIssuingDomainsForExistingAddresses = true
        };
        options.IssuingDomains.Add("google.com");

        Assert.True(new EmailChecker(options).CheckExistingAddress("bluegarden@sub.google.com").IsAllowed);
        Assert.Equal(EmailFailureKind.UnapprovedIssuingDomain,
            new EmailChecker(options).CheckExistingAddress("bluegarden@example.net").FailureKind);
    }

    [Fact]
    public void IssuedAddressCanRequireExactDomain()
    {
        var options = new EmailOptions { AllowIssuingDomainSubdomains = false };
        options.IssuingDomains.Add("google.com");

        var checker = new EmailChecker(options);
        Assert.True(checker.CheckNewAddress("bluegarden@google.com").IsAllowed);
        Assert.Equal(EmailFailureKind.UnapprovedIssuingDomain,
            checker.CheckNewAddress("bluegarden@sub.google.com").FailureKind);
    }

    [Fact]
    public void DomainPolicyDoesNotDisableLocalPartProtection()
    {
        var options = new EmailOptions
        {
            AllowIssuingDomainSubdomains = false,
            EnforceIssuingDomainsForExistingAddresses = true
        };
        options.IssuingDomains.Add("google.com");

        Assert.Equal(EmailFailureKind.ReservedLocalPart,
            new EmailChecker(options).CheckExistingAddress("admin@google.com").FailureKind);
    }
}
