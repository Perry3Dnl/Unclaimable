using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Unclaimable;
using Unclaimable.Email;

BenchmarkSwitcher
    .FromAssembly(typeof(Core083Benchmarks).Assembly)
    .Run(args);

[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 6)]
public class Core083Benchmarks
{
    private Checker _checker = null!;
    private Checker _highChecker = null!;

    [GlobalSetup]
    public void Setup()
    {
        _checker = new Checker();

        var high = new Options
        {
            ObfuscationSensitivity = ObfuscationSensitivity.High
        };
        high.DisablePattern(Pattern.Repeated);
        _highChecker = new Checker(high);
    }

    [Benchmark(Baseline = true)]
    public Result CheckOrdinaryAccepted() =>
        _checker.Check("zqvxpioneer");

    [Benchmark]
    public bool IsClaimableOrdinaryAccepted() =>
        _checker.IsClaimable("zqvxpioneer");

    [Benchmark]
    public Result CheckExactReserved() =>
        _checker.Check("admin");

    [Benchmark]
    public bool IsClaimableExactReserved() =>
        _checker.IsClaimable("admin");

    [Benchmark]
    public Result CheckObfuscation() =>
        _checker.Check("N1ke");

    [Benchmark]
    public Result CheckHighNearMatchMiss() =>
        _highChecker.Check("admmmin");
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 6)]
public class Email083Benchmarks
{
    private EmailChecker _checker = null!;

    [GlobalSetup]
    public void Setup()
    {
        var options = new EmailOptions();
        options.ProtectedDomains.Add("examplebrand.com");
        _checker = new EmailChecker(options);
    }

    [Benchmark(Baseline = true)]
    public EmailResult OrdinaryEmail() =>
        _checker.CheckExistingAddress("bluegarden@example.net");

    [Benchmark]
    public EmailResult ExactProtectedDomain() =>
        _checker.CheckExistingAddress("bluegarden@examplebrand.com");

    [Benchmark]
    public EmailResult ProtectedDomainLookalike() =>
        _checker.CheckExistingAddress("bluegarden@examp1ebrand.com");
}

 
// Scale partial matching independently of the built-in curated dataset.
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 6)]
public class PartialMatchingScale084Benchmarks
{
    [Params(0, 100, 1000, 5000, 10000)]
    public int PartialEntryCount { get; set; }

    private Checker _checker = null!;
    private string _lastEntry = null!;

    [GlobalSetup]
    public void Setup()
    {
        var options = new Options();
        options.DisableRule(Rule.ObfuscationMatching);
        _lastEntry = "zzpartial" + Math.Max(0, PartialEntryCount - 1).ToString("D5");
        for (var index = 0; index < PartialEntryCount; index++)
        {
            options.AdditionalReserved.Add("zzpartial" + index.ToString("D5"));
        }

        _checker = new Checker(options);
    }

    [Benchmark]
    public Result CheckMiss() => _checker.Check("zznomatchingreservationhere");

    [Benchmark]
    public Result CheckLateMatch() => _checker.Check("prefix" + _lastEntry + "suffix");
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 6)]
public class AdversarialObfuscation084Benchmarks
{
    private Checker _checker = null!;
    private string _candidate = null!;

    [Params(ObfuscationSensitivity.Medium, ObfuscationSensitivity.High, ObfuscationSensitivity.Extreme)]
    public ObfuscationSensitivity Sensitivity { get; set; }

    [Params(32, 128, 512)]
    public int CandidateLength { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var options = new Options { ObfuscationSensitivity = Sensitivity };
        options.DisableRule(Rule.MaximumLength);
        options.DisablePattern(Pattern.Repeated);
        _checker = new Checker(options);
        _candidate = "xqzv" + new string('x', CandidateLength - 4);
    }

    [Benchmark]
    public Result CheckLongMiss() => _checker.Check(_candidate);
}
