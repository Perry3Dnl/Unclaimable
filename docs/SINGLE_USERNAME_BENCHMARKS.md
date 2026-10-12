# Single-username latency benchmarks (0.8.4)

Run from the repository root with the .NET 8 SDK installed:

```bash
dotnet run --project platforms/dotnet/benchmarks/Unclaimable.Benchmarks083/Unclaimable.Benchmarks083.csproj -c Release -- --filter '*SingleUsername084Benchmarks*'
```

BenchmarkDotNet reports **mean latency per invocation**, error, standard deviation, and **allocated bytes per operation**. The measured method calls `Checker.Check(username)` using a checker built in `GlobalSetup`, so construction and dataset loading are excluded from the timing. It performs warmups and repeated measurements; use Release mode and a quiet machine. Results vary with CPU, runtime, power management, and configuration. No one benchmark number is a universal upper time bound.

Scenarios: ordinary accepted username; reserved exact hit; embedded profanity hit; late custom partial hit in 10,000 entries; an unmatched identifier forcing an exhaustive custom partial scan; and a High-sensitivity obfuscation near miss.

**Worst-case interpretation:** The full-scan miss is intentionally structured to traverse every participating custom partial entry. A late-hit case also exists, but the sort does not guarantee insertion-order stability for equal-length entries, so its precise position is not guaranteed. Neither test establishes a mathematically maximal input over the whole engine: different enabled rules, Unicode inputs, long lengths, data volumes, and High/Extreme obfuscation may cost more. Check overall tail latency separately in production representative traffic.

The companion unit test `SingleUsernamePerformance084Tests` verifies the test inputs retain their intended accepted/rejected semantics. Unit tests must **not** enforce wall-clock timing thresholds; performance is measured with BenchmarkDotNet and compared against prior runs on the same hardware and runtime.
