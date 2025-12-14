```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7462)
AMD Ryzen 5 5600U with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  Job-ZWIRNJ : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  .NET 8.0   : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2

Runtime=.NET 8.0  

```
| Method                         | Job        | InvocationCount | IterationCount | WarmupCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |----------- |---------------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| AdoNet_SingleSelect            | Job-ZWIRNJ | 16              | 10             | 3           | 1,130.9 μs |  63.50 μs | 37.79 μs |  1.00 |    0.05 |    1 |       - |      - | 150.65 KB |        1.00 |
| Dapper_SingleSelect            | Job-ZWIRNJ | 16              | 10             | 3           |   966.9 μs |  25.21 μs | 13.18 μs |  0.86 |    0.03 |    1 |       - |      - | 151.12 KB |        1.00 |
| EfCore_SingleSelect            | Job-ZWIRNJ | 16              | 10             | 3           | 1,674.8 μs | 124.04 μs | 82.04 μs |  1.48 |    0.08 |    2 |       - |      - | 328.37 KB |        2.18 |
| TypedQuery_RawSql_SingleSelect | Job-ZWIRNJ | 16              | 10             | 3           | 1,015.8 μs |  54.57 μs | 32.47 μs |  0.90 |    0.04 |    1 |       - |      - | 161.39 KB |        1.07 |
| TypedQuery_EfCore_SingleSelect | Job-ZWIRNJ | 16              | 10             | 3           | 2,386.6 μs | 114.86 μs | 75.97 μs |  2.11 |    0.09 |    3 |       - |      - | 244.31 KB |        1.62 |
|                                |            |                 |                |             |            |           |          |       |         |      |         |        |           |             |
| AdoNet_SingleSelect            | .NET 8.0   | Default         | Default        | Default     |   744.6 μs |   7.97 μs |  7.46 μs |  1.00 |    0.01 |    2 | 17.5781 | 3.9063 | 150.58 KB |        1.00 |
| Dapper_SingleSelect            | .NET 8.0   | Default         | Default        | Default     |   530.4 μs |   4.15 μs |  3.88 μs |  0.71 |    0.01 |    1 | 17.5781 | 2.9297 | 151.05 KB |        1.00 |
| EfCore_SingleSelect            | .NET 8.0   | Default         | Default        | Default     |   969.8 μs |   9.16 μs |  8.57 μs |  1.30 |    0.02 |    3 | 39.0625 | 7.8125 | 328.28 KB |        2.18 |
| TypedQuery_RawSql_SingleSelect | .NET 8.0   | Default         | Default        | Default     |   536.0 μs |   6.66 μs |  6.23 μs |  0.72 |    0.01 |    1 | 19.5313 | 3.9063 | 161.16 KB |        1.07 |
| TypedQuery_EfCore_SingleSelect | .NET 8.0   | Default         | Default        | Default     | 1,596.6 μs |  26.92 μs | 36.85 μs |  2.14 |    0.05 |    4 | 27.3438 |      - |  246.1 KB |        1.63 |
