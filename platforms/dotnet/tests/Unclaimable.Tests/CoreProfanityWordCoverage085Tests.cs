using System.Text.Json;
using Xunit;

namespace Unclaimable.Tests;

/// <summary>
/// Regression contract for Core profanity vocabulary: a standalone word, plus
/// the word preceded or followed by an otherwise innocuous identifier fragment.
/// Intentionally exposes roots that lack safe partial-match eligibility.
/// </summary>
public sealed class CoreProfanityWordCoverage085Tests
{
    private const string SafeWord = "orchid";
    private static readonly Lazy<Checker> CoreChecker = new(() =>
        new Checker(new Options { MaximumLength = 512, PartialMatchMinimumLength = 1, ProfanityPartialMatching = true }));

    public static IEnumerable<object[]> AllCoreProfanityWords()
    {
        var assembly = typeof(Checker).Assembly;
        foreach (var resourceName in assembly.GetManifestResourceNames()
                     .Where(name => name.StartsWith("Unclaimable.Data.", StringComparison.Ordinal)
                                    && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Missing resource {resourceName}");
            using var json = JsonDocument.Parse(stream);
            var root = json.RootElement;
            if (!root.TryGetProperty("category", out var category)
                || category.GetString() != "profanity")
                continue;

            var language = root.TryGetProperty("language", out var lang)
                ? lang.GetString() ?? "en"
                : "en";
            var words = new HashSet<string>(StringComparer.Ordinal);
            foreach (var name in new[] { "values", "partialValues" })
            {
                if (!root.TryGetProperty(name, out var entries))
                    continue;
                foreach (var item in entries.EnumerateArray())
                {
                    var word = item.GetString();
                    if (!string.IsNullOrWhiteSpace(word)
                        && words.Add(word.Normalize().ToLowerInvariant()))
                        yield return new object[] { language, word };
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllCoreProfanityWords))]
    public void StandaloneWordIsBlocked(string language, string word) =>
        AssertBlocked(word, language, word, "standalone");

    [Theory]
    [MemberData(nameof(AllCoreProfanityWords))]
    public void SafePrefixAndWordIsBlocked(string language, string word) =>
        AssertBlocked(SafeWord + word, language, word, "prefix");

    [Theory]
    [MemberData(nameof(AllCoreProfanityWords))]
    public void WordAndSafeSuffixIsBlocked(string language, string word) =>
        AssertBlocked(word + SafeWord, language, word, "suffix");

    private static void AssertBlocked(string identifier, string language, string word, string scenario)
    {
        var result = CoreChecker.Value.Check(identifier);
        Assert.True(result.IsReserved,
            $"[{language}] {scenario}: '{identifier}' containing '{word}' was accepted by Core.");
    }
}
