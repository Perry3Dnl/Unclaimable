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
        [ClaimableUsername]
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
}
