using System.Text.Json;
using Unclaimable.Extended;
using Xunit;

namespace Unclaimable.Tests;

public sealed class ExtendedProfanity085Tests
{
    private static readonly string[] LanguageCodes =
    {
        "en", "nl", "de", "fr", "es", "it", "pt", "pl",
        "tr", "id", "cs", "vi", "hu", "sv", "ro"
    };

    [Fact]
    public void EverySupportedLanguageHasAnExtendedSingleWordDataset()
    {
        var assembly = typeof(ExtendedData).Assembly;
        var total = 0;

        foreach (var language in LanguageCodes)
        {
            var resourceName = "Unclaimable.Extended.Data.slangprofanity-" + language + ".json";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            Assert.NotNull(stream);
            using var document = JsonDocument.Parse(stream!);
            var root = document.RootElement;

            Assert.Equal(1, root.GetProperty("schema").GetInt32());
            Assert.Equal("slangprofanity", root.GetProperty("category").GetString());
            Assert.Equal(language, root.GetProperty("language").GetString());

            var values = root.GetProperty("values")
                .EnumerateArray()
                .Select(entry => entry.GetString()!)
                .ToArray();

            Assert.NotEmpty(values);
            Assert.Equal(values.Length, values.Distinct(StringComparer.Ordinal).Count());
            Assert.All(values, entry =>
            {
                Assert.False(string.IsNullOrWhiteSpace(entry));
                Assert.DoesNotContain(' ', entry);
                Assert.Equal(entry, entry.Trim());
            });

            total += values.Length;
        }

        Assert.Equal(314, total);
    }

    [Fact]
    public void OptInExtendedDataIsMatchedByCore()
    {
        var options = new Options();
        options.UseExtendedData();
        var result = new Checker(options).Check("schijters");

        Assert.True(result.IsReserved);
    }

    [Fact]
    public void DisablingExtendedSlangCategoryLeavesCoreIndependent()
    {
        var options = new Options();
        options.UseExtendedData(extended =>
            extended.DisableCategory(ExtendedCategory.SlangProfanity));

        Assert.True(ExtendedData.GetCount(ExtendedCategory.SlangProfanity) >= 314);
        Assert.NotEqual("slangprofanity", new Checker(options).Check("schijters").Category);
    }
}
