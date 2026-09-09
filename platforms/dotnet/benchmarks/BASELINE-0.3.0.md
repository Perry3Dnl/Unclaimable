# Benchmark baseline — Unclaimable 0.3.0

Captured on 2026-09-09 from tag `v0.3.0` / commit `6233aa7ec70a5c69196023a227a4a76b633f8890` before any 0.4.0 matching changes.

Environment: BenchmarkDotNet 0.14.0, Ubuntu 24.04.5 LTS, AMD EPYC 9V74, .NET 8.0.31, x64 RyuJIT AVX2. Three warmup iterations and eight measurement iterations were requested.

| Benchmark | Mean | Allocated |
| --- | ---: | ---: |
| Construction | 2.865 ms | 2,393,186 B |
| Ordinary accepted input | 79.826 µs | 1,368 B |
| Exact rejection | 316.5 ns | 200 B |
| Obfuscation | 58.509 µs | 896 B |
| All languages enabled | 5.831 ms | 4,332,743 B |

The GitHub Actions benchmark run completed successfully as run `34317531576`. Treat these numbers as a release-comparison baseline rather than absolute performance guarantees; hosted-runner variance is expected.
