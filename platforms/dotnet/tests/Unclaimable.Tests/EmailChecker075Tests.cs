using Unclaimable.Email;
using Xunit;

namespace Unclaimable.Tests;

public sealed class EmailChecker075Tests
{
    [Theory]
    [InlineData("bluegarden@example.com")]
    [InlineData("blue.garden+tag@example.com")]
    [InlineData("blue_garden@example.com")]
    [InlineData("blue-garden@example.com")]
    [InlineData("q@example.com")]
    [InlineData("12345@example.com")]
    public void ExistingAddressesAllowOrdinaryEmailLocalParts(string address)
    {
        var result = new EmailChecker().CheckExistingAddress(address);

        Assert.True(result.IsAllowed);
        Assert.Equal(EmailFailureKind.None, result.FailureKind);
        Assert.NotNull(result.LocalPartResult);
        Assert.True(result.LocalPartResult!.IsClaimable);
    }

    [Fact]
    public void ExistingAddressStillAppliesUnclaimableToLocalPart()
    {
        var result = new EmailChecker().CheckExistingAddress("admin@example.com");

        Assert.False(result.IsAllowed);
        Assert.Equal(EmailFailureKind.ReservedLocalPart, result.FailureKind);
        Assert.NotNull(result.LocalPartResult);
        Assert.True(result.LocalPartResult!.IsReserved);
    }

    [Fact]
    public void ReservedLocalPartDoesNotHideSuspiciousDomainDiagnostic()
    {
        var result = CreateProtectedChecker("lidl.nl").CheckExistingAddress("admin@lidi.nl");

        Assert.Equal(EmailFailureKind.ReservedLocalPart, result.FailureKind);
        Assert.Equal(DomainLookalikeKind.Typographical, result.DomainLookalikeKind);
        Assert.Equal("lidl.nl", result.MatchedProtectedDomain);
    }

    [Theory]
    [InlineData("bluegarden@lidi.nl", DomainLookalikeKind.Typographical)]
    [InlineData("bluegarden@ldil.nl", DomainLookalikeKind.Typographical)]
    [InlineData("bluegarden@lid1.nl", DomainLookalikeKind.Confusable)]
    [InlineData("bluegarden@lidl.com", DomainLookalikeKind.ProtectedLabelReuse)]
    [InlineData("bluegarden@lidl-login.com", DomainLookalikeKind.ProtectedLabelReuse)]
    [InlineData("bluegarden@secure-lidl.com", DomainLookalikeKind.ProtectedLabelReuse)]
    [InlineData("bluegarden@lidl.nl.attacker.com", DomainLookalikeKind.EmbeddedProtectedDomain)]
    public void ProtectedDomainVariantsAreRejected(string address, DomainLookalikeKind expectedKind)
    {
        var result = CreateProtectedChecker("lidl.nl").CheckExistingAddress(address);

        Assert.False(result.IsAllowed);
        Assert.Equal(EmailFailureKind.SuspiciousDomain, result.FailureKind);
        Assert.Equal(expectedKind, result.DomainLookalikeKind);
        Assert.Equal("lidl.nl", result.MatchedProtectedDomain);
    }

    [Fact]
    public void UnicodeConfusableDomainIsRejected()
    {
        var result = CreateProtectedChecker("lidl.nl")
            .CheckExistingAddress("bluegarden@l\u0456dl.nl");

        Assert.Equal(EmailFailureKind.SuspiciousDomain, result.FailureKind);
        Assert.Equal(DomainLookalikeKind.Confusable, result.DomainLookalikeKind);
    }

    [Theory]
    [InlineData("bluegarden@lidl.nl")]
    [InlineData("bluegarden@mail.lidl.nl")]
    [InlineData("bluegarden@deep.mail.lidl.nl")]
    public void ExactProtectedDomainsAndTheirSubdomainsRemainAllowed(string address)
    {
        var result = CreateProtectedChecker("lidl.nl").CheckExistingAddress(address);

        Assert.True(result.IsAllowed);
        Assert.False(result.IsSuspiciousDomain);
    }

