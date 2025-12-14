using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

var config = ManualConfig.Create(DefaultConfig.Instance)
    .AddDiagnoser(MemoryDiagnoser.Default)
    .AddExporter(MarkdownExporter.GitHub)
    .AddExporter(HtmlExporter.Default)
    .WithOptions(ConfigOptions.DisableOptimizationsValidator)
    .AddJob(Job.Default
        .WithWarmupCount(3)
        .WithIterationCount(10)
        .WithInvocationCount(16));

// Run all benchmarks or specific ones based on args
if (args.Length > 0)
{
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
}
else
{
    BenchmarkRunner.Run(typeof(Program).Assembly, config);
}
