using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Unclaimable.Tests;

public sealed class ConformanceTests
{
    private sealed class ConformanceCase
    {
        [JsonPropertyName("value")]
        public string Value { get; init; } = string.Empty;

        [JsonPropertyName("reserved")]
        public bool Reserved { get; init; }

        [JsonPropertyName("category")]
        public string? Category { get; init; }
    }

    public static IEnumerable<object[]> Cases()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "conformance-cases.json");
        var json = File.ReadAllText(path);
        var cases = JsonSerializer.Deserialize<ConformanceCase[]>(json)
                    ?? throw new InvalidOperationException("Conformance cases could not be loaded.");

        return cases.Select(testCase => new object[]
        {
            testCase.Value,
            testCase.Reserved,
            testCase.Category
        });
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void DotNetAdapterMatchesSharedConformanceCases(string value, bool reserved, string? category)
    {
        var result = UnclaimableChecker.Default.Check(value);

        Assert.Equal(reserved, result.IsReserved);
        Assert.Equal(category, result.Category);
    }
}
