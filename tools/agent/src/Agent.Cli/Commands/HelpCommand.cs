namespace Agent.Cli.Commands;

public static class HelpCommand
{
    public static void Write()
    {
        Console.WriteLine("Core:");
        Console.WriteLine("  doctor    check toolchain health");
        Console.WriteLine("  context   compact repo/workspace summary");
        Console.WriteLine("  projects  solution/project/dependency graph");
        Console.WriteLine("  packages  NuGet/package graph");
        Console.WriteLine("  symbols   find C# symbols");
        Console.WriteLine("  refs      find references to C# symbols");
        Console.WriteLine("  member    extract member by symbol or file/line");
        Console.WriteLine("  graph     call/project graph");
        Console.WriteLine("  build     build and emit diagnostics");
        Console.WriteLine("  test      run tests and emit failures");
        Console.WriteLine("  run       run configured or explicit command");
        Console.WriteLine("  diff      git diff/status as structured changes");
        Console.WriteLine("  hotspots  static risk/perf hotspot scan");
        Console.WriteLine("  logs      inspect run/build/test logs");
        Console.WriteLine("  bench     run configured benchmark command");
        Console.WriteLine("  bench-history compare benchmark/log history");
        Console.WriteLine("  concat    build token-aware context bundle");
        Console.WriteLine("  search    structured text fallback");
        Console.WriteLine("  server    daemon status/start/stop marker");
        Console.WriteLine("  cache     inspect/clear cache buckets");
        Console.WriteLine();
        Console.WriteLine("Common flags: --format json|ndjson|table|markdown --max <n> --solution <path>");
        Console.WriteLine("Concat flags: --profile <name>|all --include <scope[,scope]|glob>");
        Console.WriteLine();
        Console.WriteLine("Presets:");
        Console.WriteLine("  preset fbx-mesh-regression");
        Console.WriteLine("  preset npr-parity-audit");
        Console.WriteLine("  preset gpu-readback-audit");
        Console.WriteLine("  preset render-perf-release");
        Console.WriteLine("  preset allocation-sweep");
        Console.WriteLine("  preset aot-boundary-check");
    }
}