    [Fact]
    public void LookalikeChecksCanBeIndependentlyDisabled()
    {
        var options = new EmailOptions
        {
            DetectTypographicalLookalikes = false,
            DetectUnicodeLookalikes = false,
            DetectProtectedLabelReuse = false
        };
        options.ProtectedDomains.Add("lidl.nl");

        var checker = new EmailChecker(options);

        Assert.True(checker.CheckExistingAddress("bluegarden@lidi.nl").IsAllowed);
        Assert.True(checker.CheckExistingAddress("bluegarden@lid1.nl").IsAllowed);
        Assert.True(checker.CheckExistingAddress("bluegarden@lidl.com").IsAllowed);
    }

    [Fact]
    public void EmbeddedProtectedDomainProtectionCannotBeDisabledByLookalikeToggles()
    {
        var options = new EmailOptions
        {
            DetectTypographicalLookalikes = false,
            DetectUnicodeLookalikes = false,
            DetectProtectedLabelReuse = false
        };
        options.ProtectedDomains.Add("lidl.nl");

        var result = new EmailChecker(options)
            .CheckExistingAddress("bluegarden@lidl.nl.attacker.com");

        Assert.Equal(DomainLookalikeKind.EmbeddedProtectedDomain, result.DomainLookalikeKind);
    }

    [Fact]
    public void ZeroEditDistanceDisablesTypographicalLookalikes()
    {
        var options = new EmailOptions { MaximumDomainEditDistance = 0 };
        options.ProtectedDomains.Add("lidl.nl");

        Assert.True(new EmailChecker(options).CheckExistingAddress("bluegarden@lidi.nl").IsAllowed);
    }

