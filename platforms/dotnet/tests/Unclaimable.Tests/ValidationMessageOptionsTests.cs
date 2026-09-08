using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Unclaimable.AspNetCore;
using Xunit;

namespace Unclaimable.Tests;

public sealed class ValidationMessageOptionsTests
{
    private sealed class SignupModel
    {
        [Display(Name = "Username")]
        [ClaimableUsername]
        public string Username { get; init; } = string.Empty;
    }

    private sealed class AttributeOverrideModel
    {
        [Display(Name = "Username")]
        [ClaimableUsername(ErrorMessage = "Attribute wins for {FieldName}.")]
        public string Username { get; init; } = string.Empty;
    }

    [Fact]
    public void ExactReservedMessageCanBeConfiguredPerReason()
    {
        using var provider = new ServiceCollection()
            .AddUnclaimable(options =>
            {
                options.ValidationMessage = "Global fallback.";
                options.Messages.Reserved = "{FieldName} '{MatchedValue}' is reserved ({Category}).";
            })
            .BuildServiceProvider();

        var error = Validate("admin", provider);

        Assert.Equal("Username 'admin' is reserved (roles).", error.ErrorMessage);
    }

    [Fact]
    public void StrictPartialMessageCanBeConfiguredPerReason()
    {
        using var provider = new ServiceCollection()
            .AddUnclaimable(options =>
            {
                options.Messages.Partial = "{FieldName} contains protected value '{MatchedValue}'.";
            })
            .BuildServiceProvider();

        var error = Validate("supportive", provider);

        Assert.Equal("Username contains protected value 'support'.", error.ErrorMessage);
    }

    [Fact]
    public void NumberPolicyMessageSupportsCharacterAndIndexPlaceholders()
    {
        using var provider = new ServiceCollection()
            .AddUnclaimable(options =>
            {
                options.Messages.NumbersNotAllowed = "{FieldName}: '{Character}' is not allowed at index {Index}.";
            })
            .BuildServiceProvider();

        var error = Validate("user2", provider);

        Assert.Equal("Username: '2' is not allowed at index 4.", error.ErrorMessage);
    }

    [Fact]
    public void ProfanityMessageTakesPrecedenceOverMatchKindMessage()
    {
        using var provider = new ServiceCollection()
            .AddUnclaimable(options =>
            {
                options.Messages.Reserved = "Reserved fallback.";
                options.Messages.Profanity = "{FieldName} contains blocked language.";
            })
            .BuildServiceProvider();

        var error = Validate("fuck", provider);

        Assert.Equal("Username contains blocked language.", error.ErrorMessage);
    }

    [Fact]
    public void GlobalValidationMessageIsUsedWhenReasonSpecificMessageIsMissing()
    {
        using var provider = new ServiceCollection()
            .AddUnclaimable(options =>
            {
                options.ValidationMessage = "{FieldName} is unavailable.";
            })
            .BuildServiceProvider();

        var error = Validate("supportive", provider);

        Assert.Equal("Username is unavailable.", error.ErrorMessage);
    }

    [Fact]
    public void BuiltInReasonMessageIsUsedWhenDeveloperConfiguresNothing()
    {
        using var provider = new ServiceCollection()
            .AddUnclaimable()
            .BuildServiceProvider();

        var error = Validate("supportive", provider);

        Assert.Equal("Username contains a reserved name and cannot be claimed.", error.ErrorMessage);
    }

    [Fact]
    public void StructuralMessagesSupportLengthPlaceholders()
    {
        using var provider = new ServiceCollection()
            .AddUnclaimable(options =>
            {
                options.MinimumLength = 4;
                options.Messages.TooShort = "{FieldName} has {Length} characters; minimum is {MinimumLength}.";
            })
            .BuildServiceProvider();

        var error = Validate("abc", provider);

        Assert.Equal("Username has 3 characters; minimum is 4.", error.ErrorMessage);
    }

    [Fact]
    public void AttributeMessageStillHasHighestPrecedence()
    {
        using var provider = new ServiceCollection()
            .AddUnclaimable(options =>
            {
                options.Messages.Reserved = "Reason-specific message.";
                options.ValidationMessage = "Global message.";
            })
            .BuildServiceProvider();

        var results = new List<ValidationResult>();
        var model = new AttributeOverrideModel { Username = "admin" };
        var context = new ValidationContext(model, provider, items: null);

        var isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);

        Assert.False(isValid);
        var error = Assert.Single(results);
        Assert.Equal("Attribute wins for Username.", error.ErrorMessage);
    }

    private static ValidationResult Validate(string username, IServiceProvider provider)
    {
        var results = new List<ValidationResult>();
        var model = new SignupModel { Username = username };
        var context = new ValidationContext(model, provider, items: null);

        var isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);

        Assert.False(isValid);
        return Assert.Single(results);
    }
}
