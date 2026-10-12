using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Unclaimable;

BenchmarkRunner.Run<CheckerBenchmarks>();

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 8)]
public class CheckerBenchmarks
{
    private Checker _defaultChecker = null!;
    private Checker _obfuscationChecker = null!;

    [GlobalSetup]
    public void Setup()
    {
        _defaultChecker = new Checker();
        _obfuscationChecker = new Checker(new Options
        {
            DisabledRules = Rule.Numbers
        });
    }

    [Benchmark]
    public Checker Construction() => new Checker();

    [Benchmark]
    public Result OrdinaryAcceptedInput() => _defaultChecker.Check("zqvxpioneer");

    [Benchmark]
    public bool OrdinaryAcceptedBoolean() => _defaultChecker.IsClaimable("zqvxpioneer");

    [Benchmark]
    public bool ReservedBoolean() => _defaultChecker.IsReserved("admin");

    [Benchmark]
    public bool OptionalNullBoolean() => _defaultChecker.IsClaimable(null);

    [Benchmark]
    public Result OptionalNullDetailed() => _defaultChecker.Check(null);

    [Benchmark]
    public Result ExactRejection() => _defaultChecker.Check("admin");

    [Benchmark]
    public Result Obfuscation() => _obfuscationChecker.Check("N1ke");

    [Benchmark]
    public Checker AllLanguagesEnabled()
    {
        var options = new Options();
        foreach (var language in Enum.GetValues<Language>())
        {
            options.AddLanguage(language);
        }

        return new Checker(options);
    }
}
