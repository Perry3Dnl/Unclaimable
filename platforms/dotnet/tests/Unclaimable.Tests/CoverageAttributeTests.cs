using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Unclaimable.AspNetCore;
using Xunit;

namespace Unclaimable.Tests;

public sealed class CoverageAttributeTests
{
    private sealed class StubChecker : IChecker
    {
        private readonly Result _result;

        public StubChecker(Result result)
        {
            _result = result;
        }

        public bool IsReserved(string? value) => _result.IsReserved;
        public bool IsClaimable(string? value) => !_result.IsReserved;
        public Result Check(string? value) => _result;
        public DetailedResult CheckDetailed(string? value, bool includeMessages = false) =>
            throw new NotSupportedException();
    }

    [Fact]
    public void AttributeAcceptsNullAndRejectsNonStringValues()
    {
        var attribute = new ClaimableUsernameAttribute();
        using var provider = new ServiceCollection().BuildServiceProvider();
        var context = CreateContext(provider);

        Assert.Null(attribute.GetValidationResult(null, context));

        var invalid = attribute.GetValidationResult(42, context);
        Assert.NotNull(invalid);
        Assert.Equal("Username must be a string.", invalid!.ErrorMessage);
    }

    [Fact]
    public void AttributeAcceptsClaimableStringValues()
    {
        using var provider = new ServiceCollection()
            .AddSingleton<IChecker>(new StubChecker(Result.Allowed("ordinarycoverageuser")))
            .BuildServiceProvider();

        var result = new ClaimableUsernameAttribute()
            .GetValidationResult("ordinarycoverageuser", CreateContext(provider));

        Assert.Null(result);
    }

