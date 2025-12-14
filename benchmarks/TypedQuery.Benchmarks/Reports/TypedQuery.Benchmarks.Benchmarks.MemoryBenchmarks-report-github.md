```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7462)
AMD Ryzen 5 5600U with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  Job-XGFKHT : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  .NET 8.0   : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2

Runtime=.NET 8.0  Force=True  

```
| Method                         | Job        | InvocationCount | IterationCount | WarmupCount | Mean         | Error       | StdDev      | Gen0    | Gen1   | Allocated |
|------------------------------- |----------- |---------------- |--------------- |------------ |-------------:|------------:|------------:|--------:|-------:|----------:|
| SingleQuery_NoParams           | Job-XGFKHT | 16              | 10             | 3           |   124.925 μs |   8.5994 μs |   5.6879 μs |       - |      - |  19.98 KB |
| SingleQuery_WithParam          | Job-XGFKHT | 16              | 10             | 3           |    41.025 μs |   1.8972 μs |   0.9923 μs |       - |      - |   6.13 KB |
| BatchQuery_5Queries_NoParams   | Job-XGFKHT | 16              | 10             | 3           |   346.531 μs |   7.4456 μs |   4.4307 μs |       - |      - |  59.01 KB |
| BatchQuery_5Queries_WithParams | Job-XGFKHT | 16              | 10             | 3           |   178.612 μs |  12.7845 μs |   8.4561 μs |       - |      - |  32.82 KB |
| BatchQuery_10Queries_Mixed     | Job-XGFKHT | 16              | 10             | 3           |   513.774 μs |   7.0458 μs |   4.6603 μs |       - |      - |  92.46 KB |
| EfCoreQuery_Batch_5Queries     | Job-XGFKHT | 16              | 10             | 3           | 7,194.062 μs | 167.5273 μs | 110.8090 μs |       - |      - | 475.68 KB |
| SingleQuery_NoParams           | .NET 8.0   | Default         | Default        | Default     |    56.764 μs |   0.1406 μs |   0.1174 μs |  2.3804 | 0.1831 |  19.76 KB |
| SingleQuery_WithParam          | .NET 8.0   | Default         | Default        | Default     |     9.051 μs |   0.1063 μs |   0.0994 μs |  0.7172 | 0.1068 |   5.87 KB |
| BatchQuery_5Queries_NoParams   | .NET 8.0   | Default         | Default        | Default     |   199.845 μs |   3.9938 μs |  10.5214 μs |  7.0801 | 0.7324 |  58.44 KB |
| BatchQuery_5Queries_WithParams | .NET 8.0   | Default         | Default        | Default     |    82.996 μs |   0.9014 μs |   0.7990 μs |  3.9063 | 0.2441 |  32.58 KB |
| BatchQuery_10Queries_Mixed     | .NET 8.0   | Default         | Default        | Default     |   282.577 μs |   3.8087 μs |   3.3763 μs | 11.2305 | 0.9766 |  91.93 KB |
| EfCoreQuery_Batch_5Queries     | .NET 8.0   | Default         | Default        | Default     | 5,530.240 μs | 109.5367 μs | 146.2284 μs | 46.8750 |      - | 474.87 KB |
