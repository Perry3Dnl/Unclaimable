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

var asciiChecker = new UnclaimableChecker(new UnclaimableOptions
{
    AsciiOnly = true
});

var invalidCharacters = asciiChecker.Check("caf\u00E9");
Require(invalidCharacters.IsReserved, "Non-ASCII input should be rejected when AsciiOnly is enabled.");
Require(invalidCharacters.MatchKind == UnclaimableMatchKind.InvalidCharacters, "AsciiOnly rejection should report InvalidCharacters.");

var services = new ServiceCollection();
services.AddUnclaimable(options => options.AdditionalReserved.Add("examplebrand"));
using var provider = services.BuildServiceProvider();

var configuredChecker = provider.GetRequiredService<IUnclaimableChecker>();
Require(configuredChecker.IsReserved("ExampleBrand"), "DI-configured AdditionalReserved entry should be rejected.");

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
