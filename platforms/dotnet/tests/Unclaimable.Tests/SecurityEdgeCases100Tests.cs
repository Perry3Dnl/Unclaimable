using System;
using Xunit;

namespace Unclaimable.Tests;

public sealed class SecurityEdgeCases100Tests
{
    [Theory]
    [InlineData("blue\u0000garden")]
    [InlineData("blue\u202Egarden")]
    [InlineData("blue\u200Bgarden")]
    public void InvalidUnicodeAndControlSequencesAreRejectedAcrossResultApis(string input)
    {
        var checker = new Checker();
        Assert.True(checker.IsReserved(input));
        Assert.False(checker.IsClaimable(input));
        Assert.True(checker.Check(input).IsReserved);
        Assert.True(checker.CheckDetailed(input).IsReserved);
    }

    [Fact]
    public void UnpairedSurrogatesAreRejectedAcrossResultApis()
    {
        // Construct malformed UTF-16 at execution time. xUnit theory discovery
        // can replace unpaired surrogates with U+FFFD and collapse distinct test IDs.
        foreach (var surrogate in new[] { (char)0xD800, (char)0xDC00 })
        {
            foreach (var input in new[] { surrogate.ToString(), "blue" + surrogate + "garden" })
            {
                var checker = new Checker();
                Assert.True(checker.IsReserved(input));
                Assert.False(checker.IsClaimable(input));
                Assert.Equal(MatchKind.InvalidCharacters, checker.Check(input).MatchKind);
                Assert.True(checker.CheckDetailed(input).IsReserved);
            }
        }
    }

    [Fact]
    public void LongInputDoesNotBypassConfiguredLengthLimit()
    {
        var checker = new Checker();
        var input = new string('a', 4096);
        Assert.True(checker.IsReserved(input));
        Assert.Equal(MatchKind.TooLong, checker.Check(input).MatchKind);
        Assert.True(checker.CheckDetailed(input).IsReserved);
    }

    [Theory]
    [InlineData("bluegarden")]
    [InlineData("saddick")]
    [InlineData("bluegardén")]
    [InlineData("аdmin")]
    [InlineData("supportive")]
    [InlineData("a\u0301b")]
    public void CheckApisAgreeForUnicodeAndMixedScriptInputs(string input)
    {
        var checker = new Checker();
        Assert.Equal(checker.IsReserved(input), !checker.IsClaimable(input));
        Assert.Equal(checker.IsReserved(input), checker.Check(input).IsReserved);
        Assert.Equal(checker.IsReserved(input), checker.CheckDetailed(input).IsReserved);
    }

    [Fact]
    public void OptionalInspectorNeverAltersInvalidUserConfiguration()
    {
        var options = new Options
        {
            MaximumLength = 0,
            ObfuscationSensitivity = (ObfuscationSensitivity)12345
        };
        var first = PolicyConfigurationInspector.Inspect(options);
        var second = PolicyConfigurationInspector.Inspect(options);
        Assert.Equal(first.Count, second.Count);
        Assert.Equal(first[0], second[0]);
        Assert.Equal(0, options.MaximumLength);
        Assert.Equal((ObfuscationSensitivity)12345, options.ObfuscationSensitivity);
    }
}
