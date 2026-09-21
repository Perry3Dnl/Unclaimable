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

var services = new ServiceCollection();
services.AddUnclaimable(options =>
{
    options.AdditionalReserved.Add("examplebrand");
    options.ValidationMessage = "{FieldName} is unavailable.";
});

using var provider = services.BuildServiceProvider();

var checker = provider.GetRequiredService<IChecker>();
Require(checker.IsReserved("examplebrand"), "IChecker should resolve from ASP.NET Core DI.");
Require(checker.IsClaimable("ordinary2"), "The frozen 0.8.0 number default should flow through ASP.NET Core DI.");

var model = new SignupModel { UserName = "examplebrand" };
var results = new List<ValidationResult>();
var context = new ValidationContext(model, provider, items: null);

Require(
    !Validator.TryValidateObject(model, context, results, validateAllProperties: true),
    "ClaimableUsernameAttribute should reject a configured reserved identifier.");

Require(
    results.Count == 1 && results[0].ErrorMessage == "UserName is unavailable.",
    "ClaimableUsernameAttribute should use the registered validation message.");

Console.WriteLine(
    $"Unclaimable.AspNetCore compatibility smoke test passed on {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}.");

public sealed class SignupModel
{
    [Required]
    [ClaimableUsername]
    public string UserName { get; set; } = string.Empty;
}
