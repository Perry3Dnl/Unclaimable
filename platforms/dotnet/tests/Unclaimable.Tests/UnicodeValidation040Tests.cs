using Xunit;

namespace Unclaimable.Tests;

public sealed class UnicodeValidation040Tests
{
    [Fact]
    public void NewStrictnessOptionsAreDisabledByDefault()
    {
        var options = new Options();

        Assert.False(options.RejectInvisibleOnlyIdentifiers);
        Assert.False(options.RejectControlCharacters);
        Assert.False(options.RejectFormatCharacters);
        Assert.False(options.ConsistentCompactMatching);
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
    public void FormatCharacterRejectionIsOptIn()
    {
        const string value = "ab\u200Dcd";

        Assert.True(new Checker(new Options()).IsClaimable(value));

        var result = new Checker(new Options { RejectFormatCharacters = true }).Check(value);
        Assert.Equal(MatchKind.InvalidCharacters, result.MatchKind);
        Assert.Equal(2, result.OffendingCharacterIndex);
        Assert.Equal("\u200D", result.OffendingCharacter);
    }

    [Fact]
    public void ControlCharacterRejectionIsOptIn()
    {
        const string value = "ab\u0001cd";

        Assert.True(new Checker(new Options()).IsClaimable(value));

        var result = new Checker(new Options { RejectControlCharacters = true }).Check(value);
        Assert.Equal(MatchKind.InvalidCharacters, result.MatchKind);
        Assert.Equal(2, result.OffendingCharacterIndex);
    }

    [Theory]
    [InlineData("\u0301\u0301\u0301")]
    [InlineData("\u200D\u200C\u2060")]
    public void InvisibleOnlyRejectionIsOptIn(string value)
    {
        Assert.True(new Checker(new Options()).IsClaimable(value));

        var result = new Checker(new Options { RejectInvisibleOnlyIdentifiers = true }).Check(value);
        Assert.Equal(MatchKind.InvalidCharacters, result.MatchKind);
    }

    [Fact]
    public void InvisibleOnlyApproximationAllowsVisibleContentWithCombiningMarks()
    {
        var checker = new Checker(new Options { RejectInvisibleOnlyIdentifiers = true });

        Assert.True(checker.IsClaimable("a\u0301bc"));
    }

    [Fact]
    public void UnicodeStrictnessOptionsAreCapturedAtConstruction()
    {
        var options = new Options();
        var checker = new Checker(options);
        options.RejectFormatCharacters = true;

        Assert.True(checker.IsClaimable("ab\u200Dcd"));
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
