```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7462)
AMD Ryzen 5 5600U with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  Job-ZWIRNJ : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  .NET 8.0   : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2

Runtime=.NET 8.0  

```
| Method                    | Job        | InvocationCount | IterationCount | WarmupCount | QueryCount | Mean         | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|-------------------------- |----------- |---------------- |--------------- |------------ |----------- |-------------:|-----------:|-----------:|------:|--------:|-----:|---------:|--------:|-----------:|------------:|
| **AdoNet_Sequential**         | **Job-ZWIRNJ** | **16**              | **10**             | **3**           | **2**          |     **97.46 μs** |   **7.794 μs** |   **5.155 μs** |  **1.00** |    **0.07** |    **1** |        **-** |       **-** |   **10.39 KB** |        **1.00** |
| Dapper_Sequential         | Job-ZWIRNJ | 16              | 10             | 3           | 2          |    104.02 μs |   4.500 μs |   2.977 μs |  1.07 |    0.06 |    1 |        - |       - |   12.15 KB |        1.17 |
| Dapper_QueryMultiple      | Job-ZWIRNJ | 16              | 10             | 3           | 2          |    114.41 μs |   5.755 μs |   3.425 μs |  1.18 |    0.07 |    1 |        - |       - |   13.78 KB |        1.33 |
| EfCore_Sequential         | Job-ZWIRNJ | 16              | 10             | 3           | 2          |    457.72 μs |  13.040 μs |   7.760 μs |  4.71 |    0.24 |    3 |        - |       - |   40.41 KB |        3.89 |
| TypedQuery_RawSql_Batched | Job-ZWIRNJ | 16              | 10             | 3           | 2          |    148.15 μs |   7.109 μs |   4.231 μs |  1.52 |    0.08 |    2 |        - |       - |   18.09 KB |        1.74 |
| TypedQuery_EfCore_Batched | Job-ZWIRNJ | 16              | 10             | 3           | 2          |  3,076.53 μs |  48.984 μs |  25.620 μs | 31.64 |    1.56 |    4 |        - |       - |  200.98 KB |       19.35 |
|                           |            |                 |                |             |            |              |            |            |       |         |      |          |         |            |             |
| AdoNet_Sequential         | .NET 8.0   | Default         | Default        | Default     | 2          |     54.58 μs |   0.239 μs |   0.212 μs |  1.00 |    0.01 |    3 |   1.2207 |       - |   10.25 KB |        1.00 |
| Dapper_Sequential         | .NET 8.0   | Default         | Default        | Default     | 2          |     47.23 μs |   0.521 μs |   0.487 μs |  0.87 |    0.01 |    1 |   1.4648 |       - |   12.02 KB |        1.17 |
| Dapper_QueryMultiple      | .NET 8.0   | Default         | Default        | Default     | 2          |     46.98 μs |   0.243 μs |   0.227 μs |  0.86 |    0.01 |    1 |   1.6479 |       - |   13.69 KB |        1.34 |
| EfCore_Sequential         | .NET 8.0   | Default         | Default        | Default     | 2          |    170.80 μs |   1.070 μs |   1.001 μs |  3.13 |    0.02 |    4 |   4.8828 |       - |   40.36 KB |        3.94 |
| TypedQuery_RawSql_Batched | .NET 8.0   | Default         | Default        | Default     | 2          |     50.60 μs |   0.642 μs |   0.601 μs |  0.93 |    0.01 |    2 |   2.1362 |  0.1221 |   17.83 KB |        1.74 |
| TypedQuery_EfCore_Batched | .NET 8.0   | Default         | Default        | Default     | 2          |  2,154.95 μs |  11.505 μs |   9.607 μs | 39.48 |    0.23 |    5 |  23.4375 |  3.9063 |  198.81 KB |       19.40 |
|                           |            |                 |                |             |            |              |            |            |       |         |      |          |         |            |             |
| **AdoNet_Sequential**         | **Job-ZWIRNJ** | **16**              | **10**             | **3**           | **5**          |    **216.97 μs** |   **8.693 μs** |   **5.750 μs** |  **1.00** |    **0.04** |    **1** |        **-** |       **-** |   **24.63 KB** |        **1.00** |
| Dapper_Sequential         | Job-ZWIRNJ | 16              | 10             | 3           | 5          |    235.94 μs |   8.205 μs |   4.883 μs |  1.09 |    0.03 |    1 |        - |       - |   29.06 KB |        1.18 |
| Dapper_QueryMultiple      | Job-ZWIRNJ | 16              | 10             | 3           | 5          |    261.30 μs |   9.111 μs |   5.422 μs |  1.21 |    0.04 |    1 |        - |       - |   36.03 KB |        1.46 |
| EfCore_Sequential         | Job-ZWIRNJ | 16              | 10             | 3           | 5          |  1,038.89 μs |  79.131 μs |  47.090 μs |  4.79 |    0.24 |    2 |        - |       - |   97.87 KB |        3.97 |
| TypedQuery_RawSql_Batched | Job-ZWIRNJ | 16              | 10             | 3           | 5          |    301.61 μs |   7.268 μs |   4.325 μs |  1.39 |    0.04 |    1 |        - |       - |   45.24 KB |        1.84 |
| TypedQuery_EfCore_Batched | Job-ZWIRNJ | 16              | 10             | 3           | 5          |  7,520.37 μs |  85.718 μs |  51.009 μs | 34.68 |    0.90 |    3 |        - |       - |  496.15 KB |       20.15 |
|                           |            |                 |                |             |            |              |            |            |       |         |      |          |         |            |             |
| AdoNet_Sequential         | .NET 8.0   | Default         | Default        | Default     | 5          |    129.30 μs |   0.357 μs |   0.298 μs |  1.00 |    0.00 |    3 |   2.9297 |       - |   24.37 KB |        1.00 |
| Dapper_Sequential         | .NET 8.0   | Default         | Default        | Default     | 5          |    109.23 μs |   0.569 μs |   0.532 μs |  0.84 |    0.00 |    1 |   3.4180 |  0.1221 |   28.78 KB |        1.18 |
| Dapper_QueryMultiple      | .NET 8.0   | Default         | Default        | Default     | 5          |    123.48 μs |   0.429 μs |   0.402 μs |  0.95 |    0.00 |    2 |   4.3945 |       - |   35.95 KB |        1.48 |
| EfCore_Sequential         | .NET 8.0   | Default         | Default        | Default     | 5          |    415.93 μs |   1.616 μs |   1.511 μs |  3.22 |    0.01 |    4 |  11.7188 |       - |   98.59 KB |        4.05 |
| TypedQuery_RawSql_Batched | .NET 8.0   | Default         | Default        | Default     | 5          |    122.71 μs |   0.335 μs |   0.280 μs |  0.95 |    0.00 |    2 |   5.3711 |  0.2441 |   44.99 KB |        1.85 |
| TypedQuery_EfCore_Batched | .NET 8.0   | Default         | Default        | Default     | 5          |  5,327.27 μs |  36.922 μs |  34.536 μs | 41.20 |    0.27 |    5 |  54.6875 |  7.8125 |  494.76 KB |       20.30 |
|                           |            |                 |                |             |            |              |            |            |       |         |      |          |         |            |             |
| **AdoNet_Sequential**         | **Job-ZWIRNJ** | **16**              | **10**             | **3**           | **10**         |    **383.50 μs** |  **12.230 μs** |   **7.278 μs** |  **1.00** |    **0.03** |    **1** |        **-** |       **-** |   **44.32 KB** |        **1.00** |
| Dapper_Sequential         | Job-ZWIRNJ | 16              | 10             | 3           | 10         |    425.71 μs |   8.827 μs |   4.617 μs |  1.11 |    0.02 |    1 |        - |       - |   53.15 KB |        1.20 |
| Dapper_QueryMultiple      | Job-ZWIRNJ | 16              | 10             | 3           | 10         |    510.15 μs |   8.580 μs |   4.487 μs |  1.33 |    0.03 |    1 |        - |       - |   80.92 KB |        1.83 |
| EfCore_Sequential         | Job-ZWIRNJ | 16              | 10             | 3           | 10         |  1,939.34 μs | 227.805 μs | 150.679 μs |  5.06 |    0.39 |    2 |        - |       - |  185.68 KB |        4.19 |
| TypedQuery_RawSql_Batched | Job-ZWIRNJ | 16              | 10             | 3           | 10         |    591.54 μs |  30.299 μs |  15.847 μs |  1.54 |    0.05 |    1 |        - |       - |  100.03 KB |        2.26 |
| TypedQuery_EfCore_Batched | Job-ZWIRNJ | 16              | 10             | 3           | 10         | 15,422.21 μs | 166.379 μs | 110.050 μs | 40.23 |    0.78 |    3 |  62.5000 |       - | 1013.16 KB |       22.86 |
|                           |            |                 |                |             |            |              |            |            |       |         |      |          |         |            |             |
| AdoNet_Sequential         | .NET 8.0   | Default         | Default        | Default     | 10         |    234.62 μs |   1.803 μs |   1.687 μs |  1.00 |    0.01 |    2 |   5.1270 |  0.2441 |   43.81 KB |        1.00 |
| Dapper_Sequential         | .NET 8.0   | Default         | Default        | Default     | 10         |    211.09 μs |   1.865 μs |   1.744 μs |  0.90 |    0.01 |    1 |   6.3477 |  0.2441 |   52.64 KB |        1.20 |
| Dapper_QueryMultiple      | .NET 8.0   | Default         | Default        | Default     | 10         |    253.47 μs |   0.960 μs |   0.801 μs |  1.08 |    0.01 |    3 |   9.7656 |  0.4883 |   80.84 KB |        1.85 |
| EfCore_Sequential         | .NET 8.0   | Default         | Default        | Default     | 10         |    798.76 μs |   2.214 μs |   1.963 μs |  3.40 |    0.02 |    5 |  21.4844 |       - |  184.22 KB |        4.20 |
| TypedQuery_RawSql_Batched | .NET 8.0   | Default         | Default        | Default     | 10         |    263.90 μs |   0.988 μs |   0.924 μs |  1.12 |    0.01 |    4 |  12.2070 |  0.9766 |   99.78 KB |        2.28 |
| TypedQuery_EfCore_Batched | .NET 8.0   | Default         | Default        | Default     | 10         | 10,814.98 μs |  85.287 μs |  79.778 μs | 46.10 |    0.46 |    6 | 109.3750 | 15.6250 | 1007.56 KB |       23.00 |
