using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Unclaimable.AspNetCore;
using Xunit;

namespace Unclaimable.Tests;

public sealed class AttributeLength040Tests
{
    [Fact]
    public void AttributeWithoutDependencyInjectionIncludesDefaultMinimumLength()
    {
        var error = Assert.Single(ValidateWithoutServices(new AttributeModel { Username = "ab" }));

        Assert.Equal("Username must be at least 3 characters long.", error.ErrorMessage);
    }

    [Fact]
    public void AttributeWithoutDependencyInjectionIncludesDefaultMaximumLength()
    {
        var error = Assert.Single(ValidateWithoutServices(new AttributeModel { Username = new string('x', 33) }));

        Assert.Equal("Username must be no more than 32 characters long.", error.ErrorMessage);
    }

    [Fact]
    public void AttributePrefersCapturedResultLengthOverMutatedRegisteredOptions()
    {
        using var provider = new ServiceCollection()
            .AddUnclaimable(options => options.MinimumLength = 5)
            .BuildServiceProvider();

        _ = provider.GetRequiredService<IChecker>();
        provider.GetRequiredService<Options>().MinimumLength = 8;

        var error = Assert.Single(ValidateWithServices(new AttributeModel { Username = "four" }, provider));
        Assert.Equal("Username must be at least 5 characters long.", error.ErrorMessage);
    }

    [Fact]
    public void AttributeFallsBackToRegisteredOptionsForCustomCheckerResults()
    {
        var options = new Options { MinimumLength = 7 };
        using var provider = new ServiceCollection()
            .AddSingleton(options)
            .AddSingleton<IChecker>(new TooShortChecker())
            .BuildServiceProvider();

        var error = Assert.Single(ValidateWithServices(new AttributeModel { Username = "value" }, provider));
        Assert.Equal("Username must be at least 7 characters long.", error.ErrorMessage);
    }

    private static IReadOnlyList<ValidationResult> ValidateWithoutServices(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Assert.False(Validator.TryValidateObject(model, context, results, validateAllProperties: true));
        return results;
    }

    private static IReadOnlyList<ValidationResult> ValidateWithServices(object model, IServiceProvider provider)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model, provider, items: null);
        Assert.False(Validator.TryValidateObject(model, context, results, validateAllProperties: true));
        return results;
    }

    private sealed class AttributeModel
    {
        [Display(Name = "Username")]
        [ClaimableUsername]
        public string Username { get; init; } = string.Empty;
    }

    private sealed class TooShortChecker : IChecker
    {
        public bool IsReserved(string? value) => true;
        public bool IsClaimable(string? value) => false;
        public Result Check(string? value) => Result.TooShort(value);
        public DetailedResult CheckDetailed(string? value, bool includeMessages = false) =>
            new DetailedResult(value, new[] { new Diagnostic(MatchKind.TooShort) });
    }
}
