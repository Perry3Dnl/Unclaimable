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
