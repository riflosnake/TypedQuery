```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7462)
AMD Ryzen 5 5600U with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  Job-ZWIRNJ : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  .NET 8.0   : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2

Runtime=.NET 8.0  

```
| Method                                         | Job        | InvocationCount | IterationCount | WarmupCount | Mean        | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------------------------------------------- |----------- |---------------- |--------------- |------------ |------------:|-----------:|-----------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| Scenario1_Dashboard_Dapper_Sequential          | Job-ZWIRNJ | 16              | 10             | 3           | 1,798.48 μs | 194.821 μs | 128.862 μs |  1.00 |    0.10 |    5 |       - |       - | 272.53 KB |        1.00 |
| Scenario1_Dashboard_Dapper_Batched             | Job-ZWIRNJ | 16              | 10             | 3           | 1,690.22 μs | 108.251 μs |  56.617 μs |  0.94 |    0.07 |    5 |       - |       - | 271.32 KB |        1.00 |
| Scenario1_Dashboard_EfCore_Sequential          | Job-ZWIRNJ | 16              | 10             | 3           | 3,032.20 μs | 220.171 μs | 145.629 μs |  1.69 |    0.14 |    6 | 62.5000 |       - | 583.27 KB |        2.14 |
| Scenario1_Dashboard_TypedQuery_Batched         | Job-ZWIRNJ | 16              | 10             | 3           | 1,704.06 μs | 223.623 μs | 147.913 μs |  0.95 |    0.10 |    5 |       - |       - | 290.62 KB |        1.07 |
| Scenario2_OrderDetails_Dapper_Sequential       | Job-ZWIRNJ | 16              | 10             | 3           |    76.94 μs |   4.645 μs |   3.072 μs |  0.04 |    0.00 |    1 |       - |       - |   5.88 KB |        0.02 |
| Scenario2_OrderDetails_Dapper_Batched          | Job-ZWIRNJ | 16              | 10             | 3           |    72.50 μs |   4.580 μs |   2.726 μs |  0.04 |    0.00 |    1 |       - |       - |   5.41 KB |        0.02 |
| Scenario2_OrderDetails_EfCore_Sequential       | Job-ZWIRNJ | 16              | 10             | 3           |   436.64 μs |  11.121 μs |   6.618 μs |  0.24 |    0.02 |    3 |       - |       - |  34.06 KB |        0.12 |
| Scenario2_OrderDetails_TypedQuery_Batched      | Job-ZWIRNJ | 16              | 10             | 3           |   132.94 μs |   6.255 μs |   3.271 μs |  0.07 |    0.01 |    2 |       - |       - |  14.75 KB |        0.05 |
| Scenario3_SearchWithFilters_Dapper_Sequential  | Job-ZWIRNJ | 16              | 10             | 3           |   403.85 μs |  11.167 μs |   7.387 μs |  0.23 |    0.02 |    3 |       - |       - |  19.29 KB |        0.07 |
| Scenario3_SearchWithFilters_Dapper_Batched     | Job-ZWIRNJ | 16              | 10             | 3           |   398.36 μs |   9.979 μs |   5.938 μs |  0.22 |    0.02 |    3 |       - |       - |   18.6 KB |        0.07 |
| Scenario3_SearchWithFilters_TypedQuery_Batched | Job-ZWIRNJ | 16              | 10             | 3           | 1,077.84 μs |  77.362 μs |  46.037 μs |  0.60 |    0.05 |    4 |       - |       - | 167.14 KB |        0.61 |
|                                                |            |                 |                |             |             |            |            |       |         |      |         |         |           |             |
| Scenario1_Dashboard_Dapper_Sequential          | .NET 8.0   | Default         | Default        | Default     |   979.37 μs |  11.911 μs |  10.558 μs |  1.00 |    0.01 |    6 | 33.2031 | 10.7422 | 272.34 KB |        1.00 |
| Scenario1_Dashboard_Dapper_Batched             | .NET 8.0   | Default         | Default        | Default     |   952.17 μs |   6.984 μs |   5.453 μs |  0.97 |    0.01 |    6 | 33.2031 |  6.8359 | 271.23 KB |        1.00 |
| Scenario1_Dashboard_EfCore_Sequential          | .NET 8.0   | Default         | Default        | Default     | 1,557.89 μs |   3.607 μs |   3.374 μs |  1.59 |    0.02 |    7 | 70.3125 | 11.7188 | 582.81 KB |        2.14 |
| Scenario1_Dashboard_TypedQuery_Batched         | .NET 8.0   | Default         | Default        | Default     |   980.95 μs |   1.438 μs |   1.275 μs |  1.00 |    0.01 |    6 | 35.1563 |  9.7656 | 290.01 KB |        1.06 |
| Scenario2_OrderDetails_Dapper_Sequential       | .NET 8.0   | Default         | Default        | Default     |    30.19 μs |   0.246 μs |   0.230 μs |  0.03 |    0.00 |    1 |  0.7019 |       - |   5.74 KB |        0.02 |
| Scenario2_OrderDetails_Dapper_Batched          | .NET 8.0   | Default         | Default        | Default     |    30.38 μs |   0.283 μs |   0.265 μs |  0.03 |    0.00 |    1 |  0.6104 |  0.0610 |   5.34 KB |        0.02 |
| Scenario2_OrderDetails_EfCore_Sequential       | .NET 8.0   | Default         | Default        | Default     |   165.92 μs |   1.137 μs |   1.064 μs |  0.17 |    0.00 |    3 |  3.9063 |       - |  33.32 KB |        0.12 |
| Scenario2_OrderDetails_TypedQuery_Batched      | .NET 8.0   | Default         | Default        | Default     |    39.20 μs |   0.507 μs |   0.474 μs |  0.04 |    0.00 |    2 |  1.7090 |  0.1221 |  14.35 KB |        0.05 |
| Scenario3_SearchWithFilters_Dapper_Sequential  | .NET 8.0   | Default         | Default        | Default     |   308.98 μs |   3.387 μs |   3.002 μs |  0.32 |    0.00 |    4 |  1.9531 |       - |  19.11 KB |        0.07 |
| Scenario3_SearchWithFilters_Dapper_Batched     | .NET 8.0   | Default         | Default        | Default     |   307.81 μs |   3.526 μs |   3.125 μs |  0.31 |    0.00 |    4 |  1.9531 |       - |  18.52 KB |        0.07 |
| Scenario3_SearchWithFilters_TypedQuery_Batched | .NET 8.0   | Default         | Default        | Default     |   614.95 μs |   6.733 μs |   5.622 μs |  0.63 |    0.01 |    5 | 19.5313 |  2.9297 | 166.59 KB |        0.61 |
