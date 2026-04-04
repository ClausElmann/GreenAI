using System.IO.Compression;
using System.Text.Json;

namespace GreenAi.E2E.VisualAnalysis;

/// <summary>
/// Packages the latest Playwright visual-test screenshots into a structured ZIP
/// suitable for upload to an external AI tool (e.g. ChatGPT, Claude) for analysis.
///
/// Input:  TestResults/Visual/current/{device}/*.png
/// Output: TestResults/Visual/analysis-pack.zip
///
/// Folder layout inside the ZIP:
///   desktop/   ← 1920×1080 screenshots
///   laptop/    ← 1366×768  screenshots
///   tablet/    ← 1024×768  screenshots
///   mobile/    ← 390×844   screenshots
///   instructions.json
///
/// No external packages required — uses System.IO.Compression (built-in).
/// No pixel-diff or AI integration inside this class — export only.
/// </summary>
public static class VisualAnalysisExporter
{
    // Must match VisualTestBase.VisualRoot resolution (AppContext.BaseDirectory/../../../TestResults/Visual)
    private static readonly string VisualRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestResults", "Visual"));

    private static readonly string CurrentRoot  = Path.Combine(VisualRoot, "current");
    private static readonly string PackRoot     = Path.Combine(VisualRoot, "analysis-pack");
    private static readonly string ZipOutput    = Path.Combine(VisualRoot, "analysis-pack.zip");

    private static readonly string[] DeviceFolders = ["desktop", "laptop", "tablet", "mobile"];

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the analysis pack and returns a summary of what was exported.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no screenshots are found under <c>TestResults/Visual/current/</c>.
    /// Run visual tests first: <c>dotnet test tests/GreenAi.E2E --filter "FullyQualifiedName~Visual"</c>
    /// </exception>
    public static async Task<ExportResult> ExportAsync()
    {
        // 1. Collect screenshots from current run
        var screenshots = CollectScreenshots();

        if (screenshots.Count == 0)
            throw new InvalidOperationException(
                "No screenshots found under TestResults/Visual/current/. " +
                "Run visual tests first: " +
                "dotnet test tests/GreenAi.E2E --filter \"FullyQualifiedName~Visual\" --nologo");

        // 2. Prepare temp pack folder (clean slate)
        if (Directory.Exists(PackRoot))
            Directory.Delete(PackRoot, recursive: true);
        Directory.CreateDirectory(PackRoot);

        // 3. Copy screenshots into structured sub-folders
        foreach (var (relativePath, sourcePath) in screenshots)
        {
            var destPath = Path.Combine(PackRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(sourcePath, destPath, overwrite: true);
        }

        // 4. Write instructions.json + analysis-manual.txt
        var instructionsPath = Path.Combine(PackRoot, "instructions.json");
        await WriteInstructionsAsync(instructionsPath);

        var manualPath = Path.Combine(PackRoot, "analysis-manual.txt");
        await WriteAnalysisManualAsync(manualPath);

        // 5. Zip (overwrite any previous export)
        if (File.Exists(ZipOutput))
            File.Delete(ZipOutput);

        ZipFile.CreateFromDirectory(PackRoot, ZipOutput, CompressionLevel.Optimal, includeBaseDirectory: false);

        // 6. Clean up temp folder
        Directory.Delete(PackRoot, recursive: true);

        return new ExportResult(
            ZipPath:    ZipOutput,
            TotalFiles: screenshots.Count,
            ByDevice:   screenshots
                .GroupBy(s => s.RelativePath.Split(Path.DirectorySeparatorChar, '/')[0])
                .ToDictionary(g => g.Key, g => g.Count()));
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static List<(string RelativePath, string SourcePath)> CollectScreenshots()
    {
        var results = new List<(string, string)>();

        if (!Directory.Exists(CurrentRoot))
            return results;

        foreach (var device in DeviceFolders)
        {
            var deviceDir = Path.Combine(CurrentRoot, device);
            if (!Directory.Exists(deviceDir))
                continue;

            foreach (var file in Directory.GetFiles(deviceDir, "*.png", SearchOption.TopDirectoryOnly))
            {
                // Store with device/ prefix so ZIP layout is: desktop/dashboard.png
                var relativePath = Path.Combine(device, Path.GetFileName(file));
                results.Add((relativePath, file));
            }
        }

        return results;
    }

    private static async Task WriteInstructionsAsync(string path)
    {
        var instructions = new
        {
            generated_at = DateTimeOffset.UtcNow.ToString("o"),
            rules = new[]
            {
                "no horizontal overflow — no element should extend beyond viewport width",
                "no overlapping elements — interactive elements must not be covered",
                "navigation must be accessible — top-bar toggle and nav links must be visible",
                "text must not be cut off — no ellipsis-truncated labels should be present",
                "interactive elements must be visible — all buttons and links in viewport"
            },
            focus = new[]
            {
                "layout stability",
                "responsive design",
                "usability"
            },
            devices = new[]
            {
                new { folder = "desktop", viewport = "1920×1080" },
                new { folder = "laptop",  viewport = "1366×768"  },
                new { folder = "tablet",  viewport = "1024×768"  },
                new { folder = "mobile",  viewport = "390×844"   },
            },
            response_format = new
            {
                description = "Return findings as JSON array",
                example = new[]
                {
                    new
                    {
                        device    = "mobile",
                        file      = "dashboard.png",
                        issue     = "Description of the problem",
                        severity  = "high|medium|low",
                        rule      = "which rule from 'rules' above was violated"
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(instructions, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    private static async Task WriteAnalysisManualAsync(string path)
    {
        const string manual = """
            TASK: ANALYZE UI SCREENSHOTS

            You MUST analyze the uploaded ZIP file.

            RULES:
            - no horizontal overflow
            - no overlapping elements
            - navigation must be accessible
            - text must not be cut off
            - interactive elements must be visible

            FOCUS:
            - layout stability
            - responsive design
            - usability

            DEVICES:
            - desktop
            - laptop
            - tablet
            - mobile

            OUTPUT FORMAT (STRICT JSON):

            {
              "summary": {
                "total_images": number,
                "issues_found": number,
                "severity": "low | medium | high"
              },
              "images": [
                {
                  "file": "path",
                  "device": "desktop | laptop | tablet | mobile",
                  "issues": [
                    {
                      "type": "overflow | overlap | navigation | visibility | spacing",
                      "severity": "low | medium | high",
                      "description": "clear explanation",
                      "suggestion": "actionable fix"
                    }
                  ]
                }
              ]
            }

            DO NOT add text outside JSON.
            """;

        await File.WriteAllTextAsync(path, manual);
    }
}

/// <summary>Summary returned by <see cref="VisualAnalysisExporter.ExportAsync"/>.</summary>
public sealed record ExportResult(
    string                     ZipPath,
    int                        TotalFiles,
    IReadOnlyDictionary<string, int> ByDevice)
{
    public override string ToString()
    {
        var deviceSummary = string.Join(", ", ByDevice.Select(kv => $"{kv.Key}:{kv.Value}"));
        return $"Exported {TotalFiles} screenshot(s) [{deviceSummary}] → {ZipPath}";
    }
}
