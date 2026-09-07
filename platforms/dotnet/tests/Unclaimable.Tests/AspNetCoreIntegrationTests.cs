using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Unclaimable.AspNetCore;
using Xunit;

namespace Unclaimable.Tests;

public sealed class AspNetCoreIntegrationTests
{
    private sealed class SignupModel
    {
        [Required]
        [Display(Name = "Username")]
        [ClaimableUsername]
        public string Username { get; init; } = string.Empty;
    }

    private sealed class CustomMessageSignupModel
    {
        [Required]
        [Display(Name = "Username")]
        [ClaimableUsername(ErrorMessage = "Choose another {FieldName}.")]
        public string Username { get; init; } = string.Empty;
    }

    [Fact]
    public void DependencyInjectionUsesApplicationSpecificReservations()
    {
        using var provider = new ServiceCollection()
            .AddUnclaimable(options => options.AdditionalReserved.Add("examplebrand"))
            .BuildServiceProvider();

        var checker = provider.GetRequiredService<IUnclaimableChecker>();

        Assert.True(checker.IsReserved("ExampleBrand"));
    }

    [Fact]
    public void ValidationAttributeUsesBuiltInMessageWhenNoMessageIsConfigured()
    {
        using var provider = new ServiceCollection()
            .AddUnclaimable(options => options.AdditionalReserved.Add("examplebrand"))
            .BuildServiceProvider();

        var results = Validate(new SignupModel { Username = "examplebrand" }, provider);

        var error = Assert.Single(results);
        Assert.Equal("Username is reserved and cannot be claimed.", error.ErrorMessage);
    }

    [Fact]
    public void ValidationAttributeUsesGlobalConfiguredMessage()
    {
        using var provider = new ServiceCollection()
            .AddUnclaimable(options =>
            {
                options.AdditionalReserved.Add("examplebrand");
                options.ValidationMessage = "{FieldName} is not available. Please choose another one.";
            })
            .BuildServiceProvider();

        var results = Validate(new SignupModel { Username = "examplebrand" }, provider);

        var error = Assert.Single(results);
        Assert.Equal("Username is not available. Please choose another one.", error.ErrorMessage);
    }

    [Fact]
    public void AttributeErrorMessageOverridesGlobalConfiguredMessage()
    {
        using var provider = new ServiceCollection()
            .AddUnclaimable(options =>
            {
                options.AdditionalReserved.Add("examplebrand");
                options.ValidationMessage = "Global message";
            })
            .BuildServiceProvider();

        var results = Validate(new CustomMessageSignupModel { Username = "examplebrand" }, provider);

        var error = Assert.Single(results);
        Assert.Equal("Choose another Username.", error.ErrorMessage);
    }

    [Fact]
    public void ValidationAttributeUsesRegisteredChecker()
    {
        using var provider = new ServiceCollection()
            .AddUnclaimable(options => options.AdditionalReserved.Add("examplebrand"))
            .BuildServiceProvider();

        var model = new SignupModel { Username = "examplebrand" };
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, provider, items: null);

        var isValid = Validator.TryValidateObject(model, context, validationResults, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, result => result.ErrorMessage?.Contains("reserved", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static IReadOnlyList<ValidationResult> Validate(object model, IServiceProvider provider)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, provider, items: null);

        var isValid = Validator.TryValidateObject(model, context, validationResults, validateAllProperties: true);

        Assert.False(isValid);
        return validationResults;
    }
}
