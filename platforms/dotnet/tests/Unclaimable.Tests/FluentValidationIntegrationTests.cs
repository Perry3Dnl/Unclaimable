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

    private sealed class DefaultAliasValidator : AbstractValidator<Signup>
    {
        public DefaultAliasValidator()
        {
            RuleFor(x => x.Username).MustNotBeReserved();
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
        Assert.False(new DefaultAliasValidator().Validate(new Signup { Username = "admin" }).IsValid);
        Assert.Contains("TooShort", Assert.Single(new DefaultAliasValidator()
            .Validate(new Signup { Username = "ab" }).Errors).ErrorMessage);
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

    [Theory]
    [InlineData("saddick")]
    [InlineData("dickinson")]
    [InlineData("admin")]
    public void AdapterAgreesWithCoreCheckerOnEmbeddedAndExactMatches(string username)
    {
        var checker = new Checker();
        var validator = new ConfiguredValidator(checker);
        var result = checker.Check(username);
        var validation = validator.Validate(new Signup { Username = username });

        Assert.Equal(result.IsClaimable, validation.IsValid);
        Assert.False(validation.IsValid);
    }

    [Fact]
    public void AdapterHonorsScopedExceptionsAndOtherReservedMatches()
    {
        const string username = "dickzorb";
        var options = new Options();
        options.AdditionalReserved.Add("zorb");
        options.AllowIdentifierForRule(username, Rule.Profanity);
        var checker = new Checker(options);
        var validator = new ConfiguredValidator(checker);

        Assert.Equal("custom", checker.Check(username).Category);
        Assert.False(validator.Validate(new Signup { Username = username }).IsValid);
    }
    private sealed class MultipleIdentifiers
    {
        public string? Username { get; set; }
        public string? DisplayName { get; set; }
    }

    private sealed class MultipleIdentifiersValidator : AbstractValidator<MultipleIdentifiers>
    {
        public MultipleIdentifiersValidator(IChecker checker)
        {
            RuleFor(x => x.Username).MustBeClaimable(checker)
                .WithMessage("{PropertyName}:{MatchKind}:{Category}");
            RuleFor(x => x.DisplayName).MustNotBeReserved(checker)
                .WithMessage("{PropertyName}:{MatchKind}:{Category}");
        }
    }

    [Fact]
    public async Task AsyncValidationMatchesSynchronousValidation()
    {
        var validator = new DefaultValidator();
        foreach (var username in new string?[] { null, "", "riverwalker47", "admin", "saddick" })
        {
            var model = new Signup { Username = username };
            var sync = validator.Validate(model);
            var asyncResult = await validator.ValidateAsync(model);

            Assert.Equal(sync.IsValid, asyncResult.IsValid);
            Assert.Equal(
                sync.Errors.Select(error => (error.PropertyName, error.ErrorMessage)),
                asyncResult.Errors.Select(error => (error.PropertyName, error.ErrorMessage)));
        }
    }

    [Fact]
    public async Task MultiplePropertiesReportIndependentFailuresAndResolvedPlaceholders()
    {
        var options = new Options();
        options.Reserve("localbrandexample", "customcategory", ReservedMatchMode.Exact);
        var validator = new MultipleIdentifiersValidator(new Checker(options));
        var invalid = new MultipleIdentifiers
        {
            Username = "admin",
            DisplayName = "localbrandexample"
        };

        var validation = await validator.ValidateAsync(invalid);
        Assert.False(validation.IsValid);
        Assert.Equal(2, validation.Errors.Count);
        Assert.Contains(validation.Errors, error =>
            error.PropertyName == "Username" &&
            error.ErrorMessage.Contains("Username:") &&
            !error.ErrorMessage.Contains("{MatchKind}") &&
            !error.ErrorMessage.Contains("{Category}"));
        Assert.Contains(validation.Errors, error =>
            error.PropertyName == "DisplayName" &&
            error.ErrorMessage.Contains("DisplayName:Exact:customcategory"));

        Assert.True((await validator.ValidateAsync(new MultipleIdentifiers
        {
            Username = "riverwalker47",
            DisplayName = null
        })).IsValid);
    }

    [Theory]
    [InlineData("dickzorb")]
    [InlineData("zorbdick")]
    public async Task ScopedExceptionDoesNotHideIndependentPartialMatchAsync(string username)
    {
        var options = new Options();
        options.AdditionalReserved.Add("zorb");
        options.AllowIdentifierForRule(username, Rule.Profanity);
        var checker = new Checker(options);
        var validator = new ConfiguredValidator(checker);

        var result = checker.Check(username);
        var validation = await validator.ValidateAsync(new Signup { Username = username });

        Assert.True(result.IsReserved);
        Assert.Equal("custom", result.Category);
        Assert.False(validation.IsValid);
        Assert.Equal("Username", Assert.Single(validation.Errors).PropertyName);
    }

    [Fact]
    public async Task ScopedExceptionPermitsOnlyTheExemptedDetectionAsync()
    {
        const string username = "saddick";
        var options = new Options();
        options.AllowIdentifierForRule(username, Rule.Profanity);
        var validator = new ConfiguredValidator(new Checker(options));

        Assert.True((await validator.ValidateAsync(new Signup { Username = username })).IsValid);
        Assert.False((await new ConfiguredValidator(new Checker())
            .ValidateAsync(new Signup { Username = username })).IsValid);
    }

    [Fact]
    public async Task AliasAndDirectRuleAreEquivalentForAsyncValidation()
    {
        var checker = new Checker();
        var direct = new InlineValidator<Signup>();
        direct.RuleFor(x => x.Username).MustBeClaimable(checker);
        var alias = new ConfiguredValidator(checker);

        foreach (var username in new string?[] { null, "riverwalker47", "admin", "saddick" })
        {
            var model = new Signup { Username = username };
            var directResult = await direct.ValidateAsync(model);
            var aliasResult = await alias.ValidateAsync(model);
            Assert.Equal(directResult.IsValid, aliasResult.IsValid);
            Assert.Equal(directResult.Errors.Select(x => x.ErrorMessage),
                aliasResult.Errors.Select(x => x.ErrorMessage));
        }
    }

}
