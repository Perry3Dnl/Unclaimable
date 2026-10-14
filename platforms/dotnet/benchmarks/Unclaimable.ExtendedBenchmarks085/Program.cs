using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Unclaimable;
using Unclaimable.Extended;

BenchmarkRunner.Run<ExtendedIndex085Benchmarks>();

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 8)]
public class ExtendedIndex085Benchmarks
{
    private Checker _checker = null!;
    private Options _options = null!;

    [GlobalSetup]
    public void Setup()
    {
        _options = new Options();
        _options.UseExtendedData();
        _checker = new Checker(_options);
    }

    // Warm re-construction, with JSON already deserialized, tracks per-instance
    // retained indexes and allocations. For true cold start, use a new process.
    [Benchmark]
    public Checker ConstructCheckerWithAllExtended() => new Checker(_options);

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
