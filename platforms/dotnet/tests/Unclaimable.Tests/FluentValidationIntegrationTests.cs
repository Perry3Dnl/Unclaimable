using FluentValidation;
using Unclaimable.FluentValidation;
using Xunit;

namespace Unclaimable.Tests;

public sealed class FluentValidationIntegrationTests
{
    private sealed class Signup
    {
        public string? Username { get; set; }
    }

    private sealed class DefaultValidator : AbstractValidator<Signup>
    {
        public DefaultValidator()
        {
            RuleFor(x => x.Username).NotEmpty().MustBeClaimable();
        }
    }

    private sealed class ConfiguredValidator : AbstractValidator<Signup>
    {
        public ConfiguredValidator(IChecker checker)
        {
            RuleFor(x => x.Username).MustNotBeReserved(checker);
        }
    }

    [Fact]
    public void DefaultRuleAcceptsOrdinaryNameAndRejectsReservedName()
    {
        var validator = new DefaultValidator();

        Assert.True(validator.Validate(new Signup { Username = "riverwalker47" }).IsValid);
        var failure = Assert.Single(validator.Validate(new Signup { Username = "admin" }).Errors);
        Assert.Contains("claimable", failure.ErrorMessage);
    }

    [Fact]
    public void RequiredFieldRemainsSeparate()
    {
        var checker = new Checker();
        Assert.True(new ConfiguredValidator(checker).Validate(new Signup()).IsValid);
        Assert.False(new DefaultValidator().Validate(new Signup()).IsValid);
    }

    [Fact]
    public void ConfiguredCheckerControlsReservationsAndProvidesReason()
    {
        var options = new Options();
        options.AdditionalReserved.Add("localbrandexample");
        var validator = new ConfiguredValidator(new Checker(options));

        var failure = Assert.Single(validator.Validate(new Signup { Username = "localbrandexample" }).Errors);
        Assert.Contains("Exact", failure.ErrorMessage);
        Assert.True(validator.Validate(new Signup { Username = "riverwalker47" }).IsValid);
    }

    [Fact]
    public void NullCheckerAndBuilderFailAtConfiguration()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ClaimableRuleExtensions.MustBeClaimable<Signup>(null!, Checker.Default));
        Assert.Throws<ArgumentNullException>(() =>
            new InlineValidator<Signup>().RuleFor(x => x.Username).MustBeClaimable(null!));
    }
}
