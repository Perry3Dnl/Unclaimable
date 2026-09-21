using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Unclaimable;
using Unclaimable.AspNetCore;
using Unclaimable.Email;
using Unclaimable.Extended;

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

var checker = Checker.Default;

Require(checker.Check("customersupport").Category == "support", "English support names should be reserved by default.");
Require(checker.Check("myloginverificationteamx").MatchKind == MatchKind.Partial, "Explicit high-risk partial entries should be protected in strict mode.");
Require(checker.Check("supportive").MatchKind == MatchKind.Partial, "Sensitive support roots should protect containing identifiers in strict mode.");
Require(checker.IsClaimable("ordinary2"), "Numbers should be allowed inside identifiers by default.");
Require(checker.Check("123456789").MatchKind == MatchKind.NumericOnly, "Numeric-only identifiers should still be blocked by the default numeric-only pattern.");
Require(checker.Check("ordinary-user").MatchKind == MatchKind.BlockedCharacter, "Hyphens should be blocked by default.");
Require(checker.Check("ordinary_user").MatchKind == MatchKind.BlockedCharacter, "Underscores should be blocked by default.");
Require(checker.Check("ordinary user").MatchKind == MatchKind.BlockedCharacter, "Whitespace should be blocked by default.");
Require(checker.Check("ab").MatchKind == MatchKind.TooShort, "Minimum length should be enforced by default.");
Require(checker.Check(new string('a', 33)).MatchKind == MatchKind.TooLong, "Maximum length should be enforced by default.");
Require(checker.Check("fuckwaffle").Category == "profanity", "Curated English profanity compounds should participate by default.");
Require(checker.IsClaimable("facturatiehulp"), "Dutch localized support data should not load until Dutch is added.");
Require(checker.IsClaimable("abrechnungshilfe"), "German localized support data should not load until German is added.");

var englishAndDutchOptions = new Options();
englishAndDutchOptions.AddLanguage(Language.Dutch);
var englishAndDutchChecker = new Checker(englishAndDutchOptions);
Require(englishAndDutchChecker.Check("customersupport").Category == "support", "Adding Dutch should keep English enabled.");
Require(englishAndDutchChecker.Check("facturatiehulp").Category == "support", "Dutch should be additive.");
Require(englishAndDutchChecker.Check("systeembeheerder").Category == "roles", "Dutch role data should be embedded in the package.");
Require(englishAndDutchChecker.Check("godverdomme").Category == "profanity", "Dutch profanity data should be embedded in the package.");

var dutchOnlyOptions = new Options();
dutchOnlyOptions.RemoveLanguage(Language.English);
dutchOnlyOptions.AddLanguage(Language.Dutch);
var dutchOnlyChecker = new Checker(dutchOnlyOptions);
Require(dutchOnlyChecker.Check("facturatiehulp").Category == "support", "Dutch-only filtering should be supported.");
Require(dutchOnlyChecker.IsClaimable("customersupport"), "Removing English should remove English localized data.");

var germanOptions = new Options();
germanOptions.AddLanguage(Language.German);
var germanChecker = new Checker(germanOptions);
Require(germanChecker.Check("abrechnungshilfe").Category == "support", "German should be addable.");
Require(germanChecker.Check("systemverwalter").Category == "roles", "German role data should be embedded in the package.");
Require(germanChecker.Check("scheiße").Category == "profanity", "German profanity data should be embedded in the package.");
Require(germanChecker.IsReserved("customersupport"), "English should remain enabled when German is added.");

var extendedOptions = new Options();
extendedOptions.AddLanguage(Language.French);
extendedOptions.AddLanguage(Language.Spanish);
extendedOptions.AddLanguage(Language.Italian);
extendedOptions.AddLanguage(Language.Portuguese);
var extendedChecker = new Checker(extendedOptions);
Require(extendedChecker.Check("serviceclient").Category == "support", "French language data should be embedded in the package.");
Require(extendedChecker.Check("servicioalcliente").Category == "support", "Spanish language data should be embedded in the package.");
Require(extendedChecker.Check("servizioclienti").Category == "support", "Italian language data should be embedded in the package.");
Require(extendedChecker.Check("atendimentocliente").Category == "support", "Portuguese language data should be embedded in the package.");

var numberRestrictedOptions = new Options().EnableRule(Rule.Numbers);
var numberRestrictedChecker = new Checker(numberRestrictedOptions);
Require(numberRestrictedChecker.Check("ordinary2").MatchKind == MatchKind.NumbersNotAllowed, "Applications should be able to reject numbers explicitly.");

var relaxed = new Checker(new Options
{
    Strictness = Strictness.Standard,
    DisabledRules = Rule.Numbers
                    | Rule.BlockedCharacters
                    | Rule.Whitespace
                    | Rule.LeadingSeparator
                    | Rule.TrailingSeparator
});

Require(relaxed.IsClaimable("ordinary-user2"), "Applications should be able to relax individual rules.");

var obfuscationChecker = new Checker(new Options
{
    DisabledRules = Rule.Numbers
});
var obfuscated = obfuscationChecker.Check("N1k3");
Require(obfuscated.MatchedValue == "nike", "N1k3 should resolve to nike when numeric identifiers are allowed.");
Require(obfuscated.MatchKind == MatchKind.Obfuscated, "N1k3 should use obfuscation matching.");

var unicodeConfusable = checker.Check("\u0430pple");
Require(unicodeConfusable.MatchedValue == "apple", "Cyrillic-a apple should resolve to apple.");
Require(unicodeConfusable.MatchKind == MatchKind.UnicodeConfusable, "Cyrillic-a apple should use Unicode-confusable matching.");

