using Unclaimable.Extended;
using Xunit;

namespace Unclaimable.Tests;

public sealed class PartialCandidateIndex085Tests
{
    [Fact]
    public void IndexedPartialMatchKeepsLongestCandidatePriorityAndOriginalOffsets()
    {
        var options = new Options();
        options.Reserve("xylophyte", "long");
        options.Reserve("phyte", "short");
        var checker = new Checker(options);

        var result = checker.Check("prexylophytepost");
        Assert.True(result.IsReserved);
        Assert.Equal("long", result.Category);
        Assert.Equal(MatchKind.Partial, result.MatchKind);
        Assert.Equal(3, result.MatchStartIndex);
        Assert.Equal(9, result.MatchLength);
    }

    [Fact]
    public void IndexedPartialMatchFindsLaterUnsuppressedOccurrences()
    {
        var options = new Options
        {
            FalsePositiveSensitivity = FalsePositiveSensitivity.Medium
        };
        options.Reserve("fooz", "custom");
        var checker = new Checker(options);

        // A safe prefix is not permission to ignore a reserved custom identifier.
        Assert.Equal("custom", checker.Check("prefixfooz").Category);
        Assert.Equal("custom", checker.Check("foozsuffix").Category);
    }

    [Fact]
    public void IndexedPartialMatchPreservesCompactMatchingAndExactPreference()
    {
        var options = new Options();
        options.Reserve("xyza-bcde", "separator");
        var checker = new Checker(options);

        var result = checker.Check("prexyzabcdepost");
        Assert.True(result.IsReserved);
        Assert.Equal("separator", result.Category);
        Assert.Equal(MatchKind.Partial, result.MatchKind);
        Assert.Equal(3, result.MatchStartIndex);
    }

    [Fact]
    public void IndexedPartialMatchDoesNotAlterSingleCharacterOrUnicodeMatching()
    {
        var options = new Options { PartialMatchMinimumLength = 1 };
        options.Reserve("ꙮ", "unicode");
        options.Reserve("z", "single");
        var checker = new Checker(options);

        Assert.Equal("unicode", checker.Check("preꙮpost").Category);
        Assert.Equal("single", checker.Check("qqzqq").Category);
    }

    [Fact]
    public void ExtendedDataStillResolvesThroughCoreWithIndexedPartials()
    {
        var options = new Options();
        options.UseExtendedData();
        var checker = new Checker(options);

        Assert.Equal("slangprofanity", checker.Check("prefixpillocksuffix").Category);
    }
}
