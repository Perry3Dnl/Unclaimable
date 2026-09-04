using Xunit;

namespace Unclaimable.Tests;

public sealed class UnclaimableCheckerTests
{
    [Theory]
    [InlineData("admin")]
    [InlineData(" ADMIN ")]
    [InlineData("moderator")]
    [InlineData("support")]
    [InlineData("system")]
    public void BuiltInReservedNamesAreRejected(string value)
    {
        Assert.True(UnclaimableChecker.Default.IsReserved(value));
        Assert.False(UnclaimableChecker.Default.IsClaimable(value));
    }

    [Theory]
    [InlineData("apple")]
    [InlineData("Microsoft")]
    [InlineData("LINUX")]
    [InlineData("github")]
    public void TechnologyNamesAreRejected(string value)
    {
        var result = UnclaimableChecker.Default.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal("technology", result.Category);
    }

    [Theory]
    [InlineData("nike")]
    [InlineData("Adidas")]
    [InlineData("coca-cola")]
    [InlineData("Louis_Vuitton")]
    public void BrandNamesAreRejected(string value)
    {
        var result = UnclaimableChecker.Default.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal("brands", result.Category);
    }

    [Theory]
    [InlineData("customer-service")]
    [InlineData("customer_service")]
    [InlineData("customer.service")]
    [InlineData("customer service")]
    public void CompactMatchingBlocksSeparatorVariants(string value)
    {
        var result = UnclaimableChecker.Default.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal("support", result.Category);
    }

    [Theory]
    [InlineData("N1ke", "nike", "brands")]
    [InlineData("N1k3", "nike", "brands")]
    [InlineData("M1crosoft", "microsoft", "technology")]
    [InlineData("G00gle", "google", "technology")]
    [InlineData("@pple", "apple", "technology")]
    [InlineData("app1e", "apple", "technology")]
    [InlineData("r00t", "root", "roles")]
    [InlineData("c0ca-c0la", "coca cola", "brands")]
    public void ObfuscationMatchingBlocksCommonLeetspeak(
        string value,
        string matchedValue,
        string category)
    {
        var result = UnclaimableChecker.Default.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(matchedValue, result.MatchedValue);
        Assert.Equal(category, result.Category);
        Assert.Equal(UnclaimableMatchKind.Obfuscated, result.MatchKind);
    }

    [Theory]
    [InlineData("administrator2")]
    [InlineData("supportive")]
    [InlineData("systematic")]
    [InlineData("ordinary-user")]
    [InlineData("ordinary123")]
    [InlineData("apples")]
    [InlineData("nikee")]
    public void SimilarButDifferentNamesRemainClaimableByDefault(string value)
    {
        Assert.True(UnclaimableChecker.Default.IsClaimable(value));
    }

    [Theory]
    [InlineData("administrator2", "administrator")]
    [InlineData("old-admin", "admin")]
    [InlineData("admin-old", "admin")]
    [InlineData("supportive", "support")]
    [InlineData("apples", "apple")]
    [InlineData("nikee", "nike")]
    [InlineData("old-N1k3", "nike")]
    public void PartialMatchingCanRejectEmbeddedReservedNames(string value, string expectedMatch)
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            PartialMatching = true
        });

        var result = checker.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(expectedMatch, result.MatchedValue);
        Assert.Equal(UnclaimableMatchKind.Partial, result.MatchKind);
        Assert.NotNull(result.MatchStartIndex);
        Assert.True(result.MatchLength >= 4);
    }

    [Fact]
    public void PartialMatchingIgnoresShortReservedValuesByDefault()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            PartialMatching = true
        });

        Assert.True(checker.IsReserved("api"));
        Assert.True(checker.IsClaimable("rapid"));
        Assert.True(checker.IsClaimable("api123"));
    }

    [Fact]
    public void PartialMinimumLengthCanBeLoweredExplicitly()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            PartialMatching = true,
            PartialMatchMinimumLength = 3
        });

        Assert.True(checker.IsReserved("rapid"));
        Assert.Equal("api", checker.Check("rapid").MatchedValue);
    }

    [Fact]
    public void NumbersCanBeRejectedBeforeReservedNameMatching()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            AllowNumbers = false,
            PartialMatching = true
        });

        var result = checker.Check("old-admin2");

        Assert.True(result.IsReserved);
        Assert.Equal(UnclaimableMatchKind.NumbersNotAllowed, result.MatchKind);
        Assert.Equal(9, result.OffendingCharacterIndex);
        Assert.Equal("2", result.OffendingCharacter);
        Assert.Null(result.MatchedValue);
    }

    [Fact]
    public void NumberPolicyRecognizesUnicodeDecimalDigits()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            AllowNumbers = false
        });

        var result = checker.Check("user\u0661");

        Assert.True(result.IsReserved);
        Assert.Equal(UnclaimableMatchKind.NumbersNotAllowed, result.MatchKind);
        Assert.Equal("\u0661", result.OffendingCharacter);
    }

    [Fact]
    public void DetailedCheckCollectsPolicyAndReservedNameDiagnostics()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            AllowNumbers = false,
            PartialMatching = true
        });

        var result = checker.CheckDetailed("old-admin2", includeMessages: true);

        Assert.True(result.IsReserved);
        Assert.False(result.IsClaimable);
        Assert.Equal(10, result.InputLength);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Kind == UnclaimableMatchKind.NumbersNotAllowed
            && diagnostic.OffendingCharacterIndex == 9
            && diagnostic.OffendingCharacter == "2"
            && !string.IsNullOrWhiteSpace(diagnostic.Message));
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Kind == UnclaimableMatchKind.Partial
            && diagnostic.MatchedValue == "admin"
            && diagnostic.MatchStartIndex == 4
            && diagnostic.MatchLength == 5
            && !string.IsNullOrWhiteSpace(diagnostic.Message));
    }

    [Fact]
    public void DetailedCheckCanReturnMachineReadableDiagnosticsWithoutMessages()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            AllowNumbers = false,
            PartialMatching = true
        });

        var result = checker.CheckDetailed("old-admin2");

        Assert.NotEmpty(result.Diagnostics);
        Assert.All(result.Diagnostics, diagnostic => Assert.Null(diagnostic.Message));
    }

    [Fact]
    public void ApplicationSpecificNamesCanBeAddedWithoutChangingGlobalData()
    {
        var options = new UnclaimableOptions();
        options.AdditionalReserved.Add("examplebrand");
        options.AdditionalReserved.Add("internalbot");

        var checker = new UnclaimableChecker(options);

        Assert.True(checker.IsReserved("ExampleBrand"));
        Assert.True(checker.IsReserved("internal-bot"));
        Assert.Equal("custom", checker.Check("examplebrand").Category);
    }

    [Fact]
    public void CompactMatchingCanBeDisabled()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            CompactMatching = false
        });

        Assert.True(checker.IsReserved("customer service"));
        Assert.False(checker.IsReserved("customer-service"));
    }

    [Fact]
    public void ObfuscationMatchingCanBeDisabled()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            ObfuscationMatching = false
        });

        Assert.True(checker.IsReserved("nike"));
        Assert.False(checker.IsReserved("N1k3"));
    }
}
