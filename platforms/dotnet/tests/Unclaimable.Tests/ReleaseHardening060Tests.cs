using System.Text.Json;
using Xunit;

namespace Unclaimable.Tests;

public sealed class ReleaseHardening060Tests
{
    [Fact]
    public void KnownSafeCorpusIsBroadAndContainsNoNormalizedDuplicates()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "safe-usernames.json");
        var values = JsonSerializer.Deserialize<string[]>(File.ReadAllText(path))
                     ?? throw new InvalidOperationException("Safe username corpus could not be loaded.");

        Assert.True(values.Length >= 250, $"Expected at least 250 known-safe usernames, found {values.Length}.");

        var normalized = values
            .Select(value => value.Trim().Normalize().ToLowerInvariant())
            .ToArray();

        Assert.Equal(normalized.Length, normalized.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void LengthRulesUseUtf16CodeUnits()
    {
        const string value = "ab\U00010428"; // Three Unicode scalars, four UTF-16 code units.
        Assert.Equal(4, value.Length);

        var atLimit = new Checker(new Options
        {
            MinimumLength = 4,
            MaximumLength = 4
        }).Check(value);

        Assert.True(atLimit.IsClaimable);

        var tooShort = new Checker(new Options
        {
            MinimumLength = 5,
            MaximumLength = 10
        }).Check(value);

        Assert.True(tooShort.IsReserved);
        Assert.Equal(MatchKind.TooShort, tooShort.MatchKind);
        Assert.Equal(5, tooShort.LengthLimit);

        var tooLong = new Checker(new Options
        {
            MinimumLength = 1,
            MaximumLength = 3
        }).Check(value);

        Assert.True(tooLong.IsReserved);
        Assert.Equal(MatchKind.TooLong, tooLong.MatchKind);
        Assert.Equal(3, tooLong.LengthLimit);
    }

    [Fact]
    public void ObfuscationCandidateCapKeepsDeterministicEarlyBranches()
    {
        var options = new Options
        {
            AllowNumbers = true,
            MinimumLength = 1
        };
        options.Reserve("iiiiii", ReservedMatchMode.Default);

        var result = new Checker(options).Check("111111");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Obfuscated, result.MatchKind);
        Assert.Equal("iiiiii", result.MatchedValue);
    }

    [Fact]
    public void ObfuscationCandidateCapIsBoundedRatherThanExhaustive()
    {
        var options = new Options
        {
            AllowNumbers = true,
            MinimumLength = 1
        };
        options.Reserve("liiiii", ReservedMatchMode.Default);

        // Each '1' can become i or l. Six ambiguous positions imply 64 possible
        // combinations, but the implementation deliberately retains at most 32.
        // Enumeration is deterministic and later branches are not guaranteed to be checked.
        var result = new Checker(options).Check("111111");

        Assert.True(result.IsClaimable);
    }
}
