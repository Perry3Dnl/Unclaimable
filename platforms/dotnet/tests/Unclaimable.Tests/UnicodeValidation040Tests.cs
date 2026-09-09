using Xunit;

namespace Unclaimable.Tests;

public sealed class UnicodeValidation040Tests
{
    [Fact]
    public void NewSecurityOptionsAreEnabledByDefault()
    {
        var options = new Options();

        Assert.True(options.RejectInvisibleOnlyIdentifiers);
        Assert.True(options.RejectControlCharacters);
        Assert.True(options.RejectFormatCharacters);
        Assert.True(options.ConsistentCompactMatching);
    }

    [Fact]
    public void MalformedUtf16IsRejectedWithoutThrowingOrConsultingPolicy()
    {
        var high = ((char)0xD800).ToString();
        var low = ((char)0xDC00).ToString();
        var cases = new[]
        {
            (Value: high + "abc", Index: 0),
            (Value: "ab" + low + "c", Index: 2),
            (Value: "ab" + high + "c", Index: 2)
        };

        var checker = new Checker(new Options(), new ThrowingPolicy());

        foreach (var testCase in cases)
        {
            var result = checker.Check(testCase.Value);

            Assert.True(result.IsReserved);
            Assert.False(result.IsClaimable);
            Assert.Equal(MatchKind.InvalidCharacters, result.MatchKind);
            Assert.Equal(testCase.Index, result.OffendingCharacterIndex);
            Assert.False(checker.IsClaimable(testCase.Value));
        }
    }

    [Fact]
    public void DetailedMalformedUtf16SkipsReservedNameNormalization()
    {
        var value = "admin" + (char)0xD800;
        var checker = new Checker(new Options(), new ThrowingPolicy());

        var result = checker.CheckDetailed(value, includeMessages: true);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(MatchKind.InvalidCharacters, diagnostic.Kind);
        Assert.Equal(5, diagnostic.OffendingCharacterIndex);
        Assert.Null(diagnostic.MatchedValue);
        Assert.Contains("UTF-16", diagnostic.Message);
    }

    [Fact]
    public void ValidSurrogatePairRemainsValidUnicodeInput()
    {
        var checker = new Checker(new Options());

        var exception = Record.Exception(() => checker.Check("ab😀cd"));

        Assert.Null(exception);
        Assert.NotEqual(MatchKind.InvalidCharacters, checker.Check("ab😀cd").MatchKind);
    }

    [Fact]
    public void FormatCharactersAreRejectedByDefaultAndCanBeAllowedExplicitly()
    {
        const string value = "ab\u200Dcd";

        var strictResult = new Checker(new Options()).Check(value);
        Assert.Equal(MatchKind.InvalidCharacters, strictResult.MatchKind);
        Assert.Equal(2, strictResult.OffendingCharacterIndex);
        Assert.Equal("\u200D", strictResult.OffendingCharacter);

        var relaxed = new Checker(new Options { RejectFormatCharacters = false });
        Assert.True(relaxed.IsClaimable(value));
    }

    [Fact]
    public void ControlCharactersAreRejectedByDefaultAndCanBeAllowedExplicitly()
    {
        const string value = "ab\u0001cd";

        var strictResult = new Checker(new Options()).Check(value);
        Assert.Equal(MatchKind.InvalidCharacters, strictResult.MatchKind);
        Assert.Equal(2, strictResult.OffendingCharacterIndex);

        var relaxed = new Checker(new Options { RejectControlCharacters = false });
        Assert.True(relaxed.IsClaimable(value));
    }

    [Theory]
    [InlineData("\u0301\u0301\u0301")]
    [InlineData("\u200D\u200C\u2060")]
    public void InvisibleOnlyIdentifiersAreRejectedByDefaultAndCanBeAllowedExplicitly(string value)
    {
        Assert.Equal(MatchKind.InvalidCharacters, new Checker(new Options()).Check(value).MatchKind);

        var relaxed = new Checker(new Options
        {
            RejectInvisibleOnlyIdentifiers = false,
            RejectControlCharacters = false,
            RejectFormatCharacters = false
        });

        Assert.True(relaxed.IsClaimable(value));
    }

    [Fact]
    public void InvisibleOnlyApproximationAllowsVisibleContentWithCombiningMarks()
    {
        var checker = new Checker(new Options());

        Assert.True(checker.IsClaimable("a\u0301bc"));
    }

    [Fact]
    public void UnicodeStrictnessOptionsAreCapturedAtConstruction()
    {
        var options = new Options();
        var checker = new Checker(options);
        options.RejectFormatCharacters = false;

        Assert.Equal(MatchKind.InvalidCharacters, checker.Check("ab\u200Dcd").MatchKind);
    }

    private sealed class ThrowingPolicy : IPolicy
    {
        public IReadOnlyCollection<string> BlockedCharacters => Array.Empty<string>();
        public void BlockCharacter(string value) => throw new InvalidOperationException("Policy should not be used.");
        public void BlockCharacters(params string[] values) => throw new InvalidOperationException("Policy should not be used.");
        public bool AllowCharacter(string value) => throw new InvalidOperationException("Policy should not be used.");
        public void AllowCharacters(params string[] values) => throw new InvalidOperationException("Policy should not be used.");
        public bool IsCharacterBlocked(string value) => throw new InvalidOperationException("Policy should not be used.");
        public bool IsCharacterExplicitlyAllowed(string value) => throw new InvalidOperationException("Policy should not be used.");
    }
}