    [Fact]
    public void MaximumEditDistanceMustStayBounded()
    {
        var options = new EmailOptions { MaximumDomainEditDistance = 3 };

        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailChecker(options));
    }

    [Theory]
    [InlineData(null, EmailFailureKind.InvalidFormat)]
    [InlineData("", EmailFailureKind.InvalidFormat)]
    [InlineData(" bluegarden@example.com", EmailFailureKind.InvalidFormat)]
    [InlineData("bluegarden@example.com ", EmailFailureKind.InvalidFormat)]
    [InlineData("bluegarden.example.com", EmailFailureKind.InvalidFormat)]
    [InlineData("blue@garden@example.com", EmailFailureKind.InvalidFormat)]
    [InlineData(".blue@example.com", EmailFailureKind.InvalidLocalPart)]
    [InlineData("blue.@example.com", EmailFailureKind.InvalidLocalPart)]
    [InlineData("blue..garden@example.com", EmailFailureKind.InvalidLocalPart)]
    [InlineData("blue garden@example.com", EmailFailureKind.InvalidLocalPart)]
    [InlineData("bluegarden@example", EmailFailureKind.InvalidDomain)]
    [InlineData("bluegarden@-example.com", EmailFailureKind.InvalidDomain)]
    [InlineData("bluegarden@example-.com", EmailFailureKind.InvalidDomain)]
    [InlineData("bluegarden@example.123", EmailFailureKind.InvalidDomain)]
    [InlineData("bluegarden@example.c", EmailFailureKind.InvalidDomain)]
    public void InvalidAddressesReturnSpecificSyntaxFailures(string? address, EmailFailureKind expectedFailure)
    {
        var result = new EmailChecker().CheckExistingAddress(address);

        Assert.False(result.IsAllowed);
        Assert.Equal(expectedFailure, result.FailureKind);
    }

    [Fact]
    public void OverlongAndMalformedLocalPartsAreRejected()
    {
        Assert.Equal(
            EmailFailureKind.InvalidLocalPart,
            new EmailChecker().CheckExistingAddress(new string('a', 65) + "@example.com").FailureKind);

        var malformed = "blue" + '\uD800' + "@example.com";
        Assert.Equal(
            EmailFailureKind.InvalidLocalPart,
            new EmailChecker().CheckExistingAddress(malformed).FailureKind);
    }

    [Fact]
    public void NewAddressCanBeLimitedToIssuingDomains()
    {
        var options = new EmailOptions();
        options.IssuingDomains.Add("lidl.nl");
        var checker = new EmailChecker(options);

        Assert.True(checker.CheckNewAddress("bluegarden@lidl.nl").IsAllowed);
        Assert.True(checker.CheckNewAddress("bluegarden@mail.lidl.nl").IsAllowed);
        Assert.Equal(
            EmailFailureKind.UnapprovedIssuingDomain,
            checker.CheckNewAddress("bluegarden@example.com").FailureKind);
        Assert.True(checker.CheckExistingAddress("bluegarden@example.com").IsAllowed);
    }

    [Fact]
    public void IssuingDomainsAreAutomaticallyProtected()
    {
        var options = new EmailOptions();
        options.IssuingDomains.Add("lidl.nl");

        var result = new EmailChecker(options).CheckExistingAddress("bluegarden@lidi.nl");

        Assert.Equal(EmailFailureKind.SuspiciousDomain, result.FailureKind);
        Assert.Equal("lidl.nl", result.MatchedProtectedDomain);
    }

    [Fact]
    public void InvalidConfiguredDomainsAreRejected()
    {
        var protectedOptions = new EmailOptions();
        protectedOptions.ProtectedDomains.Add("not-a-domain");

        var issuingOptions = new EmailOptions();
        issuingOptions.IssuingDomains.Add("also-not-a-domain");

        Assert.Throws<ArgumentException>(() => new EmailChecker(protectedOptions));
        Assert.Throws<ArgumentException>(() => new EmailChecker(issuingOptions));
    }

    [Fact]
    public void OptionsAreCapturedAtConstruction()
    {
        var options = new EmailOptions();
        options.ProtectedDomains.Add("lidl.nl");

        var checker = new EmailChecker(options);
        options.ProtectedDomains.Clear();

        Assert.Equal(
            EmailFailureKind.SuspiciousDomain,
            checker.CheckExistingAddress("bluegarden@lidi.nl").FailureKind);
    }

    [Fact]
    public void LocalPartOptionsCanAddApplicationReservations()
    {
        var options = new EmailOptions();
        options.LocalPartOptions.Reserve("billingdesk", global::Unclaimable.ReservedMatchMode.Exact);

        var result = new EmailChecker(options).CheckExistingAddress("billingdesk@example.com");

        Assert.Equal(EmailFailureKind.ReservedLocalPart, result.FailureKind);
        Assert.Equal("billingdesk", result.LocalPartResult!.MatchedValue);
    }

    [Fact]
    public void InjectedLocalPartCheckerIsUsed()
    {
        var coreOptions = new global::Unclaimable.Options();
        coreOptions.AllowedIdentifiers.Add("admin");
        coreOptions.DisableRule(
            global::Unclaimable.Rule.MinimumLength
            | global::Unclaimable.Rule.MaximumLength
            | global::Unclaimable.Rule.Whitespace
            | global::Unclaimable.Rule.BlockedCharacters
            | global::Unclaimable.Rule.LeadingSeparator
            | global::Unclaimable.Rule.TrailingSeparator);
        coreOptions.DisablePattern(
            global::Unclaimable.Pattern.NumericOnly
            | global::Unclaimable.Pattern.Repeated
            | global::Unclaimable.Pattern.SymbolOnly
            | global::Unclaimable.Pattern.AsciiArt
            | global::Unclaimable.Pattern.UppercaseOnly);

        var checker = new EmailChecker(
            new global::Unclaimable.Checker(coreOptions),
            new EmailOptions());

        Assert.True(checker.CheckExistingAddress("admin@example.com").IsAllowed);
    }

    [Fact]
    public void InvalidPurposeIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new EmailChecker().Check("bluegarden@example.com", (EmailAddressPurpose)999));
    }

    private static EmailChecker CreateProtectedChecker(string domain)
    {
        var options = new EmailOptions();
        options.ProtectedDomains.Add(domain);
        return new EmailChecker(options);
    }
}
