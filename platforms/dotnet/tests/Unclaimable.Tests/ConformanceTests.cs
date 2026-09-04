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

        return cases.Select(testCase => new object[] { testCase });
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void DotNetAdapterMatchesSharedConformanceCases(ConformanceCase testCase)
    {
        var result = UnclaimableChecker.Default.Check(testCase.Value);

        Assert.Equal(testCase.Reserved, result.IsReserved);
        Assert.Equal(testCase.Category, result.Category);
    }
}