    [Fact]
    public void AttributeFallsBackToDefaultCheckerWhenNoCheckerIsRegistered()
    {
        var attribute = new ClaimableUsernameAttribute();
        using var provider = new ServiceCollection().BuildServiceProvider();

        var result = attribute.GetValidationResult("admin", CreateContext(provider));

        Assert.NotNull(result);
        Assert.Contains("reserved", result!.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuiltInMessagesCoverAllMatchKindsAndProfanity()
    {
        var kinds = new[]
        {
            MatchKind.Exact,
            MatchKind.Compact,
            MatchKind.Partial,
            MatchKind.Obfuscated,
            MatchKind.UnicodeConfusable,
            MatchKind.NumbersNotAllowed,
            MatchKind.InvalidCharacters,
            MatchKind.TooShort,
            MatchKind.TooLong,
            MatchKind.BlockedCharacter,
            MatchKind.LeadingSeparator,
            MatchKind.TrailingSeparator,
            MatchKind.NumericOnly,
            MatchKind.RepeatedPattern,
            MatchKind.SymbolOnly,
            MatchKind.AsciiArt,
            MatchKind.UppercaseOnly,
            MatchKind.CountryName,
            MatchKind.PopularCityName,
            MatchKind.None
        };

        foreach (var kind in kinds)
        {
            var result = new Result(
                true,
                "input",
                "matched",
                "custom",
                kind,
                offendingCharacterIndex: 1,
                offendingCharacter: "x",
                matchStartIndex: 0,
                matchLength: 1);

            using var provider = new ServiceCollection()
                .AddSingleton<IChecker>(new StubChecker(result))
                .BuildServiceProvider();

            var validation = new ClaimableUsernameAttribute()
                .GetValidationResult("input", CreateContext(provider));

            Assert.NotNull(validation);
            Assert.False(string.IsNullOrWhiteSpace(validation!.ErrorMessage));
        }

        var profanityResult = new Result(true, "word", "word", "profanity", MatchKind.Exact);
        using var profanityProvider = new ServiceCollection()
            .AddSingleton<IChecker>(new StubChecker(profanityResult))
            .BuildServiceProvider();

        var profanityValidation = new ClaimableUsernameAttribute()
            .GetValidationResult("word", CreateContext(profanityProvider));

        Assert.Equal("Username contains language that is not allowed.", profanityValidation!.ErrorMessage);
    }

    [Fact]
    public void ReasonSpecificMessagesCoverAllConfiguredProperties()
    {
        var options = new Options();
        options.Messages.Reserved = "reserved";
        options.Messages.Compact = "compact";
        options.Messages.Partial = "partial";
        options.Messages.Obfuscated = "obfuscated";
        options.Messages.UnicodeConfusable = "unicode";
        options.Messages.NumbersNotAllowed = "numbers";
        options.Messages.InvalidCharacters = "invalid";
        options.Messages.TooShort = "short";
        options.Messages.TooLong = "long";
        options.Messages.BlockedCharacter = "blocked";
        options.Messages.LeadingSeparator = "leading";
        options.Messages.TrailingSeparator = "trailing";
        options.Messages.Profanity = "profanity";
        options.Messages.NumericOnly = "numeric";
        options.Messages.RepeatedPattern = "repeated";
        options.Messages.SymbolOnly = "symbol";
        options.Messages.AsciiArt = "ascii";
        options.Messages.UppercaseOnly = "uppercase";
        options.Messages.CountryName = "country";
        options.Messages.PopularCityName = "city";

        var expectations = new Dictionary<MatchKind, string>
        {
            [MatchKind.Exact] = "reserved",
            [MatchKind.Compact] = "compact",
            [MatchKind.Partial] = "partial",
            [MatchKind.Obfuscated] = "obfuscated",
            [MatchKind.UnicodeConfusable] = "unicode",
            [MatchKind.NumbersNotAllowed] = "numbers",
            [MatchKind.InvalidCharacters] = "invalid",
            [MatchKind.TooShort] = "short",
            [MatchKind.TooLong] = "long",
            [MatchKind.BlockedCharacter] = "blocked",
            [MatchKind.LeadingSeparator] = "leading",
            [MatchKind.TrailingSeparator] = "trailing",
            [MatchKind.NumericOnly] = "numeric",
            [MatchKind.RepeatedPattern] = "repeated",
            [MatchKind.SymbolOnly] = "symbol",
            [MatchKind.AsciiArt] = "ascii",
            [MatchKind.UppercaseOnly] = "uppercase",
            [MatchKind.CountryName] = "country",
            [MatchKind.PopularCityName] = "city"
        };

        foreach (var pair in expectations)
        {
            var result = new Result(true, "input", "matched", "custom", pair.Key);
            using var provider = new ServiceCollection()
                .AddSingleton(options)
                .AddSingleton<IChecker>(new StubChecker(result))
                .BuildServiceProvider();

            var validation = new ClaimableUsernameAttribute()
                .GetValidationResult("input", CreateContext(provider));

            Assert.Equal(pair.Value, validation!.ErrorMessage);
        }

        var profanityResult = new Result(true, "word", "word", "profanity", MatchKind.Exact);
        using var profanityProvider = new ServiceCollection()
            .AddSingleton(options)
            .AddSingleton<IChecker>(new StubChecker(profanityResult))
            .BuildServiceProvider();

        var profanityValidation = new ClaimableUsernameAttribute()
            .GetValidationResult("word", CreateContext(profanityProvider));

        Assert.Equal("profanity", profanityValidation!.ErrorMessage);
    }

    [Fact]
    public void UnknownReasonFallsBackToGlobalValidationMessageWhenOptionsAreRegistered()
    {
        var options = new Options { ValidationMessage = "fallback" };
        var result = new Result(true, "input", null, null, MatchKind.None);
        using var provider = new ServiceCollection()
            .AddSingleton(options)
            .AddSingleton<IChecker>(new StubChecker(result))
            .BuildServiceProvider();

        var validation = new ClaimableUsernameAttribute()
            .GetValidationResult("input", CreateContext(provider));

        Assert.Equal("fallback", validation!.ErrorMessage);
    }

    [Fact]
    public void AttributeExpandsEverySupportedMessagePlaceholder()
    {
        var options = new Options
        {
            MinimumLength = 4,
            MaximumLength = 20
        };

        var result = new Result(
            true,
            "input",
            "matched",
            "custom",
            MatchKind.Exact,
            offendingCharacterIndex: 2,
            offendingCharacter: "x",
            matchStartIndex: 0,
            matchLength: 3);

        using var provider = new ServiceCollection()
            .AddSingleton(options)
            .AddSingleton<IChecker>(new StubChecker(result))
            .BuildServiceProvider();

        var attribute = new ClaimableUsernameAttribute
        {
            ErrorMessage = "{FieldName}|{MatchedValue}|{Category}|{Character}|{Index}|{Length}|{MinimumLength}|{MaximumLength}"
        };

        var validation = attribute.GetValidationResult("input", CreateContext(provider));

        Assert.Equal("Username|matched|custom|x|2|5|4|20", validation!.ErrorMessage);
    }

    [Fact]
    public void AttributeUsesActualLengthLimitsFromCheckerResults()
    {
        var shortOptions = new Options { MinimumLength = 8 };
        var shortChecker = new Checker(shortOptions);
        using var shortProvider = new ServiceCollection()
            .AddSingleton(shortOptions)
            .AddSingleton<IChecker>(shortChecker)
            .BuildServiceProvider();

        var shortAttribute = new ClaimableUsernameAttribute
        {
            ErrorMessage = "min={MinimumLength};max={MaximumLength}"
        };
        var shortValidation = shortAttribute.GetValidationResult("abc", CreateContext(shortProvider));
        Assert.Equal("min=8;max=32", shortValidation!.ErrorMessage);

        var longOptions = new Options { MaximumLength = 4 };
        var longChecker = new Checker(longOptions);
        using var longProvider = new ServiceCollection()
            .AddSingleton(longOptions)
            .AddSingleton<IChecker>(longChecker)
            .BuildServiceProvider();

        var longAttribute = new ClaimableUsernameAttribute
        {
            ErrorMessage = "min={MinimumLength};max={MaximumLength}"
        };
        var longValidation = longAttribute.GetValidationResult("abcdefgh", CreateContext(longProvider));
        Assert.Equal("min=3;max=4", longValidation!.ErrorMessage);
    }

    private static ValidationContext CreateContext(IServiceProvider provider) =>
        new ValidationContext(new object(), provider, items: null)
        {
            DisplayName = "Username"
        };
}
