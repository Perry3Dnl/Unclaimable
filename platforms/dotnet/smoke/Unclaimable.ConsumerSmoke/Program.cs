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

var obfuscated = checker.Check("N1k3");
Require(obfuscated.IsReserved, "N1k3 should be rejected.");
Require(obfuscated.MatchedValue == "nike", "N1k3 should resolve to nike.");
Require(obfuscated.Category == "brands", "N1k3 should resolve to the brands category.");
Require(obfuscated.MatchKind == UnclaimableMatchKind.Obfuscated, "N1k3 should use obfuscation matching.");

var unicodeConfusable = checker.Check("\u0430pple");
Require(unicodeConfusable.IsReserved, "Cyrillic-a apple should be rejected.");
Require(unicodeConfusable.MatchedValue == "apple", "Cyrillic-a apple should resolve to apple.");
Require(unicodeConfusable.MatchKind == UnclaimableMatchKind.UnicodeConfusable, "Cyrillic-a apple should use Unicode-confusable matching.");

Require(checker.IsClaimable("ordinary-user"), "ordinary-user should remain claimable.");
Require(checker.IsClaimable("old-admin"), "Partial matching should remain opt-in.");

var strictChecker = new UnclaimableChecker(new UnclaimableOptions
{
    PartialMatching = true,
    AllowNumbers = false
});

var numberViolation = strictChecker.Check("old-admin2");
Require(numberViolation.IsReserved, "Numbers should be rejected when AllowNumbers is false.");
Require(numberViolation.MatchKind == UnclaimableMatchKind.NumbersNotAllowed, "Number policy should fail before reserved-name matching.");
Require(numberViolation.OffendingCharacterIndex == 9, "Number policy should expose the offending index.");
Require(numberViolation.OffendingCharacter == "2", "Number policy should expose the offending character.");

var detailed = strictChecker.CheckDetailed("old-admin2", includeMessages: true);
Require(detailed.Diagnostics.Any(item => item.Kind == UnclaimableMatchKind.NumbersNotAllowed), "Detailed checks should include the number-policy failure.");
Require(detailed.Diagnostics.Any(item => item.Kind == UnclaimableMatchKind.Partial && item.MatchedValue == "admin"), "Detailed checks should also include the embedded reserved name.");
Require(detailed.Diagnostics.All(item => !string.IsNullOrWhiteSpace(item.Message)), "Detailed checks with messages should provide user-facing feedback.");

var asciiChecker = new UnclaimableChecker(new UnclaimableOptions
{
    AsciiOnly = true
});

var invalidCharacters = asciiChecker.Check("caf\u00E9");
Require(invalidCharacters.IsReserved, "Non-ASCII input should be rejected when AsciiOnly is enabled.");
Require(invalidCharacters.MatchKind == UnclaimableMatchKind.InvalidCharacters, "AsciiOnly rejection should report InvalidCharacters.");
Require(invalidCharacters.OffendingCharacterIndex == 3, "AsciiOnly rejection should report the original input index.");

var services = new ServiceCollection();
services.AddUnclaimable(options =>
{
    options.AdditionalReserved.Add("examplebrand");
    options.PartialMatching = true;
    options.AllowNumbers = false;
});
using var provider = services.BuildServiceProvider();

var configuredChecker = provider.GetRequiredService<IUnclaimableChecker>();
Require(configuredChecker.IsReserved("ExampleBrand"), "DI-configured AdditionalReserved entry should be rejected.");
Require(configuredChecker.IsReserved("old-examplebrand"), "DI-configured partial matching should apply to AdditionalReserved entries.");
Require(configuredChecker.Check("user2").MatchKind == UnclaimableMatchKind.NumbersNotAllowed, "DI-configured number policy should be enforced.");

var rejectedModel = new SignupModel { UserName = "examplebrand" };
var rejectedResults = new List<ValidationResult>();
var rejectedContext = new ValidationContext(rejectedModel, provider, items: null);
Require(
    !Validator.TryValidateObject(rejectedModel, rejectedContext, rejectedResults, validateAllProperties: true),
    "ClaimableUsernameAttribute should reject a configured reserved value.");

var acceptedModel = new SignupModel { UserName = "ordinary-user" };
var acceptedResults = new List<ValidationResult>();
var acceptedContext = new ValidationContext(acceptedModel, provider, items: null);
Require(
    Validator.TryValidateObject(acceptedModel, acceptedContext, acceptedResults, validateAllProperties: true),
    "ClaimableUsernameAttribute should accept an ordinary value.");

Console.WriteLine("Packaged Unclaimable consumer smoke test passed.");

public sealed class SignupModel
{
    [Required]
    [ClaimableUsername]
    public string UserName { get; set; } = string.Empty;
}
