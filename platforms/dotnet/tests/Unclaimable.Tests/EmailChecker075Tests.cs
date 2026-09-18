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

    public static IEnumerable<object[]> BrandDomainSpoofCases()
    {
        // McDonald's
        yield return new object[] { "mcdonalds.com", "mcdonald.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "mcdonalds.com", "mcdonaldss.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "mcdonalds.com", "mcdnoalds.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "mcdonalds.com", "mcdonaldz.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "mcdonalds.com", "mcd0nalds.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "mcdonalds.com", "mcdоnalds.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "mcdonalds.com", "xn--mcdnalds-pbh.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "mcdonalds.com", "mcdonalds.net", DomainLookalikeKind.ProtectedLabelReuse };
        yield return new object[] { "mcdonalds.com", "mcdonalds-login.com", DomainLookalikeKind.ProtectedLabelReuse };
        yield return new object[] { "mcdonalds.com", "login-mcdonalds.com", DomainLookalikeKind.ProtectedLabelReuse };
        yield return new object[] { "mcdonalds.com", "mcdonalds.secure-login.example.com", DomainLookalikeKind.ProtectedLabelReuse };
        yield return new object[] { "mcdonalds.com", "mcdonalds.com.attacker.com", DomainLookalikeKind.EmbeddedProtectedDomain };

        // Nike
        yield return new object[] { "nike.com", "nie.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "nike.com", "niike.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "nike.com", "nkie.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "nike.com", "nixe.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "nike.com", "nik3.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "nike.com", "nіke.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "nike.com", "xn--nke-jhd.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "nike.com", "nike.net", DomainLookalikeKind.ProtectedLabelReuse };
        yield return new object[] { "nike.com", "nike-login.com", DomainLookalikeKind.ProtectedLabelReuse };
        yield return new object[] { "nike.com", "login-nike.com", DomainLookalikeKind.ProtectedLabelReuse };
        yield return new object[] { "nike.com", "nike.com.attacker.com", DomainLookalikeKind.EmbeddedProtectedDomain };

        // Google
        yield return new object[] { "google.com", "gogle.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "google.com", "gooogle.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "google.com", "googel.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "google.com", "googxe.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "google.com", "g00gle.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "google.com", "goog1e.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "google.com", "goo9le.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "google.com", "gοogle.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "google.com", "xn--gogle-rce.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "google.com", "google.co", DomainLookalikeKind.Typographical };
        yield return new object[] { "google.com", "google.net", DomainLookalikeKind.ProtectedLabelReuse };
        yield return new object[] { "google.com", "google-login.com", DomainLookalikeKind.ProtectedLabelReuse };
        yield return new object[] { "google.com", "login-google.com", DomainLookalikeKind.ProtectedLabelReuse };
        yield return new object[] { "google.com", "google.secure-login.example.com", DomainLookalikeKind.ProtectedLabelReuse };
        yield return new object[] { "google.com", "google.com.attacker.com", DomainLookalikeKind.EmbeddedProtectedDomain };

        // Amazon, including the explicit "amazone" typo.
        yield return new object[] { "amazon.com", "amazone.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "amazon.com", "amazn.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "amazon.com", "amaozn.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "amazon.com", "amazxn.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "amazon.com", "amaz0n.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "amazon.com", "ama2on.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "amazon.com", "amazοn.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "amazon.com", "xn--amazn-uce.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "amazon.com", "amazon.net", DomainLookalikeKind.ProtectedLabelReuse };
        yield return new object[] { "amazon.com", "amazon-login.com", DomainLookalikeKind.ProtectedLabelReuse };
        yield return new object[] { "amazon.com", "amazon.com.attacker.com", DomainLookalikeKind.EmbeddedProtectedDomain };

        // Visa
        yield return new object[] { "visa.com", "vis.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "visa.com", "viisa.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "visa.com", "vsia.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "visa.com", "viza.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "visa.com", "vi5a.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "visa.com", "vіsa.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "visa.com", "xn--vsa-jhd.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "visa.com", "visa.net", DomainLookalikeKind.ProtectedLabelReuse };
        yield return new object[] { "visa.com", "visa-login.com", DomainLookalikeKind.ProtectedLabelReuse };
        yield return new object[] { "visa.com", "visa.com.attacker.com", DomainLookalikeKind.EmbeddedProtectedDomain };

        // Nvidia
        yield return new object[] { "nvidia.com", "nvida.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "nvidia.com", "nnvidia.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "nvidia.com", "nvidai.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "nvidia.com", "nvidix.com", DomainLookalikeKind.Typographical };
        yield return new object[] { "nvidia.com", "nvidi4.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "nvidia.com", "nvіdia.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "nvidia.com", "xn--nvdia-o2e.com", DomainLookalikeKind.Confusable };
        yield return new object[] { "nvidia.com", "nvidia.net", DomainLookalikeKind.ProtectedLabelReuse };
        yield return new object[] { "nvidia.com", "nvidia-login.com", DomainLookalikeKind.ProtectedLabelReuse };
        yield return new object[] { "nvidia.com", "nvidia.com.attacker.com", DomainLookalikeKind.EmbeddedProtectedDomain };
    }

    [Theory]
    [MemberData(nameof(BrandDomainSpoofCases))]
    public void BrandDomainSpoofMatrixIsRejected(
        string protectedDomain,
        string candidateDomain,
        DomainLookalikeKind expectedKind)
    {
        var result = CreateProtectedChecker(protectedDomain)
            .CheckExistingAddress("bluegarden@" + candidateDomain);

        Assert.False(result.IsAllowed);
        Assert.Equal(EmailFailureKind.SuspiciousDomain, result.FailureKind);
        Assert.Equal(expectedKind, result.DomainLookalikeKind);
        Assert.Equal(protectedDomain, result.MatchedProtectedDomain);
    }

    public static IEnumerable<object[]> ProtectedBrandDomains()
    {
        yield return new object[] { "mcdonalds.com" };
        yield return new object[] { "nike.com" };
        yield return new object[] { "google.com" };
        yield return new object[] { "amazon.com" };
        yield return new object[] { "visa.com" };
        yield return new object[] { "nvidia.com" };
    }

    [Theory]
    [MemberData(nameof(ProtectedBrandDomains))]
    public void ProtectedBrandDomainsAllowExactCaseVariantsAndRealSubdomains(string protectedDomain)
    {
        var checker = CreateProtectedChecker(protectedDomain);

        Assert.True(checker.CheckExistingAddress("bluegarden@" + protectedDomain).IsAllowed);
        Assert.True(checker.CheckExistingAddress("bluegarden@" + protectedDomain.ToUpperInvariant()).IsAllowed);
        Assert.True(checker.CheckExistingAddress("bluegarden@mail." + protectedDomain).IsAllowed);
        Assert.True(checker.CheckExistingAddress("bluegarden@deep.mail." + protectedDomain).IsAllowed);
    }

    [Fact]
    public void MultipleProtectedBrandsReportTheSpecificDomainThatMatched()
    {
        var options = new EmailOptions();
        foreach (var data in ProtectedBrandDomains())
        {
            options.ProtectedDomains.Add((string)data[0]);
        }

        var checker = new EmailChecker(options);

        Assert.Equal(
            "nike.com",
            checker.CheckExistingAddress("bluegarden@nik3.com").MatchedProtectedDomain);
        Assert.Equal(
            "amazon.com",
            checker.CheckExistingAddress("bluegarden@amazone.com").MatchedProtectedDomain);
        Assert.Equal(
            "google.com",
            checker.CheckExistingAddress("bluegarden@xn--gogle-rce.com").MatchedProtectedDomain);
    }

    [Fact]
    public void DistanceTwoSpoofsAreOptInThroughMaximumDomainEditDistance()
    {
        var defaultOptions = new EmailOptions();
        defaultOptions.ProtectedDomains.Add("google.com");
        Assert.True(
            new EmailChecker(defaultOptions)
                .CheckExistingAddress("bluegarden@ggoglee.com")
                .IsAllowed);

        var expandedOptions = new EmailOptions { MaximumDomainEditDistance = 2 };
        expandedOptions.ProtectedDomains.Add("google.com");

        var result = new EmailChecker(expandedOptions)
            .CheckExistingAddress("bluegarden@ggoglee.com");

        Assert.Equal(EmailFailureKind.SuspiciousDomain, result.FailureKind);
        Assert.Equal(DomainLookalikeKind.Typographical, result.DomainLookalikeKind);
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

    public static IEnumerable<object[]> AsciiConfusableMappingCases()
    {
        yield return new object[] { "o.com", "0.com" };
        yield return new object[] { "l.com", "1.com" };
        yield return new object[] { "z.com", "2.com" };
        yield return new object[] { "e.com", "3.com" };
        yield return new object[] { "a.com", "4.com" };
        yield return new object[] { "s.com", "5.com" };
        yield return new object[] { "g.com", "6.com" };
        yield return new object[] { "g.com", "9.com" };
        yield return new object[] { "t.com", "7.com" };
        yield return new object[] { "b.com", "8.com" };
    }

    [Theory]
    [MemberData(nameof(AsciiConfusableMappingCases))]
    public void EveryAsciiDomainConfusableMappingIsCovered(string protectedDomain, string spoofDomain)
    {
        var result = CreateProtectedChecker(protectedDomain)
            .CheckExistingAddress("bluegarden@" + spoofDomain);

        Assert.Equal(EmailFailureKind.SuspiciousDomain, result.FailureKind);
        Assert.Equal(DomainLookalikeKind.Confusable, result.DomainLookalikeKind);
    }

    public static IEnumerable<object[]> UnicodeConfusableMappingCases()
    {
        yield return new object[] { "a.com", "а.com" };
        yield return new object[] { "b.com", "в.com" };
        yield return new object[] { "e.com", "е.com" };
        yield return new object[] { "k.com", "к.com" };
        yield return new object[] { "m.com", "м.com" };
        yield return new object[] { "h.com", "н.com" };
        yield return new object[] { "o.com", "о.com" };
        yield return new object[] { "p.com", "р.com" };
        yield return new object[] { "c.com", "с.com" };
        yield return new object[] { "t.com", "т.com" };
        yield return new object[] { "y.com", "у.com" };
        yield return new object[] { "x.com", "х.com" };
        yield return new object[] { "s.com", "ѕ.com" };
        yield return new object[] { "i.com", "і.com" };
        yield return new object[] { "j.com", "ј.com" };
        yield return new object[] { "l.com", "ӏ.com" };
        yield return new object[] { "a.com", "α.com" };
        yield return new object[] { "b.com", "β.com" };
        yield return new object[] { "e.com", "ε.com" };
        yield return new object[] { "i.com", "ι.com" };
        yield return new object[] { "k.com", "κ.com" };
        yield return new object[] { "m.com", "μ.com" };
        yield return new object[] { "v.com", "ν.com" };
        yield return new object[] { "o.com", "ο.com" };
        yield return new object[] { "p.com", "ρ.com" };
        yield return new object[] { "t.com", "τ.com" };
        yield return new object[] { "y.com", "υ.com" };
        yield return new object[] { "x.com", "χ.com" };
        yield return new object[] { "c.com", "ς.com" };
        yield return new object[] { "i.com", "ı.com" };
    }

    [Theory]
    [MemberData(nameof(UnicodeConfusableMappingCases))]
    public void EveryUnicodeDomainConfusableMappingIsCovered(string protectedDomain, string spoofDomain)
    {
        var result = CreateProtectedChecker(protectedDomain)
            .CheckExistingAddress("bluegarden@" + spoofDomain);

        Assert.Equal(EmailFailureKind.SuspiciousDomain, result.FailureKind);
        Assert.Equal(DomainLookalikeKind.Confusable, result.DomainLookalikeKind);
    }

    [Fact]
    public void CombiningMarkHomographIsDetected()
    {
        var result = CreateProtectedChecker("cafe.com")
            .CheckExistingAddress("bluegarden@café.com");

        Assert.Equal(EmailFailureKind.SuspiciousDomain, result.FailureKind);
        Assert.Equal(DomainLookalikeKind.Confusable, result.DomainLookalikeKind);
    }

    [Fact]
    public void InternationalizedDomainsAndLocalPartsCanBeValid()
    {
        var options = new EmailOptions();
        options.ProtectedDomains.Add("bücher.de");
        var checker = new EmailChecker(options);

        var exact = checker.CheckExistingAddress("bücher@BÜCHER.DE");
        Assert.True(exact.IsAllowed);
        Assert.Equal("xn--bcher-kva.de", exact.Domain);

        var idnTld = new EmailChecker().CheckExistingAddress("bluegarden@example.рф");
        Assert.True(idnTld.IsAllowed);
        Assert.StartsWith("example.xn--", idnTld.Domain);
    }

    [Theory]
    [InlineData("!#$%&'*+-/=?^_\u0060{|}~@example.com")]
    [InlineData("😀@example.com")]
    public void ValidExtendedLocalPartFormsAreAccepted(string address)
    {
        Assert.True(new EmailChecker().CheckExistingAddress(address).IsAllowed);
    }

    [Fact]
    public void InvalidUnicodeAndAsciiLocalPartCharactersAreRejected()
    {
        var checker = new EmailChecker();

        Assert.Equal(
            EmailFailureKind.InvalidLocalPart,
            checker.CheckExistingAddress("blue(garden@example.com").FailureKind);
        Assert.Equal(
            EmailFailureKind.InvalidLocalPart,
            checker.CheckExistingAddress("blue\u00A0garden@example.com").FailureKind);
        Assert.Equal(
            EmailFailureKind.InvalidLocalPart,
            checker.CheckExistingAddress("blue\u200Dgarden@example.com").FailureKind);
        Assert.Equal(
            EmailFailureKind.InvalidLocalPart,
            checker.CheckExistingAddress("blue\uE000garden@example.com").FailureKind);

        var lowSurrogate = "blue" + '\uDC00' + "@example.com";
        Assert.Equal(
            EmailFailureKind.InvalidLocalPart,
            checker.CheckExistingAddress(lowSurrogate).FailureKind);
    }

    [Fact]
    public void OversizedMalformedAndInvalidIdnDomainsAreRejected()
    {
        var checker = new EmailChecker();

        Assert.Equal(
            EmailFailureKind.InvalidDomain,
            checker.CheckExistingAddress("bluegarden@.example.com").FailureKind);
        Assert.Equal(
            EmailFailureKind.InvalidDomain,
            checker.CheckExistingAddress("bluegarden@example.com.").FailureKind);
        Assert.Equal(
            EmailFailureKind.InvalidDomain,
            checker.CheckExistingAddress("bluegarden@example..com").FailureKind);
        Assert.Equal(
            EmailFailureKind.InvalidDomain,
            checker.CheckExistingAddress("bluegarden@example_.com").FailureKind);
        Assert.Equal(
            EmailFailureKind.InvalidDomain,
            checker.CheckExistingAddress("bluegarden@" + new string('a', 64) + ".com").FailureKind);

        var malformedIdn = "bluegarden@" + '\uD800' + ".com";
        Assert.Equal(
            EmailFailureKind.InvalidDomain,
            checker.CheckExistingAddress(malformedIdn).FailureKind);

        var oversizedDomain =
            new string('a', 63) + "." +
            new string('b', 63) + "." +
            new string('c', 63) + "." +
            new string('d', 61) + ".com";
        Assert.Equal(
            EmailFailureKind.InvalidDomain,
            checker.CheckExistingAddress("bluegarden@" + oversizedDomain).FailureKind);
    }

    [Fact]
    public void TotalMailboxLengthLimitIsEnforced()
    {
        var domain =
            new string('a', 62) + "." +
            new string('b', 62) + "." +
            new string('c', 62) + ".com";
        var address = new string('q', 64) + "@" + domain;

        Assert.Equal(
            EmailFailureKind.InvalidFormat,
            new EmailChecker().CheckExistingAddress(address).FailureKind);
    }

    [Fact]
    public void UnrelatedDomainsRemainAllowedWhenBrandProtectionIsEnabled()
    {
        var checker = CreateProtectedChecker("google.com");

        Assert.True(checker.CheckExistingAddress("bluegarden@ordinary-example.net").IsAllowed);
    }

    [Fact]
    public void ShortProtectedLabelsDoNotTriggerLabelReuse()
    {
        var checker = CreateProtectedChecker("aa.com");

        Assert.True(checker.CheckExistingAddress("bluegarden@aa.net").IsAllowed);
    }

    [Fact]
    public void DuplicateProtectedAndIssuingDomainConfigurationIsSafe()
    {
        var options = new EmailOptions();
        options.ProtectedDomains.Add("google.com");
        options.IssuingDomains.Add("GOOGLE.COM");

        var checker = new EmailChecker(options);

        Assert.True(checker.CheckNewAddress("bluegarden@google.com").IsAllowed);
        Assert.Equal(
            DomainLookalikeKind.Typographical,
            checker.CheckExistingAddress("bluegarden@gogle.com").DomainLookalikeKind);
    }

    [Fact]
    public void ConstructorGuardsRejectNullDependenciesAndNegativeDistance()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailChecker((EmailOptions)null!));
        Assert.Throws<ArgumentNullException>(
            () => new EmailChecker((global::Unclaimable.IChecker)null!, new EmailOptions()));
        Assert.Throws<ArgumentNullException>(
            () => new EmailChecker(new global::Unclaimable.Checker(), null!));

        var options = new EmailOptions { MaximumDomainEditDistance = -1 };
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailChecker(options));
    }

    [Fact]
    public void IsAllowedConvenienceMethodUsesTheRequestedPurpose()
    {
        var options = new EmailOptions();
        options.IssuingDomains.Add("google.com");
        var checker = new EmailChecker(options);

        Assert.True(checker.IsAllowed("bluegarden@example.com", EmailAddressPurpose.ExistingAddress));
        Assert.False(checker.IsAllowed("bluegarden@example.com", EmailAddressPurpose.NewAddress));
    }

    [Fact]
    public void EmailResultExposesParsedAndDiagnosticState()
    {
        var result = CreateProtectedChecker("google.com")
            .CheckExistingAddress("bluegarden@gogle.com");

        Assert.Equal("bluegarden@gogle.com", result.Address);
        Assert.Equal(EmailAddressPurpose.ExistingAddress, result.Purpose);
        Assert.Equal("bluegarden", result.LocalPart);
        Assert.Equal("gogle.com", result.Domain);
        Assert.NotNull(result.LocalPartResult);
        Assert.True(result.IsSuspiciousDomain);
        Assert.Equal("google.com", result.MatchedProtectedDomain);
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
