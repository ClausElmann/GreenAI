using GreenAi.E2E.VisualAnalysis;

namespace GreenAi.E2E.VisualAnalysis;

/// <summary>
/// Trigger for the visual analysis export pipeline.
///
/// Usage — run on demand after visual tests have produced screenshots:
///
///   dotnet test tests/GreenAi.E2E --filter ExportVisualAnalysisPackage --nologo
///
/// What it does:
///   1. Reads latest screenshots from TestResults/Visual/current/{device}/
///   2. Copies them into a structured temp folder (analysis-pack/)
///   3. Generates instructions.json with analysis rules
///   4. Zips everything to TestResults/Visual/analysis-pack.zip
///   5. Cleans up the temp folder
///
/// Output:
///   TestResults/Visual/analysis-pack.zip
///
/// Next step (manual):
///   Upload the ZIP + instructions.json contents to ChatGPT/Claude and ask:
///   "Analyse the screenshots using the included instructions.json rules."
///
/// Pre-requisite:
///   Visual tests must have run first to produce screenshots.
///   dotnet test tests/GreenAi.E2E --filter "FullyQualifiedName~Visual" --nologo
/// </summary>
public sealed class ExportVisualAnalysisTests
{
    [Fact]
    [Trait("Category", "OnDemand")]
    public async Task ExportVisualAnalysisPackage()
    {
        // Skip gracefully if visual tests haven't run yet (no screenshots)
        // Use AppContext.BaseDirectory (same resolution as VisualAnalysisExporter)
        var screenshotsDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestResults", "Visual", "current"));
        if (!Directory.Exists(screenshotsDir) ||
            !Directory.EnumerateFiles(screenshotsDir, "*.png", SearchOption.AllDirectories).Any())
        {
            Assert.Skip(
                "No screenshots found. Run visual tests first: " +
                "dotnet test tests/GreenAi.E2E --filter \"FullyQualifiedName~Visual\" --nologo");
        }

        var result = await VisualAnalysisExporter.ExportAsync();

        // Surface the zip path prominently in test output
        Assert.True(
            File.Exists(result.ZipPath),
            $"Export succeeded but ZIP not found at: {result.ZipPath}");

        // At least 1 file must have been packaged
        Assert.True(
            result.TotalFiles > 0,
            "ZIP was created but appears to contain 0 screenshots — unexpected.");

        // Human-readable summary printed to test output (visible in dotnet test -v n)
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  VISUAL ANALYSIS PACKAGE CREATED");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine($"  ZIP : {result.ZipPath}");
        Console.WriteLine($"  Files:");
        foreach (var (device, count) in result.ByDevice)
            Console.WriteLine($"    {device,-10} {count} screenshot(s)");
        Console.WriteLine($"  Total: {result.TotalFiles} screenshot(s)");
        Console.WriteLine();
        Console.WriteLine("  Next step:");
        Console.WriteLine("    Upload the ZIP and its instructions.json to ChatGPT/Claude.");
        Console.WriteLine("    Prompt: Analyse these screenshots per instructions.json.");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
    }
}