var configurableOptions = new Options();
configurableOptions.DisableCategory(Category.Brands);
configurableOptions.AllowedIdentifiers.Add("superadmin");
configurableOptions.Reserve("acme", matching: ReservedMatchMode.Exact);
var configurableChecker = new Checker(configurableOptions);
Require(configurableChecker.IsClaimable("nike"), "Packaged consumers should be able to disable a built-in category.");
Require(configurableChecker.IsClaimable("superadmin"), "Packaged consumers should be able to allow one complete built-in identifier.");
Require(configurableChecker.IsReserved("mysuperadminx"), "Allowed identifiers should not automatically allow compounds.");
Require(configurableChecker.IsReserved("ACME"), "Exact application reservations should use case/Unicode normalization.");
Require(configurableChecker.IsClaimable("acmeorchid"), "Exact application reservations should not block ordinary compounds.");

var wholeOptions = new Options();
wholeOptions.Reserve("Example Identity", "examplecategory", ReservedMatchMode.WholeIdentifier);
var wholeChecker = new Checker(wholeOptions);
Require(wholeChecker.Check("ExampleIdentity").Category == "examplecategory", "WholeIdentifier reservations should preserve categories and compact matching.");
Require(wholeChecker.IsClaimable("MyExampleIdentityFan"), "WholeIdentifier reservations should not become generic substring roots.");

var startupOptions = new Options();
startupOptions.AdditionalBlockedCharacters("^", "$");
var startupChecker = new Checker(startupOptions);
Require(startupChecker.Check("normal^name").MatchKind == MatchKind.BlockedCharacter, "Startup blocked characters should be enforced.");

var runtimeOptions = new Options { Strictness = Strictness.Standard };
var runtimePolicy = new Policy(runtimeOptions.ConfiguredBlockedCharacters);
var runtimeChecker = new Checker(runtimeOptions, runtimePolicy);
Require(runtimeChecker.IsClaimable("normal^name"), "Caret should initially be allowed.");
runtimePolicy.BlockCharacter("^");
Require(runtimeChecker.Check("normal^name").MatchKind == MatchKind.BlockedCharacter, "Runtime policy changes should affect the existing checker.");

var services = new ServiceCollection();
services.AddUnclaimable(options =>
{
    options.AdditionalReserved.Add("examplebrand");
    options.AdditionalBlockedCharacters("^");
    options.ValidationMessage = "{FieldName} is unavailable.";
    options.Messages.Reserved = "{FieldName} '{MatchedValue}' is reserved.";
});
using var provider = services.BuildServiceProvider();

var configuredChecker = provider.GetRequiredService<IChecker>();
var configuredPolicy = provider.GetRequiredService<IPolicy>();
Require(configuredChecker.IsReserved("ExampleBrand"), "DI-configured AdditionalReserved entry should be rejected.");
Require(configuredChecker.Check("normal^name").MatchKind == MatchKind.BlockedCharacter, "DI should seed startup blocked characters.");
configuredPolicy.BlockCharacter("@");
Require(configuredChecker.Check("normal@name").MatchKind == MatchKind.BlockedCharacter, "DI runtime policy should stay live.");

var rejectedModel = new SignupModel { UserName = "examplebrand" };
var rejectedResults = new List<ValidationResult>();
var rejectedContext = new ValidationContext(rejectedModel, provider, items: null);
Require(
    !Validator.TryValidateObject(rejectedModel, rejectedContext, rejectedResults, validateAllProperties: true),
    "ClaimableUsernameAttribute should reject a configured reserved value.");
Require(
    rejectedResults.Count == 1 && rejectedResults[0].ErrorMessage == "UserName 'examplebrand' is reserved.",
    "ClaimableUsernameAttribute should use the configured reason-specific validation message.");

var emailOptions = new EmailOptions();
emailOptions.ProtectedDomains.Add("lidl.nl");
emailOptions.IssuingDomains.Add("lidl.nl");
var emailChecker = new EmailChecker(emailOptions);
Require(
    emailChecker.CheckExistingAddress("admin@lidi.nl").FailureKind == EmailFailureKind.ReservedLocalPart,
    "Packaged email consumers should still apply Unclaimable to external email local parts.");
Require(
    emailChecker.CheckExistingAddress("bluegarden@lidi.nl").DomainLookalikeKind == DomainLookalikeKind.Typographical,
    "Packaged email consumers should detect protected-domain typo variants.");
Require(
    emailChecker.CheckNewAddress("bluegarden@lidl.nl").IsAllowed,
    "Packaged email consumers should be able to issue an ordinary local part on an approved exact domain.");

Require(ExtendedData.TotalEntries > 30000, "Packaged Extended data should contain the large optional snapshot.");
var extendedDataOptions = new Options();
extendedDataOptions.UseExtendedData();
var extendedDataChecker = new Checker(extendedDataOptions);
Require(extendedDataChecker.IsReserved("aalborguniversity"), "Extended education data should be active after opt-in.");
Require(extendedDataChecker.IsReserved("aalborgairport"), "Extended transport data should be active after opt-in.");
var noEducationOptions = new Options();
noEducationOptions.UseExtendedData(extended => extended.DisableCategory(ExtendedCategory.Education));
Require(new Checker(noEducationOptions).IsClaimable("aalborguniversity"), "Extended categories should be independently disableable.");

Console.WriteLine("Packaged Unclaimable consumer smoke test passed.");

public sealed class SignupModel
{
    [Required]
    [ClaimableUsername]
    public string UserName { get; set; } = string.Empty;
}
