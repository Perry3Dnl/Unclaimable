using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Unclaimable;
using Unclaimable.AspNetCore;

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

var checker = UnclaimableChecker.Default;

Require(checker.Check("customersupport").Category == "support", "English support names should be reserved by default.");
Require(checker.Check("supportportal").MatchKind == UnclaimableMatchKind.Partial, "Strict partial matching should be enabled for English data by default.");
Require(checker.Check("ordinary2").MatchKind == UnclaimableMatchKind.NumbersNotAllowed, "Numbers should be blocked by default.");
Require(checker.Check("ordinary-user").MatchKind == UnclaimableMatchKind.BlockedCharacter, "Hyphens should be blocked by default.");
Require(checker.Check("ordinary_user").MatchKind == UnclaimableMatchKind.BlockedCharacter, "Underscores should be blocked by default.");
Require(checker.Check("ordinary user").MatchKind == UnclaimableMatchKind.BlockedCharacter, "Whitespace should be blocked by default.");
Require(checker.Check("ab").MatchKind == UnclaimableMatchKind.TooShort, "Minimum length should be enforced by default.");
Require(checker.Check(new string('a', 33)).MatchKind == UnclaimableMatchKind.TooLong, "Maximum length should be enforced by default.");
Require(checker.Check("fuckwaffle").Category == "profanity", "English profanity should participate by default.");
Require(checker.IsClaimable("facturatiehulp"), "Dutch localized support data should not load until Dutch is added.");
Require(checker.IsClaimable("abrechnungshilfe"), "German localized support data should not load until German is added.");

var englishAndDutchOptions = new UnclaimableOptions();
englishAndDutchOptions.AddLanguage(UnclaimableLanguage.Dutch);
var englishAndDutchChecker = new UnclaimableChecker(englishAndDutchOptions);
Require(englishAndDutchChecker.Check("customersupport").Category == "support", "Adding Dutch should keep English enabled.");
Require(englishAndDutchChecker.Check("facturatiehulp").Category == "support", "Dutch should be additive.");
Require(englishAndDutchChecker.Check("systeembeheerder").Category == "roles", "Dutch role data should be embedded in the package.");
Require(englishAndDutchChecker.Check("godverdomme").Category == "profanity", "Dutch profanity data should be embedded in the package.");

var dutchOnlyOptions = new UnclaimableOptions();
dutchOnlyOptions.RemoveLanguage(UnclaimableLanguage.English);
dutchOnlyOptions.AddLanguage(UnclaimableLanguage.Dutch);
var dutchOnlyChecker = new UnclaimableChecker(dutchOnlyOptions);
Require(dutchOnlyChecker.Check("facturatiehulp").Category == "support", "Dutch-only filtering should be supported.");
Require(dutchOnlyChecker.IsClaimable("customersupport"), "Removing English should remove English localized data.");

var germanOptions = new UnclaimableOptions();
germanOptions.AddLanguage(UnclaimableLanguage.German);
var germanChecker = new UnclaimableChecker(germanOptions);
Require(germanChecker.Check("abrechnungshilfe").Category == "support", "German should be addable.");
Require(germanChecker.Check("systemverwalter").Category == "roles", "German role data should be embedded in the package.");
Require(germanChecker.Check("scheiße").Category == "profanity", "German profanity data should be embedded in the package.");
Require(germanChecker.IsReserved("customersupport"), "English should remain enabled when German is added.");

var extendedOptions = new UnclaimableOptions();
extendedOptions.AddLanguage(UnclaimableLanguage.French);
extendedOptions.AddLanguage(UnclaimableLanguage.Spanish);
extendedOptions.AddLanguage(UnclaimableLanguage.Italian);
extendedOptions.AddLanguage(UnclaimableLanguage.Portuguese);
var extendedChecker = new UnclaimableChecker(extendedOptions);
Require(extendedChecker.Check("serviceclient").Category == "support", "French language data should be embedded in the package.");
Require(extendedChecker.Check("servicioalcliente").Category == "support", "Spanish language data should be embedded in the package.");
Require(extendedChecker.Check("servizioclienti").Category == "support", "Italian language data should be embedded in the package.");
Require(extendedChecker.Check("atendimentocliente").Category == "support", "Portuguese language data should be embedded in the package.");

