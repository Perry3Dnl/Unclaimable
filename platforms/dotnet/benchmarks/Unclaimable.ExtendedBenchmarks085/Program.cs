using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Unclaimable;
using Unclaimable.Extended;

BenchmarkRunner.Run<ExtendedIndex085Benchmarks>();

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 8)]
public class ExtendedIndex085Benchmarks
{
    [Params("Core", "Core+Extended")]
    public string PackageSet { get; set; } = "Core";

    private Checker _checker = null!;
    private Options _options = null!;

    [GlobalSetup]
    public void Setup()
    {
        _options = new Options();
        if (PackageSet == "Core+Extended")
        {
            _options.UseExtendedData();
        }
        _checker = new Checker(_options);
    }

    // Warm re-construction with data already loaded. Measures allocations,
    // not retained heap or first-process startup. PackageSet identifies Core-only
    // versus Core+Extended, while the Git ref identifies package version.
    [Benchmark]
    public Checker ConstructChecker() => new Checker(_options);

    [Benchmark]
    public bool NoMatch() => _checker.IsClaimable("qzxnormalusername");

    [Benchmark]
    public bool ExactMatch() => _checker.IsClaimable("pillock");

    [Benchmark]
    public bool EmbeddedMatch() => _checker.IsClaimable("prefixpillocksuffix");

    [Benchmark]
    public bool CompactEmbeddedMatch() => _checker.IsClaimable("prefixpill-ocksuffix");

    [Benchmark]
    public Result DiagnosticMatch() => _checker.Check("prefixpillocksuffix");
}
