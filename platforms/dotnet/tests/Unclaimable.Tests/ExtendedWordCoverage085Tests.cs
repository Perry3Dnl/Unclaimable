using System.Text.Json;
using Unclaimable.Extended;
using Xunit;

namespace Unclaimable.Tests;

/// <summary>
/// Every language-specific Extended slang/profanity entry must be detected as
/// a complete identifier and when concatenated with an otherwise safe word.
/// This is deliberately a behavioral contract rather than a data-count test.
/// </summary>
public sealed class ExtendedWordCoverage085Tests
{
    private const string SafeWord = "orchid";
    private static readonly Lazy<Checker> ExtendedChecker = new(() =>
    {
        var options = new Options { MaximumLength = 512 };
        options.UseExtendedData();
        return new Checker(options);
    });

    // One shared checker keeps the generated test cases reasonably inexpensive.
    // The identifier is checked with all Extended categories enabled, as consumers
    // commonly configure it. Each row contains the original language and word.
    public static IEnumerable<object[]> AllLanguageWords()
    {
        var assembly = typeof(ExtendedData).Assembly;
        foreach (var resourceName in assembly.GetManifestResourceNames()
                     .Where(name => name.StartsWith("Unclaimable.Extended.Data.slangprofanity-", StringComparison.Ordinal)
                                    && name.EndsWith(".json", StringComparison.Ordinal))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Missing resource {resourceName}");
            using var json = JsonDocument.Parse(stream);
            var language = resourceName["Unclaimable.Extended.Data.slangprofanity-".Length..^".json".Length];
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in json.RootElement.GetProperty("values").EnumerateArray())
            {
                var word = item.GetString();
                if (!string.IsNullOrWhiteSpace(word) && unique.Add(word.Normalize().ToLowerInvariant()))
                    yield return new object[] { language, word };
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllLanguageWords))]
    public void StandaloneWordIsBlocked(string language, string word)
    {
        AssertBlocked(word, language, word, "standalone");
    }

    [Theory]
    [MemberData(nameof(AllLanguageWords))]
    public void SafePrefixAndWordIsBlocked(string language, string word)
    {
        AssertBlocked(SafeWord + word, language, word, "prefix");
    }

    [Theory]
    [MemberData(nameof(AllLanguageWords))]
    public void WordAndSafeSuffixIsBlocked(string language, string word)
    {
        AssertBlocked(word + SafeWord, language, word, "suffix");
    }

    private static void AssertBlocked(string identifier, string language, string word, string scenario)
    {
        var result = ExtendedChecker.Value.Check(identifier);
        Assert.True(result.IsReserved,
            $"[{language}] {scenario}: '{identifier}' contains reserved word '{word}' but was allowed. " +
            "Review the entry's Core partial-match eligibility and configured rules.");
    }
}