var relaxed = new UnclaimableChecker(new UnclaimableOptions
{
    Strictness = UnclaimableStrictness.Standard,
    DisabledRules = UnclaimableRule.Numbers
                    | UnclaimableRule.BlockedCharacters
                    | UnclaimableRule.Whitespace
                    | UnclaimableRule.LeadingSeparator
                    | UnclaimableRule.TrailingSeparator
});

Require(relaxed.IsClaimable("ordinary-user2"), "Applications should be able to relax individual rules.");

var obfuscationChecker = new UnclaimableChecker(new UnclaimableOptions
{
    DisabledRules = UnclaimableRule.Numbers
});
var obfuscated = obfuscationChecker.Check("N1k3");
Require(obfuscated.MatchedValue == "nike", "N1k3 should resolve to nike when numeric identifiers are allowed.");
Require(obfuscated.MatchKind == UnclaimableMatchKind.Obfuscated, "N1k3 should use obfuscation matching.");

var unicodeConfusable = checker.Check("\u0430pple");
Require(unicodeConfusable.MatchedValue == "apple", "Cyrillic-a apple should resolve to apple.");
Require(unicodeConfusable.MatchKind == UnclaimableMatchKind.UnicodeConfusable, "Cyrillic-a apple should use Unicode-confusable matching.");

var startupOptions = new UnclaimableOptions();
startupOptions.AdditionalBlockedCharacters("^", "$");
var startupChecker = new UnclaimableChecker(startupOptions);
Require(startupChecker.Check("normal^name").MatchKind == UnclaimableMatchKind.BlockedCharacter, "Startup blocked characters should be enforced.");

var runtimeOptions = new UnclaimableOptions { Strictness = UnclaimableStrictness.Standard };
var runtimePolicy = new UnclaimablePolicy(runtimeOptions.ConfiguredBlockedCharacters);
var runtimeChecker = new UnclaimableChecker(runtimeOptions, runtimePolicy);
Require(runtimeChecker.IsClaimable("normal^name"), "Caret should initially be allowed.");
runtimePolicy.BlockCharacter("^");
Require(runtimeChecker.Check("normal^name").MatchKind == UnclaimableMatchKind.BlockedCharacter, "Runtime policy changes should affect the existing checker.");

var services = new ServiceCollection();
services.AddUnclaimable(options =>
{
    options.AdditionalReserved.Add("examplebrand");
    options.AdditionalBlockedCharacters("^");
    options.ValidationMessage = "{FieldName} is unavailable.";
    options.Messages.Reserved = "{FieldName} '{MatchedValue}' is reserved.";
});
using var provider = services.BuildServiceProvider();

var configuredChecker = provider.GetRequiredService<IUnclaimableChecker>();
var configuredPolicy = provider.GetRequiredService<IUnclaimablePolicy>();
Require(configuredChecker.IsReserved("ExampleBrand"), "DI-configured AdditionalReserved entry should be rejected.");
Require(configuredChecker.Check("normal^name").MatchKind == UnclaimableMatchKind.BlockedCharacter, "DI should seed startup blocked characters.");
configuredPolicy.BlockCharacter("@");
Require(configuredChecker.Check("normal@name").MatchKind == UnclaimableMatchKind.BlockedCharacter, "DI runtime policy should stay live.");

var rejectedModel = new SignupModel { UserName = "examplebrand" };
var rejectedResults = new List<ValidationResult>();
var rejectedContext = new ValidationContext(rejectedModel, provider, items: null);
Require(
    !Validator.TryValidateObject(rejectedModel, rejectedContext, rejectedResults, validateAllProperties: true),
    "ClaimableUsernameAttribute should reject a configured reserved value.");
Require(
    rejectedResults.Count == 1 && rejectedResults[0].ErrorMessage == "UserName 'examplebrand' is reserved.",
    "ClaimableUsernameAttribute should use the configured reason-specific validation message.");

Console.WriteLine("Packaged Unclaimable consumer smoke test passed.");

public sealed class SignupModel
{
    [Required]
    [ClaimableUsername]
    public string UserName { get; set; } = string.Empty;
}
