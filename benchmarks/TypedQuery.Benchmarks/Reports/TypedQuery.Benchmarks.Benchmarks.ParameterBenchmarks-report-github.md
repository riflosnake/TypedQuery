```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7462)
AMD Ryzen 5 5600U with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  Job-ZWIRNJ : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  .NET 8.0   : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2

Runtime=.NET 8.0  

```
| Method                              | Job        | InvocationCount | IterationCount | WarmupCount | Mean       | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |---------------- |--------------- |------------ |-----------:|-----------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Dapper_NoParameters                 | Job-ZWIRNJ | 16              | 10             | 3           | 105.528 μs | 14.2006 μs |  9.3928 μs |  1.01 |    0.12 |    5 |      - |      - |  16.81 KB |        1.00 |
| TypedQuery_NoParameters             | Job-ZWIRNJ | 16              | 10             | 3           | 134.146 μs | 11.2915 μs |  7.4687 μs |  1.28 |    0.12 |    6 |      - |      - |  20.76 KB |        1.23 |
| Dapper_SingleParameter              | Job-ZWIRNJ | 16              | 10             | 3           |  18.038 μs |  3.2170 μs |  1.9144 μs |  0.17 |    0.02 |    1 |      - |      - |   2.13 KB |        0.13 |
| TypedQuery_SingleParameter          | Job-ZWIRNJ | 16              | 10             | 3           |  54.322 μs |  9.9547 μs |  6.5844 μs |  0.52 |    0.07 |    3 |      - |      - |   6.17 KB |        0.37 |
| Dapper_FiveParameters               | Job-ZWIRNJ | 16              | 10             | 3           |  42.669 μs |  5.4378 μs |  3.5968 μs |  0.41 |    0.05 |    2 |      - |      - |   5.12 KB |        0.30 |
| TypedQuery_FiveParameters           | Job-ZWIRNJ | 16              | 10             | 3           |  81.578 μs | 13.5448 μs |  8.9591 μs |  0.78 |    0.10 |    4 |      - |      - |  14.63 KB |        0.87 |
| Dapper_TenParameters                | Job-ZWIRNJ | 16              | 10             | 3           |  84.419 μs | 13.0020 μs |  8.6000 μs |  0.81 |    0.10 |    4 |      - |      - |   7.64 KB |        0.45 |
| TypedQuery_TenParameters            | Job-ZWIRNJ | 16              | 10             | 3           | 134.607 μs | 14.4241 μs |  9.5407 μs |  1.28 |    0.13 |    6 |      - |      - |  26.96 KB |        1.60 |
| Dapper_Batch_5QueriesWithParams     | Job-ZWIRNJ | 16              | 10             | 3           | 144.562 μs | 17.6053 μs | 11.6448 μs |  1.38 |    0.15 |    6 |      - |      - |  18.52 KB |        1.10 |
| TypedQuery_Batch_5QueriesWithParams | Job-ZWIRNJ | 16              | 10             | 3           | 191.709 μs | 13.2388 μs |  7.8782 μs |  1.83 |    0.16 |    7 |      - |      - |  32.82 KB |        1.95 |
|                                     |            |                 |                |             |            |            |            |       |         |      |        |        |           |             |
| Dapper_NoParameters                 | .NET 8.0   | Default         | Default        | Default     |  59.520 μs |  1.1727 μs |  1.7189 μs |  1.00 |    0.04 |    7 | 2.0142 | 0.0610 |  16.74 KB |        1.00 |
| TypedQuery_NoParameters             | .NET 8.0   | Default         | Default        | Default     |  65.645 μs |  1.2714 μs |  1.4642 μs |  1.10 |    0.04 |    8 | 2.4414 | 0.1221 |  20.52 KB |        1.23 |
| Dapper_SingleParameter              | .NET 8.0   | Default         | Default        | Default     |   6.232 μs |  0.1215 μs |  0.1818 μs |  0.10 |    0.00 |    1 | 0.2441 |      - |   2.04 KB |        0.12 |
| TypedQuery_SingleParameter          | .NET 8.0   | Default         | Default        | Default     |   9.523 μs |  0.1202 μs |  0.1124 μs |  0.16 |    0.00 |    2 | 0.7172 | 0.1068 |   5.93 KB |        0.35 |
| Dapper_FiveParameters               | .NET 8.0   | Default         | Default        | Default     |  19.286 μs |  0.3835 μs |  0.8733 μs |  0.32 |    0.02 |    3 | 0.6104 |      - |   5.03 KB |        0.30 |
| TypedQuery_FiveParameters           | .NET 8.0   | Default         | Default        | Default     |  21.769 μs |  0.4297 μs |  0.7179 μs |  0.37 |    0.02 |    4 | 1.7395 | 0.1526 |  14.37 KB |        0.86 |
| Dapper_TenParameters                | .NET 8.0   | Default         | Default        | Default     |  39.060 μs |  0.3679 μs |  0.3441 μs |  0.66 |    0.02 |    5 | 0.9155 |      - |   7.55 KB |        0.45 |
| TypedQuery_TenParameters            | .NET 8.0   | Default         | Default        | Default     |  46.247 μs |  0.2832 μs |  0.2649 μs |  0.78 |    0.02 |    6 | 3.1738 | 0.1221 |   26.7 KB |        1.59 |
| Dapper_Batch_5QueriesWithParams     | .NET 8.0   | Default         | Default        | Default     |  70.752 μs |  1.0574 μs |  0.9891 μs |  1.19 |    0.04 |    9 | 2.1973 | 0.1221 |  18.43 KB |        1.10 |
| TypedQuery_Batch_5QueriesWithParams | .NET 8.0   | Default         | Default        | Default     |  79.595 μs |  0.8395 μs |  0.7852 μs |  1.34 |    0.04 |   10 | 3.9063 | 0.2441 |  32.58 KB |        1.95 |
