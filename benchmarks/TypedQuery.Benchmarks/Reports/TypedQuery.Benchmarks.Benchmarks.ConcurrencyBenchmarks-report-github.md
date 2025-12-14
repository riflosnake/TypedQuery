```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7462)
AMD Ryzen 5 5600U with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  Job-ZWIRNJ : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  .NET 8.0   : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2

Runtime=.NET 8.0  

```
| Method                           | Job        | InvocationCount | IterationCount | WarmupCount | ThreadCount | Mean       | Error     | StdDev    | Gen0    | Completed Work Items | Lock Contentions | Gen1    | Allocated |
|--------------------------------- |----------- |---------------- |--------------- |------------ |------------ |-----------:|----------:|----------:|--------:|---------------------:|-----------------:|--------:|----------:|
| **ConcurrentBatchExecutions_RawSql** | **Job-ZWIRNJ** | **16**              | **10**             | **3**           | **1**           |   **325.7 μs** |  **15.88 μs** |  **10.50 μs** |       **-** |                    **-** |                **-** |       **-** |   **50.1 KB** |
| ConcurrentBatchExecutions_RawSql | .NET 8.0   | Default         | Default        | Default     | 1           |   170.5 μs |   1.75 μs |   1.64 μs |  5.8594 |                    - |                - |  0.7324 |  49.53 KB |
| **ConcurrentBatchExecutions_RawSql** | **Job-ZWIRNJ** | **16**              | **10**             | **3**           | **4**           | **1,229.4 μs** |  **21.68 μs** |  **11.34 μs** |       **-** |                    **-** |                **-** |       **-** | **197.88 KB** |
| ConcurrentBatchExecutions_RawSql | .NET 8.0   | Default         | Default        | Default     | 4           |   672.8 μs |   1.51 μs |   1.18 μs | 23.4375 |                    - |                - |  4.8828 | 197.23 KB |
| **ConcurrentBatchExecutions_RawSql** | **Job-ZWIRNJ** | **16**              | **10**             | **3**           | **8**           | **3,040.0 μs** | **639.55 μs** | **423.02 μs** |       **-** |                    **-** |                **-** |       **-** | **394.96 KB** |
| ConcurrentBatchExecutions_RawSql | .NET 8.0   | Default         | Default        | Default     | 8           | 1,346.6 μs |   5.29 μs |   4.95 μs | 46.8750 |                    - |                - | 13.6719 | 394.18 KB |
