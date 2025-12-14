```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7462)
AMD Ryzen 5 5600U with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  Job-ZWIRNJ : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  .NET 8.0   : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2

Runtime=.NET 8.0  

```
| Method                     | Job        | InvocationCount | IterationCount | WarmupCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |---------------- |--------------- |------------ |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Dapper_GetSingle           | Job-ZWIRNJ | 16              | 10             | 3           |  12.699 μs | 1.7899 μs | 1.1839 μs |  1.01 |    0.12 |    1 |      - |      - |   1.48 KB |        1.00 |
| TypedQuery_GetSingle       | Job-ZWIRNJ | 16              | 10             | 3           |  44.088 μs | 5.4936 μs | 3.6337 μs |  3.50 |    0.40 |    2 |      - |      - |   6.09 KB |        4.11 |
| Dapper_GetList             | Job-ZWIRNJ | 16              | 10             | 3           | 102.394 μs | 4.8347 μs | 3.1979 μs |  8.12 |    0.73 |    3 |      - |      - |  16.83 KB |       11.38 |
| TypedQuery_GetList         | Job-ZWIRNJ | 16              | 10             | 3           | 123.015 μs | 9.7648 μs | 6.4588 μs |  9.76 |    0.96 |    4 |      - |      - |  20.78 KB |       14.04 |
| Dapper_MultipleResults     | Job-ZWIRNJ | 16              | 10             | 3           | 183.767 μs | 3.5370 μs | 2.1048 μs | 14.58 |    1.24 |    5 |      - |      - |  32.68 KB |       22.09 |
| TypedQuery_MultipleResults | Job-ZWIRNJ | 16              | 10             | 3           | 226.809 μs | 3.9328 μs | 2.0569 μs | 17.99 |    1.53 |    5 |      - |      - |     38 KB |       25.68 |
|                            |            |                 |                |             |            |           |           |       |         |      |        |        |           |             |
| Dapper_GetSingle           | .NET 8.0   | Default         | Default        | Default     |   5.025 μs | 0.0441 μs | 0.0412 μs |  1.00 |    0.01 |    1 | 0.1678 |      - |   1.39 KB |        1.00 |
| TypedQuery_GetSingle       | .NET 8.0   | Default         | Default        | Default     |   8.894 μs | 0.1259 μs | 0.1178 μs |  1.77 |    0.03 |    2 | 0.7172 | 0.1068 |   5.87 KB |        4.22 |
| Dapper_GetList             | .NET 8.0   | Default         | Default        | Default     |  55.639 μs | 0.4377 μs | 0.3880 μs | 11.07 |    0.12 |    3 | 2.0142 | 0.0610 |  16.74 KB |       12.04 |
| TypedQuery_GetList         | .NET 8.0   | Default         | Default        | Default     |  58.276 μs | 0.8425 μs | 0.7881 μs | 11.60 |    0.18 |    4 | 2.5024 | 0.1831 |  20.52 KB |       14.75 |
| Dapper_MultipleResults     | .NET 8.0   | Default         | Default        | Default     | 106.824 μs | 0.6419 μs | 0.6004 μs | 21.26 |    0.20 |    5 | 3.9063 | 0.2441 |  32.59 KB |       23.44 |
| TypedQuery_MultipleResults | .NET 8.0   | Default         | Default        | Default     | 115.181 μs | 1.2748 μs | 1.1925 μs | 22.92 |    0.29 |    6 | 4.5166 | 0.3662 |  37.44 KB |       26.92 |
